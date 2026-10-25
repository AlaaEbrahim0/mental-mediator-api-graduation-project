using System.Security.Cryptography;
using Application.Contracts;
using Application.Dtos.ChatDtos;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Microsoft.AspNetCore.Identity;
using Shared;

namespace Application.Services;

public class ChatService : IChatService
{
	private const int MaxPreviewLength = 120;

	private readonly IRepositoryManager _repos;
	private readonly IUserClaimsService _userClaimsService;
	private readonly UserManager<BaseUser> _userManager;
	private readonly IChatContentModerator _contentModerator;
	private readonly IChatRealtimeNotifier _realtimeNotifier;

	public ChatService(
		IRepositoryManager repos,
		IUserClaimsService userClaimsService,
		UserManager<BaseUser> userManager,
		IChatContentModerator contentModerator,
		IChatRealtimeNotifier realtimeNotifier)
	{
		_repos = repos;
		_userClaimsService = userClaimsService;
		_userManager = userManager;
		_contentModerator = contentModerator;
		_realtimeNotifier = realtimeNotifier;
	}

	public async Task<Result<ChatRoomPageResponse>> GetRoomsAsync(ChatRoomRequestParameters parameters)
	{
		var currentUser = await GetCurrentUserAsync();
		if (currentUser.IsFailure)
		{
			return currentUser.Error;
		}

		var rooms = await _repos.ChatRooms.GetPublicRoomsAsync(
			parameters.BeforeRoomId,
			parameters.Limit + 1,
			false);

		var hasMore = rooms.Count > parameters.Limit;
		var page = rooms.Take(parameters.Limit).ToList();
		var roomIds = page.Select(x => x.Id).ToList();

		var memberships = await _repos.ChatRooms.GetMembershipsAsync(currentUser.Value.Id, roomIds, false);
		var membershipByRoom = memberships.ToDictionary(x => x.RoomId);
		var memberCounts = await _repos.ChatRooms.GetActiveMemberCountsAsync(roomIds);

		// Only load conversation content for rooms the user is actually in.
		var memberRoomIds = memberships
			.Where(x => x.LeftAtUtc is null)
			.Select(x => x.RoomId)
			.ToList();
		var latestMessages = await _repos.ChatMessages.GetLatestMessagesAsync(memberRoomIds, false);

		var response = new ChatRoomPageResponse
		{
			Rooms = page.Select(room =>
			{
				membershipByRoom.TryGetValue(room.Id, out var membership);
				memberCounts.TryGetValue(room.Id, out var memberCount);
				latestMessages.TryGetValue(room.Id, out var latestMessage);

				return MapRoom(room, membership, memberCount, latestMessage);
			}).ToList(),
			HasMore = hasMore,
			NextBeforeRoomId = hasMore && page.Count > 0 ? page[^1].Id : null
		};

		return response;
	}

	public async Task<Result<ChatRoomResponse>> GetRoomAsync(int roomId)
	{
		var currentUser = await GetCurrentUserAsync();
		if (currentUser.IsFailure)
		{
			return currentUser.Error;
		}

		var room = await _repos.ChatRooms.GetRoomByIdAsync(roomId, false);
		if (room is null || room.Visibility != ChatRoomVisibility.Public)
		{
			return ChatErrors.RoomNotFound();
		}

		var membership = await _repos.ChatRooms.GetMembershipAsync(roomId, currentUser.Value.Id, false);
		var isActiveMember = IsActiveMember(membership);

		if (room.IsArchived && !isActiveMember)
		{
			return ChatErrors.RoomNotFound();
		}

		var memberCount = await _repos.ChatRooms.GetActiveMemberCountAsync(roomId);
		var latestMessage = isActiveMember
			? await _repos.ChatMessages.GetLatestMessageAsync(roomId, false)
			: null;

		return MapRoom(room, membership, memberCount, latestMessage);
	}

