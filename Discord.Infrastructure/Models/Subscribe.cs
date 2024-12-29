namespace Discord.Infrastructure.Models;

public class Subscribe
{
    public Guid Id { get; set; }
    
    public List<ulong> SubscribedUsers { get; set; }
    
    public ulong TrackedUser { get; set; }
    
    public DateTime LastSeen { get; set; }
}