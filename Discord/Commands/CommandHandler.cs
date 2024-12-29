using System.Reflection;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Discord.Commands;

    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly ILogger<CommandHandler> _logger;
        private readonly IConfiguration _config;

        public CommandHandler(DiscordSocketClient client, CommandService commands, IServiceProvider services, ILogger<CommandHandler> logger, IConfiguration config)
        {
            _client = client;
            _commands = commands;
            _services = services;
            _logger = logger;
            _config = config;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing CommandHandler...");

            await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);
            _logger.LogInformation("Command modules loaded.");

            _client.MessageReceived += HandleCommandAsync;
            _client.Ready += OnReadyAsync;
            _client.Log += LogAsync;
            _logger.LogInformation("Subscribed to Ready and MessageReceived events.");

            string token = _config["Token"];
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Bot token is missing in the configuration.");
                return;
            }

            _logger.LogInformation("Logging in to Discord...");
            await _client.LoginAsync(TokenType.Bot, token);
            _logger.LogInformation($"Client started. ConnectionState: {_client.ConnectionState}");

            _logger.LogInformation("Starting Discord client...");
            await _client.StartAsync();
            _logger.LogInformation("Discord client started.");
        }
        
        private Task LogAsync(LogMessage logMessage)
        {
            switch (logMessage.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    _logger.LogError(logMessage.ToString());
                    break;
                case LogSeverity.Warning:
                    _logger.LogWarning(logMessage.ToString());
                    break;
                case LogSeverity.Info:
                    _logger.LogInformation(logMessage.ToString());
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    _logger.LogDebug(logMessage.ToString());
                    break;
            }
            return Task.CompletedTask;
        }

        private async Task OnReadyAsync()
        {
            _logger.LogInformation($"Connected as -> [{_client.CurrentUser}] :)");
            
            await Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            
            var guildChannel = message.Channel as SocketGuildChannel;
            if (guildChannel == null || guildChannel.Guild.Id != Convert.ToUInt64(_config["GuidId"]))
            {
                _logger.LogWarning($"Message from unauthorized guild: {guildChannel?.Guild.Id}");
                return;
            }

            int argPos = 0;
            string prefix = _config["Prefix"];

            if (!message.HasStringPrefix(prefix, ref argPos) && !message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                return;

            var context = new SocketCommandContext(_client, message);

            var result = await _commands.ExecuteAsync(context, argPos, _services);

            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
            {
                _logger.LogWarning($"Error executing command: {result.ErrorReason}");
                await context.Channel.SendMessageAsync($"Error: {result.ErrorReason}");
            }
        }
    }