	public async Task<Result<ChatRoomResponse>> EnsureActiveMemberAsync(int roomId)
	{
		var currentUser = await GetCurrentUserAsync();
		if (currentUser.IsFailure)
		{
			return currentUser.Error;
		}

		var room = await _repos.ChatRooms.GetRoomByIdAsync(roomId, false);
		if (room is null || room.Visibility != ChatRoomVisibility.Public)
		{
			return ChatErrors.RoomNotFound();
		}

		var membership = await _repos.ChatRooms.GetMembershipAsync(
			roomId,
			currentUser.Value.Id,
			false);

		if (!IsActiveMember(membership))
		{
			return ChatErrors.RoomNotFound();
		}

		var memberCount = await _repos.ChatRooms.GetActiveMemberCountAsync(roomId);
		var latestMessage = await _repos.ChatMessages.GetLatestMessageAsync(roomId, false);

		return MapRoom(room, membership, memberCount, latestMessage);
	}

	public async Task<Result<ChatRoomResponse>> CreateRoomAsync(CreateChatRoomRequest request)
	{
		var chatUserResult = await GetCurrentChatUserAsync();
		if (chatUserResult.IsFailure)
		{
			return chatUserResult.Error;
		}

		var chatUser = chatUserResult.Value;
		if (!chatUser.IsDoctor && !chatUser.IsAdmin)
		{
			return ChatErrors.CreateRoomForbidden();
		}

		var name = (request.Name ?? string.Empty).Trim();
		if (name.Length == 0 || name.Length > 100 || name.Any(char.IsControl))
		{
			return ChatErrors.InvalidRoomName();
		}

		var description = request.Description?.Trim() ?? string.Empty;
		if (description.Length > 500 ||
			description.Any(character =>
				char.IsControl(character) && character is not '\n' and not '\r' and not '\t'))
		{
			return ChatErrors.InvalidRoomDescription();
		}

		var now = DateTime.UtcNow;
		var room = new ChatRoom
		{
			Name = name,
			Description = description,
			Visibility = ChatRoomVisibility.Public,
			CreatedById = chatUser.User.Id,
			CreatedAtUtc = now,
			IsArchived = false
		};

		var owner = new ChatRoomMember
		{
			Room = room,
			UserId = chatUser.User.Id,
			Role = ChatRoomRole.Owner,
			DisplayName = GetNewMemberDisplayName(chatUser),
			IsDoctor = chatUser.IsDoctor,
			JoinedAtUtc = now
		};

		_repos.ChatRooms.CreateRoom(room);
		_repos.ChatRooms.CreateMembership(owner);
		await _repos.SaveAsync();

		return MapRoom(room, owner, 1, null);
	}

	public async Task<Result<ChatRoomMemberResponse>> JoinRoomAsync(int roomId)
	{
		var chatUserResult = await GetCurrentChatUserAsync();
		if (chatUserResult.IsFailure)
		{
			return chatUserResult.Error;
		}

		var chatUser = chatUserResult.Value;
		var room = await _repos.ChatRooms.GetRoomByIdAsync(roomId, false);
		if (room is null || room.Visibility != ChatRoomVisibility.Public)
		{
			return ChatErrors.RoomNotFound();
		}

		if (room.IsArchived)
		{
			return ChatErrors.RoomArchived();
		}

		var membership = await _repos.ChatRooms.GetMembershipAsync(roomId, chatUser.User.Id, true);

		if (membership is not null)
		{
			var displayName = ResolveDisplayName(chatUser, membership);
			var role = ResolveRole(chatUser, membership);
			var changed = membership.LeftAtUtc is not null ||
				membership.DisplayName != displayName ||
				membership.IsDoctor != chatUser.IsDoctor ||
				membership.Role != role;

			if (changed)
			{
				membership.LeftAtUtc = null;
				membership.DisplayName = displayName;
				membership.IsDoctor = chatUser.IsDoctor;
				membership.Role = role;
				_repos.ChatRooms.UpdateMembership(membership);
				await _repos.SaveAsync();

				await _realtimeNotifier.MemberUpdatedAsync(roomId, MapMember(membership));
			}

			return MapMember(membership);
		}

		membership = new ChatRoomMember
		{
			RoomId = roomId,
			UserId = chatUser.User.Id,
			Role = ChatRoomRole.Member,
			DisplayName = GetNewMemberDisplayName(chatUser),
			IsDoctor = chatUser.IsDoctor,
			JoinedAtUtc = DateTime.UtcNow
		};

		var concurrentMembership = await _repos.ChatRooms.CreateMembershipIfAbsentAsync(membership);

		if (concurrentMembership is null)
		{
			// This request created the row. A concurrent request that lost the
			// unique-index race returns the existing row and does not rebroadcast.
			await _realtimeNotifier.MemberUpdatedAsync(roomId, MapMember(membership));
		}

		return MapMember(concurrentMembership ?? membership);
	}

