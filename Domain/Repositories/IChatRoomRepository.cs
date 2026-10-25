using Domain.Entities;

namespace Domain.Repositories;

public interface IChatRoomRepository
{
	Task<ChatRoom?> GetRoomByIdAsync(int roomId, bool trackChanges);
	Task<IReadOnlyList<ChatRoom>> GetPublicRoomsAsync(int? beforeRoomId, int limit, bool trackChanges);
	Task<ChatRoomMember?> GetMembershipAsync(int roomId, string userId, bool trackChanges);
	Task<ChatRoomMember?> GetMembershipByIdAsync(int roomId, int memberId, bool trackChanges);
	Task<IReadOnlyList<ChatRoomMember>> GetActiveMembersAsync(int roomId, bool trackChanges);
	Task<IReadOnlyList<ChatRoomMember>> GetMembershipsAsync(string userId, IReadOnlyCollection<int> roomIds, bool trackChanges);
	Task<int> GetActiveMemberCountAsync(int roomId);
	Task<IReadOnlyDictionary<int, int>> GetActiveMemberCountsAsync(IReadOnlyCollection<int> roomIds);
	void CreateRoom(ChatRoom room);
	void UpdateRoom(ChatRoom room);
	void CreateMembership(ChatRoomMember member);
	void UpdateMembership(ChatRoomMember member);

	/// <summary>
	/// Persists the membership unless a concurrent request already created one
	/// for the same room and user. Returns null when the membership was created,
	/// or the existing row when the unique index rejected a duplicate.
	/// </summary>
	Task<ChatRoomMember?> CreateMembershipIfAbsentAsync(ChatRoomMember member);
}
