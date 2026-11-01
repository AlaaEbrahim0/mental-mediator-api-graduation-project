using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

public interface IChatRoomInvitationRepository
{
	void Create(ChatRoomInvitation invitation);

	void Update(ChatRoomInvitation invitation);

	Task<ChatRoomInvitation?> GetByIdAsync(int id, bool trackChanges);

	/// <summary>The single pending invitation for a user in a room, if any.</summary>
	Task<ChatRoomInvitation?> GetPendingAsync(int roomId, string userId, bool trackChanges);

	/// <summary>Already-pending invitee ids in a room, used to skip duplicates.</summary>
	Task<IReadOnlyCollection<string>> GetPendingUserIdsAsync(int roomId);

	Task<IReadOnlyList<ChatRoomInvitation>> GetPendingForRoomAsync(int roomId, bool trackChanges);

	Task<IReadOnlyList<ChatRoomInvitation>> GetPendingForUserAsync(string userId, bool trackChanges);

	/// <summary>
	/// Non-member accounts to offer as invitation targets, matched on name and
	/// capped. Only ever called after the inviter's room permission is checked,
	/// so it cannot be used to enumerate the user base at large.
	/// </summary>
	Task<IReadOnlyList<BaseUser>> SearchInviteCandidatesAsync(
		string? search,
		IReadOnlyCollection<string> excludeUserIds,
		int take);
}