	public async Task<Result> LeaveRoomAsync(int roomId)
	{
		var currentUser = await GetCurrentUserAsync();
		if (currentUser.IsFailure)
		{
			return currentUser.Error;
		}

		var room = await _repos.ChatRooms.GetRoomByIdAsync(roomId, false);
		if (room is null || room.Visibility != ChatRoomVisibility.Public)
		{
			return ChatErrors.RoomNotFound();
		}

		var membership = await _repos.ChatRooms.GetMembershipAsync(roomId, currentUser.Value.Id, true);
		if (membership is null || membership.LeftAtUtc is not null)
		{
			return Result.Success();
		}

		if (membership.Role == ChatRoomRole.Owner)
		{
			return ChatErrors.OwnerCannotLeave();
		}

		membership.LeftAtUtc = DateTime.UtcNow;
		_repos.ChatRooms.UpdateMembership(membership);
		await _repos.SaveAsync();

		await _realtimeNotifier.MemberLeftAsync(
			roomId,
			membership.Id,
			currentUser.Value.Id);

		return Result.Success();
	}

	public async Task<Result<IReadOnlyList<ChatRoomMemberResponse>>> GetRoomMembersAsync(int roomId)
	{
		var currentUser = await GetCurrentUserAsync();
		if (currentUser.IsFailure)
		{
			return currentUser.Error;
		}

		var room = await _repos.ChatRooms.GetRoomByIdAsync(roomId, false);
		if (room is null || room.Visibility != ChatRoomVisibility.Public)
		{
			return ChatErrors.RoomNotFound();
		}

		var membership = await _repos.ChatRooms.GetMembershipAsync(roomId, currentUser.Value.Id, false);
		if (!IsActiveMember(membership))
		{
			return ChatErrors.RoomNotFound();
		}

		var members = await _repos.ChatRooms.GetActiveMembersAsync(roomId, false);
		IReadOnlyList<ChatRoomMemberResponse> response = members.Select(MapMember).ToList();
		return Result<IReadOnlyList<ChatRoomMemberResponse>>.Success(response);
	}

	public async Task<Result<ChatMessagePageResponse>> GetMessagesAsync(int roomId, ChatMessageRequestParameters parameters)
	{
		var currentUser = await GetCurrentUserAsync();
		if (currentUser.IsFailure)
		{
			return currentUser.Error;
		}

		if (parameters.BeforeId.HasValue && parameters.AfterId.HasValue)
		{
			return ChatErrors.InvalidMessagePagination();
		}

		var room = await _repos.ChatRooms.GetRoomByIdAsync(roomId, false);
		if (room is null || room.Visibility != ChatRoomVisibility.Public)
		{
			return ChatErrors.RoomNotFound();
		}

		var membership = await _repos.ChatRooms.GetMembershipAsync(roomId, currentUser.Value.Id, false);
		if (!IsActiveMember(membership))
		{
			// Rooms are public, but their conversation is only visible to active
			// members. Returning 404 avoids confirming membership state.
			return ChatErrors.RoomNotFound();
		}

		var messages = await _repos.ChatMessages.GetMessagesAsync(
			roomId,
			parameters.BeforeId,
			parameters.AfterId,
			parameters.Limit + 1,
			false);

		var hasMore = messages.Count > parameters.Limit;

		// The repository returns pages in ascending order. For latest/before
		// pages the extra row is the oldest one, so keep the newest window.
		// For after pages the extra row is the newest one, so keep the first
		// window. This is what lets recovery callers request everything newer
		// than the last message they saw.
		var page = parameters.AfterId.HasValue
			? messages.Take(parameters.Limit).ToList()
			: messages.Skip(Math.Max(0, messages.Count - parameters.Limit)).ToList();

		var response = new ChatMessagePageResponse
		{
			Messages = page.Select(message => MapMessage(message)).ToList(),
			HasMore = hasMore,
			NextBeforeId = !parameters.AfterId.HasValue && hasMore && page.Count > 0
				? page[0].Id
				: null,
			NextAfterId = parameters.AfterId.HasValue && hasMore && page.Count > 0
				? page[^1].Id
				: null
		};

		return response;
	}

