using System.Security.Cryptography;
using Application.Contracts;
using Application.Dtos.ChatDtos;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Shared;

namespace Application.Services;

public class ChatService : IChatService
{
	private const int MaxPreviewLength = 120;

	/// <summary>
	/// The display role a doctor takes when entering a room a patient owns. Kept
	/// as one constant so the join path and the role change path agree.
	/// </summary>
	private const string DoctorRole = "Doctor";

	private const string AdminRole = "Admin";

	private const string PatientRole = "User";

	private readonly IRepositoryManager _repos;
	private readonly IUserClaimsService _userClaimsService;
	private readonly UserManager<BaseUser> _userManager;
	private readonly IChatContentModerator _contentModerator;
	private readonly IChatRealtimeNotifier _realtimeNotifier;
	private readonly IRoomPassphraseHasher _passphraseHasher;
	private readonly IRoomJoinThrottle _joinThrottle;
	private readonly IStorageService _storageService;

	public ChatService(
		IRepositoryManager repos,
		IUserClaimsService userClaimsService,
		UserManager<BaseUser> userManager,
		IChatContentModerator contentModerator,
		IChatRealtimeNotifier realtimeNotifier,
		IRoomPassphraseHasher passphraseHasher,
		IRoomJoinThrottle joinThrottle,
		IStorageService storageService)
	{
		_repos = repos;
		_userClaimsService = userClaimsService;
		_userManager = userManager;
		_contentModerator = contentModerator;
		_realtimeNotifier = realtimeNotifier;
		_passphraseHasher = passphraseHasher;
		_joinThrottle = joinThrottle;
		_storageService = storageService;
	}

