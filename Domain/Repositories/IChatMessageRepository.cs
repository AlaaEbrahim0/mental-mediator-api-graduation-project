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
	/// Persists the message unless the sender already used the same client message id.
	/// Returns null when the message was created, or the existing message when the
	/// unique index rejected a concurrent duplicate.
	/// </summary>
	Task<ChatMessage?> CreateMessageIfAbsentAsync(ChatMessage message);

	void CreateMessage(ChatMessage message);
	void UpdateMessage(ChatMessage message);
}