	public async Task<Result<ChatMessageResponse>> SendMessageAsync(int roomId, SendChatMessageRequest request)
	{
		var currentUser = await GetCurrentUserAsync();
		if (currentUser.IsFailure)
		{
			return currentUser.Error;
		}

		if (request.ClientMessageId == Guid.Empty)
		{
			return ChatErrors.InvalidClientMessageId();
		}

		var room = await _repos.ChatRooms.GetRoomByIdAsync(roomId, false);
		if (room is null || room.Visibility != ChatRoomVisibility.Public)
		{
			return ChatErrors.RoomNotFound();
		}

		if (room.IsArchived)
		{
			return ChatErrors.RoomArchived();
		}

		var membership = await _repos.ChatRooms.GetMembershipAsync(roomId, currentUser.Value.Id, false);
		if (!IsActiveMember(membership))
		{
			return ChatErrors.RoomNotFound();
		}

		var content = request.Content?.Trim() ?? string.Empty;
		if (content.Length == 0 || content.Length > 2000)
		{
			return ChatErrors.InvalidMessageContent();
		}

		// A retry with the same client message id must not call the model again.
		// Compare the stored message first, then only moderate genuinely new
		// content.
		var existing = await _repos.ChatMessages.GetByClientMessageIdAsync(
			currentUser.Value.Id,
			request.ClientMessageId,
			false);

		if (existing is not null)
		{
			return HandleDuplicateMessage(existing, roomId, content);
		}

		var moderation = await _contentModerator.EnsureAllowedAsync(content);
		if (moderation.IsFailure)
		{
			return moderation.Error;
		}

		// Moderation can take up to a few seconds. Recheck the room state before
		// persisting so an archive that happened during moderation is respected.
		var currentRoom = await _repos.ChatRooms.GetRoomByIdAsync(roomId, false);
		if (currentRoom is null)
		{
			return ChatErrors.RoomNotFound();
		}

		if (currentRoom.IsArchived)
		{
			return ChatErrors.RoomArchived();
		}

		var message = new ChatMessage
		{
			RoomId = roomId,
			SenderId = currentUser.Value.Id,
			SenderMemberId = membership!.Id,
			SenderDisplayName = membership.DisplayName,
			SenderIsDoctor = membership.IsDoctor,
			Content = content,
			ClientMessageId = request.ClientMessageId,
			SentAtUtc = DateTime.UtcNow,
			IsDeleted = false
		};

		// The unique index on (SenderId, ClientMessageId) is the final guard
		// against two concurrent retries both passing the check above.
		var duplicate = await _repos.ChatMessages.CreateMessageIfAbsentAsync(message);
		if (duplicate is not null)
		{
			return HandleDuplicateMessage(duplicate, roomId, content);
		}

		var trackedRoom = await _repos.ChatRooms.GetRoomByIdAsync(roomId, true);
		if (trackedRoom is not null &&
			(trackedRoom.LastMessageAtUtc is null || trackedRoom.LastMessageAtUtc < message.SentAtUtc))
		{
			trackedRoom.LastMessageAtUtc = message.SentAtUtc;
			await _repos.SaveAsync();
		}

		// Persist before broadcast. The notifier logs and swallows delivery
		// failures so the command still succeeds; recovery uses the REST
		// history endpoint.
		await _realtimeNotifier.MessageCreatedAsync(roomId, MapMessage(message));

		return MapMessage(message);
	}

