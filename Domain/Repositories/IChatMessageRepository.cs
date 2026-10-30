using Domain.Entities;

namespace Domain.Repositories;

public interface IChatMessageRepository
{
	Task<ChatMessage?> GetMessageByIdAsync(long messageId, bool trackChanges);
	Task<ChatMessage?> GetByClientMessageIdAsync(string senderId, Guid clientMessageId, bool trackChanges);
	Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(int roomId, long? beforeId, long? afterId, int limit, bool trackChanges);
	Task<ChatMessage?> GetLatestMessageAsync(int roomId, bool trackChanges);
	Task<IReadOnlyDictionary<int, ChatMessage>> GetLatestMessagesAsync(IReadOnlyCollection<int> roomIds, bool trackChanges);

	/// <summary>
	/// Per-room count of messages newer than the caller's last read mark,
	/// excluding the caller's own messages and deleted rows. Only active
	/// memberships are considered; other rooms are absent from the result.
	/// </summary>
	Task<IReadOnlyDictionary<int, int>> GetUnreadCountsAsync(string userId, IReadOnlyCollection<int> roomIds);

	/// <summary>
	/// Persists the message unless the sender already used the same client message id.
	/// Returns null when the message was created, or the existing message when the
	/// unique index rejected a concurrent duplicate.
	/// </summary>
	Task<ChatMessage?> CreateMessageIfAbsentAsync(ChatMessage message);

	void CreateMessage(ChatMessage message);
	void UpdateMessage(ChatMessage message);
}
