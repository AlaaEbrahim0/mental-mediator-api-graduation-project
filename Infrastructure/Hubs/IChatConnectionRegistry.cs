namespace Infrastructure.Hubs;

/// <summary>
/// Tracks which chat connections belong to a user and which rooms each
/// connection has subscribed to. This is process-local, matching the current
/// single-instance SignalR deployment; a multi-instance deployment needs a
/// backplane and a shared registry.
/// </summary>
public interface IChatConnectionRegistry
{
	void AddConnection(string connectionId, string userId);

	void RemoveConnection(string connectionId);

	void AddSubscription(string connectionId, int roomId);

	void RemoveSubscription(string connectionId, int roomId);

	bool IsSubscribed(string connectionId, int roomId);

	IReadOnlyCollection<string> GetConnections(string userId);
}
