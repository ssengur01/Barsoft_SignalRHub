using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Barsoft.SignalRHub.SignalRHub.Hubs;

/// <summary>
/// SignalR Hub for real-time stock movement notifications
/// Requires JWT authentication
/// Implements multi-tenant filtering via SignalR Groups
/// </summary>
[Authorize]
public class StokHareketHub : Hub<IStokHareketHubClient>
{
    private readonly ILogger<StokHareketHub> _logger;

    public StokHareketHub(ILogger<StokHareketHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// Automatically joins user to their authorized branch groups
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userCode = Context.User?.FindFirst("userCode")?.Value;
        var subeIdsString = Context.User?.FindFirst("subeIds")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userCode))
        {
            _logger.LogWarning("User connected without valid claims. ConnectionId: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
            return;
        }

        _logger.LogInformation(
            "User connected: {UserCode} (ID: {UserId}), ConnectionId: {ConnectionId}",
            userCode, userId, Context.ConnectionId);

        // Add user to their branch groups for multi-tenant filtering
        if (!string.IsNullOrWhiteSpace(subeIdsString))
        {
            var subeIds = ParseSubeIds(subeIdsString);

            foreach (var subeId in subeIds)
            {
                var groupName = $"sube_{subeId}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                _logger.LogDebug("Added connection {ConnectionId} to group {GroupName}", Context.ConnectionId, groupName);
            }

            _logger.LogInformation(
                "User {UserCode} joined {Count} branch groups: [{SubeIds}]",
                userCode, subeIds.Count, string.Join(", ", subeIds));
        }
        else
        {
            _logger.LogWarning("User {UserCode} has no branch access (subeIds empty)", userCode);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userCode = Context.User?.FindFirst("userCode")?.Value ?? "Unknown";

        if (exception != null)
        {
            _logger.LogWarning(exception,
                "User {UserCode} disconnected with error. ConnectionId: {ConnectionId}",
                userCode, Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation(
                "User {UserCode} disconnected. ConnectionId: {ConnectionId}",
                userCode, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client can call this method to test connection
    /// Returns server timestamp
    /// </summary>
    public async Task<string> Ping()
    {
        var userCode = Context.User?.FindFirst("userCode")?.Value ?? "Unknown";
        _logger.LogDebug("Ping received from user: {UserCode}", userCode);
        return await Task.FromResult($"Pong from server at {DateTime.UtcNow:O}");
    }

    /// <summary>
    /// Client can call this method to get their group memberships
    /// Useful for debugging multi-tenant setup
    /// </summary>
    public async Task<object> GetMyGroups()
    {
        var userCode = Context.User?.FindFirst("userCode")?.Value ?? "Unknown";
        var subeIdsString = Context.User?.FindFirst("subeIds")?.Value;
        var subeIds = ParseSubeIds(subeIdsString ?? "");

        var groups = subeIds.Select(id => $"sube_{id}").ToList();

        _logger.LogDebug("User {UserCode} requested group info: {Groups}", userCode, string.Join(", ", groups));

        return await Task.FromResult(new
        {
            UserCode = userCode,
            SubeIds = subeIds,
            Groups = groups,
            ConnectionId = Context.ConnectionId
        });
    }

    /// <summary>
    /// Parses comma-separated subeIds string to list of integers
    /// </summary>
    private List<int> ParseSubeIds(string subeIdsString)
    {
        if (string.IsNullOrWhiteSpace(subeIdsString))
            return new List<int>();

        return subeIdsString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
            .Where(x => x > 0)
            .ToList();
    }
}

/// <summary>
/// Strongly-typed client interface for StokHareketHub
/// Defines methods that can be called on clients
/// </summary>
public interface IStokHareketHubClient
{
    /// <summary>
    /// Called when a new stock movement is created
    /// </summary>
    Task StokHareketCreated(object @event);

    /// <summary>
    /// Called when a stock movement is updated
    /// </summary>
    Task StokHareketUpdated(object @event);

    /// <summary>
    /// Called when any stock movement event occurs (created or updated)
    /// </summary>
    Task StokHareketReceived(object @event);
}
