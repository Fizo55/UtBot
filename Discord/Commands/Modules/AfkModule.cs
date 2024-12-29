using Discord.Infrastructure;
using Discord.Infrastructure.Models;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Discord.Commands.Modules;

public class AfkNotificationHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<AfkNotificationHandler> _logger;
    private readonly IDatabaseHandler _dbHandler;

    public AfkNotificationHandler(DiscordSocketClient client, ILogger<AfkNotificationHandler> logger, IDatabaseHandler dbHandler)
    {
        _client = client;
        _logger = logger;
        _dbHandler = dbHandler;

        _client.MessageReceived += OnMessageReceivedAsync;
    }

    [SlashCommand("subscribe", "Get notified when a specific user comes back")]
    public async Task SubscribeAsync(string userId)
    {
        if (!ulong.TryParse(userId, out var id))
        {
            await RespondAsync("Provide a valid UserId.", ephemeral: true);
            return;
        }
        
        await DeferAsync();

        var afkGuild = _client.GetGuild(1202299865259053096);
        await afkGuild.DownloadUsersAsync();

        var mem = afkGuild?.GetUser(id);
        if (mem == null)
        {
            await FollowupAsync(
                "User not found."
            );
            return;
        }

        var tracked = (await _dbHandler.GetAsync<Subscribe>(s => s.TrackedUser == id)).FirstOrDefault();

        if (tracked == null)
        {
            var subscribe = new Subscribe
            {
                SubscribedUsers = new() { Context.User.Id },
                TrackedUser = id,
                LastSeen = DateTime.MinValue
            };

            await _dbHandler.AddAsync(subscribe);
            await FollowupAsync(
                $"You will be notified when **{mem.DisplayName}** comes back (if they’re AFK for at least 30 minutes)."
            );
            return;
        }

        if (tracked.SubscribedUsers.Contains(Context.User.Id))
        {
            await FollowupAsync("You are already subscribed to this user.");
            return;
        }

        tracked.SubscribedUsers.Add(Context.User.Id);
        await _dbHandler.UpdateAsync(tracked);

        await FollowupAsync(
            $"You will be notified when **{mem.DisplayName}** comes back (if they’re AFK for at least 30 minutes)."
        );
    }

    [SlashCommand("unsubscribe", "Remove notification from the specific user")]
    public async Task UnsubscribeAsync(string userId)
    {
        if (!ulong.TryParse(userId, out var id))
        {
            await RespondAsync("Provide a valid UserId.", ephemeral: true);
            return;
        }
        
        await DeferAsync();

        var afkGuild = _client.GetGuild(1202299865259053096);
        await afkGuild.DownloadUsersAsync();

        var mem = afkGuild?.GetUser(id);
        if (mem == null)
        {
            await FollowupAsync(
                "User not found."
            );
            return;
        }

        var tracked = (await _dbHandler.GetAsync<Subscribe>(s => s.TrackedUser == id)).FirstOrDefault();

        if (tracked == null || !tracked.SubscribedUsers.Contains(Context.User.Id))
        {
            await FollowupAsync("You are not subscribed to this user.");
            return;
        }

        tracked.SubscribedUsers.Remove(Context.User.Id);
        await _dbHandler.UpdateAsync(tracked);

        await FollowupAsync($"You have unsubscribed from **{mem.DisplayName}** notifications.");
    }

    private async Task OnMessageReceivedAsync(SocketMessage arg)
    {
        try
        {
            if (arg is not SocketUserMessage message) return;

            var tracked = (await _dbHandler.GetAsync<Subscribe>(s => s.TrackedUser == message.Author.Id)).FirstOrDefault();
            if (tracked == null) return;

            var nowUtc = DateTime.UtcNow;
            var timeSinceLast = nowUtc - tracked.LastSeen;

            if (timeSinceLast.TotalMinutes >= 30 && tracked.SubscribedUsers.Count > 0)
            {
                var mentions = string.Join(" ", tracked.SubscribedUsers.Select(u => $"<@{u}>"));

                var afkGuild = _client.GetGuild(1202299865259053096);
                await afkGuild.DownloadUsersAsync();

                var mem = afkGuild?.GetUser(tracked.TrackedUser);
                if (mem == null)
                {
                    await FollowupAsync(
                        "User not found."
                    );
                    return;
                }

                ulong notificationChannelId = 1275033665138720850;
                if (_client.GetChannel(notificationChannelId) is IMessageChannel notificationChannel)
                {
                    await notificationChannel.SendMessageAsync(
                        $"Hey {mentions}, {mem.DisplayName} is back."
                    );
                }
                else
                {
                    _logger.LogError(
                        "Notification channel with ID {ChannelId} not found.",
                        notificationChannelId
                    );
                }
            }

            tracked.LastSeen = nowUtc;
            await _dbHandler.UpdateAsync(tracked);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnMessageReceivedAsync");
        }
    }
}