	public async Task<Result<ChatMessageResponse>> DeleteMessageAsync(long messageId)
	{
		var currentUserResult = await GetCurrentChatUserAsync();
		if (currentUserResult.IsFailure)
		{
			return currentUserResult.Error;
		}

		var currentUser = currentUserResult.Value;
		var message = await _repos.ChatMessages.GetMessageByIdAsync(messageId, true);
		if (message is null)
		{
			return ChatErrors.MessageNotFound();
		}

		var room = await _repos.ChatRooms.GetRoomByIdAsync(message.RoomId, false);
		if (room is null || room.Visibility != ChatRoomVisibility.Public)
		{
			return ChatErrors.MessageNotFound();
		}

		var membership = await _repos.ChatRooms.GetMembershipAsync(
			message.RoomId,
			currentUser.User.Id,
			false);

		var isSender = string.Equals(message.SenderId, currentUser.User.Id, StringComparison.Ordinal);
		if (!isSender && !await CanModerateAsync(membership, currentUser.User))
		{
			if (!IsActiveMember(membership))
			{
				return ChatErrors.MessageNotFound();
			}

			return ChatErrors.MessageForbidden();
		}

		if (message.IsDeleted)
		{
			return MapMessage(message);
		}

		message.IsDeleted = true;
		message.DeletedAtUtc = DateTime.UtcNow;
		message.DeletedById = currentUser.User.Id;
		_repos.ChatMessages.UpdateMessage(message);
		await _repos.SaveAsync();

		await _realtimeNotifier.MessageDeletedAsync(message.RoomId, MapMessage(message));

		return MapMessage(message);
	}

	public async Task<Result<ChatMessageReportResponse>> ReportMessageAsync(long messageId, ReportChatMessageRequest request)
	{
		var currentUser = await GetCurrentUserAsync();
		if (currentUser.IsFailure)
		{
			return currentUser.Error;
		}

		var message = await _repos.ChatMessages.GetMessageByIdAsync(messageId, false);
		if (message is null)
		{
			return ChatErrors.MessageNotFound();
		}

		var room = await _repos.ChatRooms.GetRoomByIdAsync(message.RoomId, false);
		if (room is null || room.Visibility != ChatRoomVisibility.Public)
		{
			return ChatErrors.MessageNotFound();
		}

		var membership = await _repos.ChatRooms.GetMembershipAsync(
			message.RoomId,
			currentUser.Value.Id,
			false);

		if (!IsActiveMember(membership))
		{
			return ChatErrors.MessageNotFound();
		}

		if (string.Equals(message.SenderId, currentUser.Value.Id, StringComparison.Ordinal))
		{
			return ChatErrors.CannotReportOwnMessage();
		}

		if (message.IsDeleted)
		{
			return ChatErrors.MessageDeleted();
		}

		var existingReport = await _repos.ChatMessageReports.GetReportAsync(
			messageId,
			currentUser.Value.Id,
			false);

		if (existingReport is not null)
		{
			return ChatErrors.ReportAlreadyExists();
		}

		var reason = request.Reason?.Trim() ?? string.Empty;
		if (reason.Length == 0 || reason.Length > 1000)
		{
			return ChatErrors.InvalidReportReason();
		}

		var report = new ChatMessageReport
		{
			MessageId = messageId,
			ReporterId = currentUser.Value.Id,
			Reason = reason,
			Status = ChatReportStatus.Open,
			CreatedAtUtc = DateTime.UtcNow
		};

		var concurrentReport = await _repos.ChatMessageReports.CreateReportIfAbsentAsync(report);
		if (concurrentReport is not null)
		{
			return ChatErrors.ReportAlreadyExists();
		}

		return MapReport(report, message, false);
	}

	public async Task<Result<IReadOnlyList<ChatMessageReportResponse>>> GetRoomReportsAsync(int roomId, ChatReportRequestParameters parameters)
	{
		var currentUserResult = await GetCurrentChatUserAsync();
		if (currentUserResult.IsFailure)
		{
			return currentUserResult.Error;
		}

		var currentUser = currentUserResult.Value;
		var room = await _repos.ChatRooms.GetRoomByIdAsync(roomId, false);
		if (room is null || room.Visibility != ChatRoomVisibility.Public)
		{
			return ChatErrors.RoomNotFound();
		}

		var membership = await _repos.ChatRooms.GetMembershipAsync(roomId, currentUser.User.Id, false);
		if (!IsActiveMember(membership))
		{
			return ChatErrors.RoomNotFound();
		}

		if (!await CanModerateAsync(membership, currentUser.User))
		{
			return ChatErrors.ModeratorRequired();
		}

		var reports = await _repos.ChatMessageReports.GetReportsForRoomAsync(
			roomId,
			parameters.Status,
			parameters.Limit,
			false);

		var response = new List<ChatMessageReportResponse>();

		foreach (var report in reports)
		{
			if (report.Message is null)
			{
				continue;
			}

			response.Add(MapReport(report, report.Message, true));
		}

		return Result<IReadOnlyList<ChatMessageReportResponse>>.Success(response);
	}