	public async Task<Result<ChatRoomPageResponse>> GetRoomsAsync(ChatRoomRequestParameters parameters)
	{
		var currentUser = await GetCurrentUserAsync();
		if (currentUser.IsFailure)
		{
			return currentUser.Error;
		}

		var rooms = await _repos.ChatRooms.GetListableRoomsAsync(
			parameters.BeforeRoomId,
			parameters.Limit + 1,
			false);

		var hasMore = rooms.Count > parameters.Limit;
		var page = rooms.Take(parameters.Limit).ToList();
		var roomIds = page.Select(x => x.Id).ToList();

		var memberships = await _repos.ChatRooms.GetMembershipsAsync(currentUser.Value.Id, roomIds, false);
		var membershipByRoom = memberships.ToDictionary(x => x.RoomId);
		var memberCounts = await _repos.ChatRooms.GetActiveMemberCountsAsync(roomIds);
		var ownersByRoom = await _repos.ChatRooms.GetOwnersByRoomIdsAsync(roomIds);

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
				ownersByRoom.TryGetValue(room.Id, out var owner);

				return MapRoom(room, membership, memberCount, latestMessage, owner);
			}).ToList(),
			HasMore = hasMore,
			NextBeforeRoomId = hasMore && page.Count > 0 ? page[^1].Id : null
		};

		return response;
	}

	public async Task<Result<ChatRoomBrowseResponse>> BrowseRoomsAsync(RoomBrowseParameters parameters)
	{
		var currentUser = await GetCurrentUserAsync();
		if (currentUser.IsFailure)
		{
			return currentUser.Error;
		}

		// RequestParameters clamps only the upper bound; a negative page size would
		// reach SQL Server as FETCH NEXT (-1).
		var pageSize = Math.Clamp(parameters.PageSize, 1, 50);
		var pageNumber = parameters.PageNumber < 1 ? 1 : parameters.PageNumber;

		var (rooms, totalCount) = await _repos.ChatRooms.BrowseRoomsAsync(new RoomBrowseQuery(
			parameters.Search,
			parameters.Tab,
			parameters.Sort,
			DateTime.UtcNow,
			(pageNumber - 1) * pageSize,
			pageSize));

		var roomIds = rooms.Select(x => x.Id).ToList();
		var memberships = await _repos.ChatRooms.GetMembershipsAsync(currentUser.Value.Id, roomIds, false);
		var membershipByRoom = memberships.ToDictionary(x => x.RoomId);
		var memberCounts = await _repos.ChatRooms.GetActiveMemberCountsAsync(roomIds);
		var ownersByRoom = await _repos.ChatRooms.GetOwnersByRoomIdsAsync(roomIds);

		var memberRoomIds = memberships
			.Where(x => x.LeftAtUtc is null)
			.Select(x => x.RoomId)
			.ToList();
		var latestMessages = await _repos.ChatMessages.GetLatestMessagesAsync(memberRoomIds, false);

		return new ChatRoomBrowseResponse
		{
			Rooms = rooms.Select(room =>
			{
				membershipByRoom.TryGetValue(room.Id, out var membership);
				memberCounts.TryGetValue(room.Id, out var memberCount);
				latestMessages.TryGetValue(room.Id, out var latestMessage);
				ownersByRoom.TryGetValue(room.Id, out var owner);

				return MapRoom(room, membership, memberCount, latestMessage, owner);
			}).ToList(),
			TotalCount = totalCount,
			PageNumber = pageNumber,
			PageSize = pageSize,
			HasMore = pageNumber * pageSize < totalCount
		};
	}

	public async Task<Result<ChatRoomResponse>> GetRoomAsync(int roomId)
	{
		var currentUser = await GetCurrentUserAsync();
		if (currentUser.IsFailure)
		{
			return currentUser.Error;
		}

		var room = await _repos.ChatRooms.GetRoomByIdAsync(roomId, false);
		if (room is null)
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
		var owner = await _repos.ChatRooms.GetOwnerAsync(roomId, false);
		var latestMessage = isActiveMember
			? await _repos.ChatMessages.GetLatestMessageAsync(roomId, false)
			: null;

		// A private room is reachable by non-members on purpose: this is the
		// response that tells the client to ask for the passphrase. The
		// description and preview are redacted inside MapRoom.
		return MapRoom(room, membership, memberCount, latestMessage, owner);
	}

	public async Task<Result<ChatRoomResponse>> EnsureActiveMemberAsync(int roomId)
	{
		var currentUser = await GetCurrentUserAsync();
		if (currentUser.IsFailure)
		{
			return currentUser.Error;
		}

		var room = await _repos.ChatRooms.GetRoomByIdAsync(roomId, false);
		if (room is null)
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
		var owner = await _repos.ChatRooms.GetOwnerAsync(roomId, false);
		var latestMessage = await _repos.ChatMessages.GetLatestMessageAsync(roomId, false);

		return MapRoom(room, membership, memberCount, latestMessage, owner);
	}

	public async Task<Result<ChatRoomResponse>> CreateRoomAsync(CreateChatRoomRequest request)
	{
		var chatUserResult = await GetCurrentChatUserAsync();
		if (chatUserResult.IsFailure)
		{
			return chatUserResult.Error;
		}

		var chatUser = chatUserResult.Value;

		// Patients may open their own consultation and follow-up rooms. The account
		// still has to hold one of the portal roles, so a role-less row left behind
		// by a half-finished sign-up cannot.
		if (!chatUser.IsDoctor && !chatUser.IsAdmin && !await _userManager.IsInRoleAsync(chatUser.User, PatientRole))
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

		var passphrase = request.Passphrase?.Trim() ?? string.Empty;
		var hasPassphrase = passphrase.Length > 0;
		if (hasPassphrase && !RoomAccessPolicy.IsValidPassphrase(passphrase))
		{
			return ChatErrors.InvalidRoomPassphrase();
		}

		var now = DateTime.UtcNow;
		var scheduleCheck = RoomAccessPolicy.ValidateScheduledStart(request.ScheduledStartAtUtc, now);
		if (scheduleCheck.IsFailure)
		{
			return scheduleCheck.Error;
		}

		var room = new ChatRoom
		{
			Name = name,
			Description = description,
			Visibility = request.Visibility,
			CreatedById = chatUser.User.Id,
			CreatedAtUtc = now,
			IsArchived = false,
			ScheduledStartAtUtc = request.ScheduledStartAtUtc
		};

		// A passphrase always makes the room private; the policy object owns that
		// rule so create and update cannot drift apart.
		RoomAccessPolicy.ApplyPrivacy(
			room,
			request.Visibility,
			hasPassphrase ? _passphraseHasher.Hash(passphrase) : null);

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

		return MapRoom(room, owner, 1, null, owner);
	}

	public async Task<Result<ChatRoomMemberResponse>> JoinRoomAsync(int roomId, JoinChatRoomRequest? request)
	{
		var chatUserResult = await GetCurrentChatUserAsync();
		if (chatUserResult.IsFailure)
		{
			return chatUserResult.Error;
		}

		var chatUser = chatUserResult.Value;
		var room = await _repos.ChatRooms.GetRoomByIdAsync(roomId, false);
		if (room is null)
		{
			return ChatErrors.RoomNotFound();
		}

		var passphrase = request?.Passphrase;
		var supplied = !string.IsNullOrWhiteSpace(passphrase);
		var requiresPassphrase = room.PasswordHash is not null;
		var matches = supplied &&
			requiresPassphrase &&
			_passphraseHasher.Verify(passphrase!, room.PasswordHash!);

		var userId = chatUser.User.Id;
		var blocked = _joinThrottle.IsBlocked(userId, roomId);

		if (!blocked && supplied && requiresPassphrase && !matches)
		{
			// Only a genuine wrong guess counts. Counting blocked attempts would let
			// a determined caller hold the block open forever.
			_joinThrottle.RegisterFailure(userId, roomId);
		}

		var membership = await _repos.ChatRooms.GetMembershipAsync(roomId, userId, true);
		var isActiveMember = IsActiveMember(membership);

		var decision = RoomAccessPolicy.EvaluateJoin(room, new RoomAccessPolicy.JoinAttempt(
			isActiveMember,
			supplied,
			matches,
			blocked));

		if (decision.IsFailure)
		{
			return decision.Error;
		}

		if (requiresPassphrase)
		{
			_joinThrottle.Reset(userId, roomId);
		}

		// A room a patient owns has no moderator until a doctor arrives, and the
		// portal's only safeguard for patient-created rooms is a doctor being able
		// to pause or close them, so the promotion happens on join.
		var roomOwner = await _repos.ChatRooms.GetOwnerAsync(roomId, false);

		if (membership is not null)
		{
			var displayName = ResolveDisplayName(chatUser, membership);
			var role = ResolveRole(chatUser, membership, roomOwner?.IsDoctor ?? true);
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
			UserId = userId,
			Role = chatUser.IsDoctor && roomOwner is not null && !roomOwner.IsDoctor
				? ChatRoomRole.Moderator
				: ChatRoomRole.Member,
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
		if (room is null)
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
		if (room is null)
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
		if (room is null)
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
		if (room is null)
		{
			return ChatErrors.RoomNotFound();
		}

		if (room.IsArchived)
		{
			return ChatErrors.RoomArchived();
		}

		// A paused room is still readable, but it accepts nothing new.
		if (room.IsLocked)
		{
			return ChatErrors.RoomLocked();
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
		// persisting so an archive or a pause that happened during moderation is
		// respected.
		var currentRoom = await _repos.ChatRooms.GetRoomByIdAsync(roomId, false);
		if (currentRoom is null)
		{
			return ChatErrors.RoomNotFound();
		}

		if (currentRoom.IsArchived)
		{
			return ChatErrors.RoomArchived();
		}

		if (currentRoom.IsLocked)
		{
			return ChatErrors.RoomLocked();
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
		if (room is null)
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
		if (room is null)
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
		if (room is null)
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
		if (room is null)
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
		if (room is null)
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
		var context = await GetModeratorContextAsync(roomId);
		if (context.IsFailure)
		{
			return context.Error;
		}

		var (room, membership) = context.Value;

		var changed = !room.IsArchived;

		if (changed)
		{
			room.IsArchived = true;
			_repos.ChatRooms.UpdateRoom(room);
			await _repos.SaveAsync();
		}

		var response = await BuildRoomAsync(room, membership);

		if (changed)
		{
			await _realtimeNotifier.RoomArchivedAsync(roomId, response);
		}

		return response;
	}

	public async Task<Result<ChatRoomResponse>> LockRoomAsync(int roomId) =>
		await SetLockStateAsync(roomId, locked: true);

	public async Task<Result<ChatRoomResponse>> UnlockRoomAsync(int roomId) =>
		await SetLockStateAsync(roomId, locked: false);

	private async Task<Result<ChatRoomResponse>> SetLockStateAsync(int roomId, bool locked)
	{
		var context = await GetModeratorContextAsync(roomId);
		if (context.IsFailure)
		{
			return context.Error;
		}

		var (room, membership) = context.Value;
		var changed = room.IsLocked != locked;

		if (changed)
		{
			room.IsLocked = locked;
			_repos.ChatRooms.UpdateRoom(room);
			await _repos.SaveAsync();
		}

		var response = await BuildRoomAsync(room, membership);

		if (changed)
		{
			await _realtimeNotifier.RoomUpdatedAsync(roomId, response);
		}

		return response;
	}

	public async Task<Result<ChatRoomResponse>> UpdateRoomAsync(int roomId, UpdateChatRoomRequest request)
	{
		var context = await GetModeratorContextAsync(roomId);
		if (context.IsFailure)
		{
			return context.Error;
		}

		var (room, membership) = context.Value;

		// Absent means "leave it alone", so only the supplied field is validated
		// and written. An empty name is still rejected: a nameless room is
		// unreadable in every list.
		var name = room.Name;
		if (request.Name is not null)
		{
			name = request.Name.Trim();
			if (name.Length == 0 || name.Length > 100 || name.Any(char.IsControl))
			{
				return ChatErrors.InvalidRoomName();
			}
		}

		var description = room.Description;
		if (request.Description is not null)
		{
			description = request.Description.Trim();
			if (description.Length > 500 ||
				description.Any(character =>
					char.IsControl(character) && character is not '\n' and not '\r' and not '\t'))
			{
				return ChatErrors.InvalidRoomDescription();
			}
		}

		var changed = room.Name != name || room.Description != description;

		if (changed)
		{
			room.Name = name;
			room.Description = description;
			_repos.ChatRooms.UpdateRoom(room);
			await _repos.SaveAsync();
		}

		var response = await BuildRoomAsync(room, membership);

		if (changed)
		{
			await _realtimeNotifier.RoomUpdatedAsync(roomId, response);
		}

		return response;
	}

	public async Task<Result<ChatRoomResponse>> UpdatePrivacyAsync(int roomId, UpdateRoomPrivacyRequest request)
	{
		var context = await GetModeratorContextAsync(roomId);
		if (context.IsFailure)
		{
			return context.Error;
		}

		var (room, membership) = context.Value;
		var previousVisibility = room.Visibility;
		var previousHash = room.PasswordHash;

		// Going public drops the stored passphrase: an open room that still asks
		// for one is a state no client can represent.
		RoomAccessPolicy.ApplyPrivacy(room, request.Visibility, hashedPassphrase: null);

		var changed = previousVisibility != room.Visibility || !string.Equals(previousHash, room.PasswordHash, StringComparison.Ordinal);

		if (changed)
		{
			_repos.ChatRooms.UpdateRoom(room);
			await _repos.SaveAsync();
		}

		var response = await BuildRoomAsync(room, membership);

		if (changed)
		{
			await _realtimeNotifier.RoomUpdatedAsync(roomId, response);
		}

		return response;
	}

	public async Task<Result<ChatRoomResponse>> SetPassphraseAsync(int roomId, SetRoomPassphraseRequest request)
	{
		var context = await GetModeratorContextAsync(roomId);
		if (context.IsFailure)
		{
			return context.Error;
		}

		var passphrase = request.Passphrase?.Trim() ?? string.Empty;
		if (!RoomAccessPolicy.IsValidPassphrase(passphrase))
		{
			return ChatErrors.InvalidRoomPassphrase();
		}

		var (room, membership) = context.Value;
		RoomAccessPolicy.ApplyPrivacy(room, visibility: null, _passphraseHasher.Hash(passphrase));

		_repos.ChatRooms.UpdateRoom(room);
		await _repos.SaveAsync();

		var response = await BuildRoomAsync(room, membership);
		await _realtimeNotifier.RoomUpdatedAsync(roomId, response);

		return response;
	}

	public async Task<Result> ClearPassphraseAsync(int roomId)
	{
		var context = await GetModeratorContextAsync(roomId);
		if (context.IsFailure)
		{
			return context.Error;
		}

		var (room, membership) = context.Value;

		if (room.PasswordHash is null)
		{
			return Result.Success();
		}

		RoomAccessPolicy.ClearPassphrase(room);
		_repos.ChatRooms.UpdateRoom(room);
		await _repos.SaveAsync();

		var response = await BuildRoomAsync(room, membership);
		await _realtimeNotifier.RoomUpdatedAsync(roomId, response);

		return Result.Success();
	}

	public async Task<Result<ChatRoomResponse>> UpdateScheduleAsync(int roomId, UpdateRoomScheduleRequest request)
	{
		var context = await GetModeratorContextAsync(roomId);
		if (context.IsFailure)
		{
			return context.Error;
		}

		var scheduleCheck = RoomAccessPolicy.ValidateScheduledStart(request.ScheduledStartAtUtc, DateTime.UtcNow);
		if (scheduleCheck.IsFailure)
		{
			return scheduleCheck.Error;
		}

		var (room, membership) = context.Value;
		var changed = room.ScheduledStartAtUtc != request.ScheduledStartAtUtc;

		if (changed)
		{
			room.ScheduledStartAtUtc = request.ScheduledStartAtUtc;
			_repos.ChatRooms.UpdateRoom(room);
			await _repos.SaveAsync();
		}

		var response = await BuildRoomAsync(room, membership);

		if (changed)
		{
			await _realtimeNotifier.RoomUpdatedAsync(roomId, response);
		}

		return response;
	}

	public async Task<Result<ChatRoomResponse>> UploadCoverAsync(int roomId, IFormFile? image)
	{
		var context = await GetModeratorContextAsync(roomId);
		if (context.IsFailure)
		{
			return context.Error;
		}

		if (image is null || image.Length == 0)
		{
			return ChatErrors.InvalidRoomCover();
		}

		var uploaded = await _storageService.UploadPhoto(image);
		if (uploaded.IsFailure)
		{
			return uploaded.Error;
		}

		var (room, membership) = context.Value;
		room.CoverImageUrl = uploaded.Value;
		_repos.ChatRooms.UpdateRoom(room);
		await _repos.SaveAsync();

		var response = await BuildRoomAsync(room, membership);
		await _realtimeNotifier.RoomUpdatedAsync(roomId, response);

		return response;
	}

	public async Task<Result<ChatRoomResponse>> RemoveCoverAsync(int roomId)
	{
		var context = await GetModeratorContextAsync(roomId);
		if (context.IsFailure)
		{
			return context.Error;
		}

		var (room, membership) = context.Value;
		var changed = room.CoverImageUrl is not null;

		if (changed)
		{
			room.CoverImageUrl = null;
			_repos.ChatRooms.UpdateRoom(room);
			await _repos.SaveAsync();
		}

		var response = await BuildRoomAsync(room, membership);

		if (changed)
		{
			await _realtimeNotifier.RoomUpdatedAsync(roomId, response);
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
		var isDoctor = roles.Any(role => string.Equals(role, DoctorRole, StringComparison.OrdinalIgnoreCase));
		var isAdmin = roles.Any(role => string.Equals(role, AdminRole, StringComparison.OrdinalIgnoreCase));

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

		return await _userManager.IsInRoleAsync(currentUser, DoctorRole);
	}

	/// <summary>
	/// Loads the room for a change that only a moderator may make: the caller has
	/// to be an active member and either the owner or a doctor moderator. The room
	/// is tracked so the caller's assignment is what gets saved.
	/// </summary>
	private async Task<Result<(ChatRoom Room, ChatRoomMember Membership)>> GetModeratorContextAsync(int roomId)
	{
		var chatUserResult = await GetCurrentChatUserAsync();
		if (chatUserResult.IsFailure)
		{
			return chatUserResult.Error;
		}

		var chatUser = chatUserResult.Value;
		var room = await _repos.ChatRooms.GetRoomByIdAsync(roomId, true);
		if (room is null)
		{
			return ChatErrors.RoomNotFound();
		}

		var membership = await _repos.ChatRooms.GetMembershipAsync(roomId, chatUser.User.Id, false);
		if (!IsActiveMember(membership))
		{
			return ChatErrors.RoomNotFound();
		}

		if (!await CanModerateAsync(membership, chatUser.User))
		{
			return ChatErrors.ModeratorRequired();
		}

		return (room, membership!);
	}

	/// <summary>
	/// Builds the response for a room the caller has just read or changed. Counts
	/// and the latest message are read here so every command returns the same
	/// snapshot shape as the list and detail endpoints.
	/// </summary>
	private async Task<ChatRoomResponse> BuildRoomAsync(ChatRoom room, ChatRoomMember? membership)
	{
		var memberCount = await _repos.ChatRooms.GetActiveMemberCountAsync(room.Id);
		var owner = await _repos.ChatRooms.GetOwnerAsync(room.Id, false);
		var isActiveMember = IsActiveMember(membership);
		var latestMessage = isActiveMember
			? await _repos.ChatMessages.GetLatestMessageAsync(room.Id, false)
			: null;

		return MapRoom(room, membership, memberCount, latestMessage, owner);
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

	private static ChatRoomRole ResolveRole(
		ChatUser user,
		ChatRoomMember membership,
		bool ownerIsDoctor)
	{
		if (membership.Role == ChatRoomRole.Owner)
		{
			return ChatRoomRole.Owner;
		}

		if (user.IsDoctor && !ownerIsDoctor)
		{
			return ChatRoomRole.Moderator;
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

	internal static ChatRoomResponse MapRoom(
		ChatRoom room,
		ChatRoomMember? membership,
		int memberCount,
		ChatMessage? latestMessage,
		ChatRoomMember? owner = null)
	{
		var activeMembership = IsActiveMember(membership) ? membership : null;
		var canSeeContent = RoomAccessPolicy.CanSeeContent(room, activeMembership is not null);

		return new ChatRoomResponse
		{
			Id = room.Id,
			Name = room.Name,
			Description = RoomAccessPolicy.RedactDescription(room, canSeeContent),
			Visibility = room.Visibility,
			CreatedAtUtc = room.CreatedAtUtc,
			LastMessageAtUtc = room.LastMessageAtUtc,
			IsArchived = room.IsArchived,
			IsPrivate = room.Visibility == ChatRoomVisibility.Private,
			RequiresPassword = room.PasswordHash is not null,
			IsLocked = room.IsLocked,
			CoverImageUrl = room.CoverImageUrl,
			ScheduledStartAtUtc = room.ScheduledStartAtUtc,
			MemberCount = memberCount,
			IsMember = activeMembership is not null,
			MyRole = activeMembership?.Role,
			MyMemberId = activeMembership?.Id,
			OwnerDisplayName = owner?.DisplayName,
			OwnerIsDoctor = owner?.IsDoctor ?? false,
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
			UserId = member.UserId,
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
			SenderUserId = message.SenderId,
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
