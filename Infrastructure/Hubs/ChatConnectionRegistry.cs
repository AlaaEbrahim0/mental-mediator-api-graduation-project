using System.Collections.Concurrent;

namespace Infrastructure.Hubs;

public class ChatConnectionRegistry : IChatConnectionRegistry
{
	private sealed class ConnectionState
	{
		public required string UserId { get; init; }

		public ConcurrentDictionary<int, byte> Rooms { get; } = new();
	}

	private readonly ConcurrentDictionary<string, ConnectionState> _connections = new();
	private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userConnections = new();

	public void AddConnection(string connectionId, string userId)
	{
		var state = new ConnectionState { UserId = userId };

		if (!_connections.TryAdd(connectionId, state))
		{
			return;
		}

		var connections = _userConnections.GetOrAdd(
			userId,
			_ => new ConcurrentDictionary<string, byte>());

		connections[connectionId] = 0;
	}

	public void RemoveConnection(string connectionId)
	{
		if (!_connections.TryRemove(connectionId, out var state))
		{
			return;
		}

		if (!_userConnections.TryGetValue(state.UserId, out var connections))
		{
			return;
		}

		connections.TryRemove(connectionId, out _);

		if (connections.IsEmpty)
		{
			_userConnections.TryRemove(state.UserId, out _);
		}
	}

	public void AddSubscription(string connectionId, int roomId)
	{
		if (_connections.TryGetValue(connectionId, out var state))
		{
			state.Rooms[roomId] = 0;
		}
	}

	public void RemoveSubscription(string connectionId, int roomId)
	{
		if (_connections.TryGetValue(connectionId, out var state))
		{
			state.Rooms.TryRemove(roomId, out _);
		}
	}

	public bool IsSubscribed(string connectionId, int roomId) =>
		_connections.TryGetValue(connectionId, out var state) &&
		state.Rooms.ContainsKey(roomId);

	public IReadOnlyCollection<string> GetConnections(string userId) =>
		_userConnections.TryGetValue(userId, out var connections)
			? connections.Keys.ToList()
			: Array.Empty<string>();
}