	public async Task<Result<ChatMessageReportResponse>> UpdateReportStatusAsync(
		int roomId,
		int reportId,
		UpdateChatMessageReportStatusRequest request)
	{
		var currentUserResult = await GetCurrentChatUserAsync();
		if (currentUserResult.IsFailure)
		{
			return currentUserResult.Error;
		}

		var currentUser = currentUserResult.Value;
		var room = await _repos.ChatRooms.GetRoomByIdAsync(roomId, false);
		if (room is null || room.Visibility != ChatRoomVisibility.Public)
		{
			return ChatErrors.RoomNotFound();
		}

		var membership = await _repos.ChatRooms.GetMembershipAsync(roomId, currentUser.User.Id, false);
		if (!IsActiveMember(membership))
		{
			return ChatErrors.RoomNotFound();
		}

		if (!await CanModerateAsync(membership, currentUser.User))
		{
			return ChatErrors.ModeratorRequired();
		}

		if (request.Status != ChatReportStatus.Reviewed &&
			request.Status != ChatReportStatus.Dismissed &&
			request.Status != ChatReportStatus.Actioned)
		{
			return ChatErrors.InvalidReportStatus();
		}

		var report = await _repos.ChatMessageReports.GetReportByIdAsync(reportId, true);
		if (report is null || report.Message is null || report.Message.RoomId != roomId)
		{
			return ChatErrors.ReportNotFound();
		}

		if (report.Status != request.Status)
		{
			report.Status = request.Status;
			report.ReviewedAtUtc = DateTime.UtcNow;
			report.ReviewedById = currentUser.User.Id;
			_repos.ChatMessageReports.UpdateReport(report);
			await _repos.SaveAsync();
		}

		return MapReport(report, report.Message, true);
	}

	public async Task<Result<ChatRoomMemberResponse>> UpdateMemberRoleAsync(
		int roomId,
		int memberId,
		UpdateChatRoomMemberRoleRequest request)
	{
		var currentUser = await GetCurrentUserAsync();
		if (currentUser.IsFailure)
		{
			return currentUser.Error;
		}

		var room = await _repos.ChatRooms.GetRoomByIdAsync(roomId, false);
		if (room is null || room.Visibility != ChatRoomVisibility.Public)
		{
			return ChatErrors.RoomNotFound();
		}

		var ownerMembership = await _repos.ChatRooms.GetMembershipAsync(roomId, currentUser.Value.Id, false);
		if (!IsActiveMember(ownerMembership))
		{
			return ChatErrors.RoomNotFound();
		}

		if (ownerMembership!.Role != ChatRoomRole.Owner)
		{
			return ChatErrors.OwnerRequired();
		}

		var target = await _repos.ChatRooms.GetMembershipByIdAsync(roomId, memberId, true);
		if (target is null || target.LeftAtUtc is not null)
		{
			return ChatErrors.MemberNotFound();
		}

		if (target.Role == ChatRoomRole.Owner)
		{
			return ChatErrors.CannotChangeOwnerRole();
		}

		// Membership carries a display snapshot, but moderation privileges must
		// follow the account's current Doctor role. A user whose role was removed
		// must not keep moderator power through an old membership row.
		var targetUser = await _userManager.FindByIdAsync(target.UserId);
		if (targetUser is null || !await _userManager.IsInRoleAsync(targetUser, "Doctor"))
		{
			return ChatErrors.ModeratorMustBeDoctor();
		}

		if (!request.Role.HasValue ||
			(request.Role.Value != ChatRoomRole.Member && request.Role.Value != ChatRoomRole.Moderator))
		{
			return ChatErrors.InvalidMemberRole();
		}

		var requestedRole = request.Role.Value;
		var displayName = Truncate(GetRealName(targetUser, "Doctor"), 100);

		if (target.Role == requestedRole &&
			target.IsDoctor &&
			target.DisplayName == displayName)
		{
			return MapMember(target);
		}

		// A doctor's membership always carries their real name and Doctor badge,
		// even when they are only a regular member of this room.
		target.Role = requestedRole;
		target.IsDoctor = true;
		target.DisplayName = displayName;

		_repos.ChatRooms.UpdateMembership(target);
		await _repos.SaveAsync();

		await _realtimeNotifier.MemberUpdatedAsync(roomId, MapMember(target));

		return MapMember(target);
	}

