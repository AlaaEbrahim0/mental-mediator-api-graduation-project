using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

/// <summary>
/// A page request for the room directory. <see cref="NowUtc"/> travels with the
/// query because "active" and "scheduled" are relative to the caller's clock,
/// and evaluating them inside the repository would make the predicate
/// untestable against a fixed time.
/// </summary>
public sealed record RoomBrowseQuery(
	string? Search,
	RoomBrowseTab Tab,
	RoomBrowseSort Sort,
	DateTime NowUtc,
	int Skip,
	int Take)
{
	public static readonly TimeSpan ActiveWindow = TimeSpan.FromHours(24);

	public DateTime ActiveSinceUtc => NowUtc.Subtract(ActiveWindow);
}

public interface IChatRoomRepository
{
	Task<ChatRoom?> GetRoomByIdAsync(int roomId, bool trackChanges);

	/// <summary>
	/// Rooms a signed-in user may see in a list. Private rooms are included:
	/// visibility gates the conversation, not the listing.
	/// </summary>
	Task<IReadOnlyList<ChatRoom>> GetListableRoomsAsync(int? beforeRoomId, int limit, bool trackChanges);

	Task<(IReadOnlyList<ChatRoom> Rooms, int TotalCount)> BrowseRoomsAsync(RoomBrowseQuery query);

	Task<IReadOnlyList<ChatRoom>> GetRoomsCreatedAsync(string userId, bool includeArchived, int limit);

	Task<int> CountRoomsCreatedAsync(string userId, bool includeArchived);

	Task<IReadOnlyList<ChatRoom>> GetUpcomingScheduledAsync(IReadOnlyCollection<int> roomIds, DateTime fromUtc, int limit);

	Task<ChatRoomMember?> GetOwnerAsync(int roomId, bool trackChanges);

	Task<IReadOnlyDictionary<int, ChatRoomMember>> GetOwnersByRoomIdsAsync(IReadOnlyCollection<int> roomIds);

	Task<ChatRoomMember?> GetMembershipAsync(int roomId, string userId, bool trackChanges);
	Task<ChatRoomMember?> GetMembershipByIdAsync(int roomId, int memberId, bool trackChanges);
	Task<IReadOnlyList<ChatRoomMember>> GetActiveMembersAsync(int roomId, bool trackChanges);

	/// <summary>
	/// Memberships created in <paramref name="roomIds"/> since a cutoff, newest
	/// first. Backs the doctor activity feed; only joins that are still active
	/// count, because a leave is not an event worth showing twice.
	/// </summary>
	Task<IReadOnlyList<ChatRoomMember>> GetRecentJoinsAsync(
		IReadOnlyCollection<int> roomIds,
		DateTime sinceUtc,
		int take);
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
