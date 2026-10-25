namespace Infrastructure.Hubs;

public static class ChatGroups
{
	// Group names contain only the room id, never a user id or account data.
	public static string Room(int roomId) => $"chat-room-{roomId}";
}