	public async Task<Result<ChatRoomResponse>> ArchiveRoomAsync(int roomId)
	{
		var currentUser = await GetCurrentUserAsync();
		if (currentUser.IsFailure)
		{
			return currentUser.Error;
		}

		var room = await _repos.ChatRooms.GetRoomByIdAsync(roomId, true);
		if (room is null || room.Visibility != ChatRoomVisibility.Public)
		{
			return ChatErrors.RoomNotFound();
		}

		var membership = await _repos.ChatRooms.GetMembershipAsync(roomId, currentUser.Value.Id, false);
		if (!IsActiveMember(membership))
		{
			return ChatErrors.RoomNotFound();
		}

		if (membership!.Role != ChatRoomRole.Owner)
		{
			return ChatErrors.OwnerRequired();
		}

		var changed = !room.IsArchived;

		if (changed)
		{
			room.IsArchived = true;
			_repos.ChatRooms.UpdateRoom(room);
			await _repos.SaveAsync();
		}

		var memberCount = await _repos.ChatRooms.GetActiveMemberCountAsync(roomId);
		var latestMessage = await _repos.ChatMessages.GetLatestMessageAsync(roomId, false);
		var response = MapRoom(room, membership, memberCount, latestMessage);

		if (changed)
		{
			await _realtimeNotifier.RoomArchivedAsync(roomId, response);
		}

		return response;
	}

	private async Task<Result<BaseUser>> GetCurrentUserAsync()
	{
		var userId = _userClaimsService.GetUserId();
		if (string.IsNullOrWhiteSpace(userId))
		{
			return ChatErrors.UserNotAvailable();
		}

		var user = await _userManager.FindByIdAsync(userId);
		if (user is null)
		{
			return ChatErrors.UserNotAvailable();
		}

		return user;
	}

