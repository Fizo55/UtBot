using System.Reflection;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace Discord
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactionService;
        private readonly IServiceProvider _services;
        private readonly ILogger<InteractionHandler> _logger;

        public InteractionHandler(DiscordSocketClient client, InteractionService interactionService, IServiceProvider services, ILogger<InteractionHandler> logger)
        {
            _client = client;
            _interactionService = interactionService;
            _services = services;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            await _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);

            _client.Ready += OnReadyAsync;
            _client.InteractionCreated += HandleInteractionAsync;
        }

        private async Task OnReadyAsync()
        {
            foreach (var guild in _client.Guilds)
            {
                await _interactionService.RegisterCommandsToGuildAsync(guild.Id);
                _logger.LogInformation($"Registered commands to guild: {guild.Name} ({guild.Id})");
            }

            _logger.LogInformation($"Connected as -> {_client.CurrentUser} :)");
        }

        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            try
            {
                var ctx = new SocketInteractionContext(_client, interaction);
                await _interactionService.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception handling interaction: {ex.Message}");
                if (interaction.Type == InteractionType.ApplicationCommand)
                {
                    await interaction.RespondAsync("❌ An error occurred while processing your command.", ephemeral: true);
                }
            }
        }
    }
}
