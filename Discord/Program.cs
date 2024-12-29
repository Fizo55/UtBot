using System.Net.WebSockets;
using System.Text;
using Discord.Commands;
using Discord.Data;
using Discord.Infrastructure;
using Discord.Infrastructure.Data;
using Discord.Interactions;
using Discord.Services;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Discord
{
    public class Program
    {
        private const ulong _alertsChannelId = 1322572727101685802;
        private static Dictionary<string, decimal> _lastVolumes = new();
        private const decimal VolumeChangeThreshold = 0.2m;
        private static DiscordSocketClient _client;
        private static readonly StringBuilder _messageBuffer = new();

        public static async Task Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Config/botconfig.json", optional: false, reloadOnChange: true)
                .Build();

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    services.AddLogging(configure => configure.AddConsole());

                    services.AddDbContextFactory<DatabaseContext>(options =>
                        options.UseNpgsql(config.GetConnectionString("DefaultConnection")));
                    
                    services.AddSingleton<DiscordSocketClient>(_ =>
                    {
                        var discordConfig = new DiscordSocketConfig
                        {
                            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers
                        };
                        return new DiscordSocketClient(discordConfig);
                    });
                    
                    services.AddScoped<IDatabaseHandler, DatabaseHandler>();

                    services.AddSingleton<CommandService>();
                    services.AddSingleton<CommandHandler>();
                    services.AddSingleton<InteractionService>();
                    services.AddSingleton<InteractionHandler>();
                    services.AddSingleton(config);

                    services.AddSingleton<LoggingService>();
                })
                .Build();

            _client = host.Services.GetRequiredService<DiscordSocketClient>();
            await host.Services.GetRequiredService<InteractionHandler>().InitializeAsync();
            await host.Services.GetRequiredService<CommandHandler>().InitializeAsync();
            _ = Task.Run(StartBinanceVolumeMonitor);
            await Task.Delay(-1);
        }

        private static async Task StartBinanceVolumeMonitor()
        {
            var binanceUri = new Uri("wss://stream.binance.com:9443/stream?streams=!ticker@arr");
            using var socket = new ClientWebSocket();

            await socket.ConnectAsync(binanceUri, CancellationToken.None);

            var buffer = new byte[1024 * 32];

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                var chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                _messageBuffer.Append(chunk);

                if (result.EndOfMessage)
                {
                    string rawJson = _messageBuffer.ToString();

                    _messageBuffer.Clear();

                    ProcessBinanceMessage(rawJson);
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }
            }
        }

        private static void ProcessBinanceMessage(string rawJson)
        {
            try
            {
                var payload = JsonConvert.DeserializeObject<BinanceStreamPayload>(rawJson);
                if (payload?.Data == null) return;
                foreach (var ticker in payload.Data)
                {
                    if (!string.IsNullOrEmpty(ticker.Symbol) && (ticker.Symbol.Contains("USDT") || ticker.Symbol.Contains("BTC")))
                    {
                        CheckVolumeChange(ticker);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Binance message: {ex.Message}");
            }
        }

        private static async void CheckVolumeChange(TickerData ticker)
        {
            var symbol = ticker.Symbol;
            var currentVol = ticker.Volume;

            if (!_lastVolumes.TryGetValue(symbol, out var oldVol))
            {
                _lastVolumes[symbol] = currentVol;
                return;
            }

            if (oldVol < 1e-9m)
            {
                _lastVolumes[symbol] = currentVol;
                return;
            }

            var changeRatio = (currentVol - oldVol) / oldVol;

            if (changeRatio >= VolumeChangeThreshold)
            {
                if (_client.GetChannel(_alertsChannelId) is IMessageChannel channel)
                {
                    await channel.SendMessageAsync(
                        $"**Volume spike detected !**\n" +
                        $"Symbol: **{symbol}**\n" +
                        $"Old volume: `{oldVol}`\n" +
                        $"New volume: `{currentVol}`\n" +
                        $"Change: `{changeRatio:P2}`"
                    );
                }
            }

            _lastVolumes[symbol] = currentVol;
        }
    }
}