	private async Task<Result<ChatUser>> GetCurrentChatUserAsync()
	{
		var userResult = await GetCurrentUserAsync();
		if (userResult.IsFailure)
		{
			return userResult.Error;
		}

		var roles = await _userManager.GetRolesAsync(userResult.Value);
		var isDoctor = roles.Any(role => string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase));
		var isAdmin = roles.Any(role => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase));

		return new ChatUser(userResult.Value, isDoctor, isAdmin);
	}

	private static Result<ChatMessageResponse> HandleDuplicateMessage(
		ChatMessage existing,
		int roomId,
		string content)
	{
		if (existing.RoomId != roomId ||
			!string.Equals(existing.Content, content, StringComparison.Ordinal))
		{
			return ChatErrors.ClientMessageIdConflict();
		}

		return MapMessage(existing);
	}

	private static bool IsActiveMember(ChatRoomMember? member) =>
		member is not null && member.LeftAtUtc is null;

	private async Task<bool> CanModerateAsync(ChatRoomMember? member, BaseUser currentUser)
	{
		if (!IsActiveMember(member))
		{
			return false;
		}

		if (member!.Role == ChatRoomRole.Owner)
		{
			return true;
		}

		if (member.Role != ChatRoomRole.Moderator)
		{
			return false;
		}

		return await _userManager.IsInRoleAsync(currentUser, "Doctor");
	}

	private static string GetNewMemberDisplayName(ChatUser user)
	{
		if (user.IsDoctor)
		{
			return Truncate(GetRealName(user.User, "Doctor"), 100);
		}

		if (user.IsAdmin)
		{
			return "Staff";
		}

		return CreatePatientAlias();
	}

	private static string ResolveDisplayName(ChatUser user, ChatRoomMember membership)
	{
		if (user.IsDoctor)
		{
			return Truncate(GetRealName(user.User, "Doctor"), 100);
		}

		if (user.IsAdmin)
		{
			return "Staff";
		}

		// Keep an existing patient alias stable across leave/rejoin. If the row
		// previously held a staff display name, replace it with a fresh alias
		// instead of exposing that name after a role change.
		if (!membership.IsDoctor &&
			membership.DisplayName.StartsWith("Patient ", StringComparison.Ordinal))
		{
			return membership.DisplayName;
		}

		return CreatePatientAlias();
	}

	private static ChatRoomRole ResolveRole(ChatUser user, ChatRoomMember membership)
	{
		if (membership.Role == ChatRoomRole.Owner)
		{
			return ChatRoomRole.Owner;
		}

		if (membership.Role == ChatRoomRole.Moderator && user.IsDoctor)
		{
			return ChatRoomRole.Moderator;
		}

		return ChatRoomRole.Member;
	}

	private static string GetRealName(BaseUser user, string fallback)
	{
		var fullName = $"{user.FirstName} {user.LastName}".Trim();
		if (!string.IsNullOrWhiteSpace(fullName))
		{
			return fullName;
		}

		var email = user.Email;
		if (!string.IsNullOrWhiteSpace(email))
		{
			var localPart = email.Split('@')[0];
			if (!string.IsNullOrWhiteSpace(localPart))
			{
				return localPart;
			}
		}

		return fallback;
	}

	private static string CreatePatientAlias()
	{
		// Stored once on the membership row. A random alias cannot be derived
		// from a user id that may be visible through another API surface.
		// Six random bytes keep accidental alias collisions negligible even
		// in a large room.
		var bytes = RandomNumberGenerator.GetBytes(6);
		return $"Patient {Convert.ToHexString(bytes)}";
	}

	private static string Truncate(string value, int maxLength) =>
		value.Length <= maxLength ? value : value[..maxLength];

	private static ChatRoomResponse MapRoom(
		ChatRoom room,
		ChatRoomMember? membership,
		int memberCount,
		ChatMessage? latestMessage)
	{
		var activeMembership = IsActiveMember(membership) ? membership : null;

		return new ChatRoomResponse
		{
			Id = room.Id,
			Name = room.Name,
			Description = room.Description,
			Visibility = room.Visibility,
			CreatedAtUtc = room.CreatedAtUtc,
			LastMessageAtUtc = room.LastMessageAtUtc,
			IsArchived = room.IsArchived,
			MemberCount = memberCount,
			IsMember = activeMembership is not null,
			MyRole = activeMembership?.Role,
			MyMemberId = activeMembership?.Id,
			// A preview is conversation content, so it is only shown to active
			// members. Non-members can still see the room metadata and join.
			LastMessagePreview = activeMembership is not null && latestMessage is not null && !latestMessage.IsDeleted
				? BuildPreview(latestMessage.Content)
				: null
		};
	}

	private static ChatRoomMemberResponse MapMember(ChatRoomMember member) =>
		new()
		{
			MemberId = member.Id,
			DisplayName = member.DisplayName,
			IsDoctor = member.IsDoctor,
			Role = member.Role,
			JoinedAtUtc = member.JoinedAtUtc
		};

	private static ChatMessageResponse MapMessage(
		ChatMessage message,
		bool includeDeletedContent = false) =>
		new()
		{
			Id = message.Id,
			RoomId = message.RoomId,
			SenderMemberId = message.SenderMemberId,
			ClientMessageId = message.ClientMessageId,
			SenderDisplayName = message.SenderDisplayName,
			SenderIsDoctor = message.SenderIsDoctor,
			Content = message.IsDeleted && !includeDeletedContent ? string.Empty : message.Content,
			SentAtUtc = message.SentAtUtc,
			IsDeleted = message.IsDeleted
		};

	private static ChatMessageReportResponse MapReport(
		ChatMessageReport report,
		ChatMessage message,
		bool includeDeletedContent) =>
		new()
		{
			Id = report.Id,
			MessageId = report.MessageId,
			Reason = report.Reason,
			Status = report.Status,
			CreatedAtUtc = report.CreatedAtUtc,
			ReviewedAtUtc = report.ReviewedAtUtc,
			// Moderators need the original content to review a report even if
			// the sender deleted the message after it was reported.
			Message = MapMessage(message, includeDeletedContent)
		};

	private static string BuildPreview(string content)
	{
		var singleLine = content.Replace('\r', ' ').Replace('\n', ' ').Trim();
		return singleLine.Length <= MaxPreviewLength
			? singleLine
			: $"{singleLine[..MaxPreviewLength]}...";
	}

	private sealed record ChatUser(BaseUser User, bool IsDoctor, bool IsAdmin);
}
