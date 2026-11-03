using API.Configurations;
using Application.Contracts;
using Application.Dtos.ChatDtos;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shared;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// Reactions opened a new write path on chat messages, so the membership rule
/// has to hold there too: a message id is not a permission. These tests run the
/// production service over an in-memory store and assert the broadcast payload
/// the portals reconcile against.
/// </summary>
public class ChatReactionTests : IDisposable
{
	private const string PatientId = "patient-1";
	private const string DoctorId = "doctor-1";
	private const string OutsiderId = "patient-2";

	private readonly AppDbContext _context;
	private readonly ChatRoom _room;
	private readonly FakeRealtimeNotifier _notifier = new();
	private readonly FakeStorageService _storage = new();

	public ChatReactionTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"chat-reactions-{Guid.NewGuid()}")
			.Options;

		_context = new AppDbContext(options);

		_context.Doctors.Add(new Doctor
		{
			Id = DoctorId,
			Email = "doctor@example.com",
			UserName = "doctor",
			NormalizedEmail = "DOCTOR@EXAMPLE.COM",
			NormalizedUserName = "DOCTOR",
			FirstName = "Dana",
			LastName = "Doctor",
			Gender = "female",
			IsAvailable = true,
			SessionFees = 100m,
			Specialization = DoctorSpecialization.ClinicalPsychology,
		});

		_context.Clients.Add(new User
		{
			Id = PatientId,
			Email = "patient@example.com",
			UserName = "patient",
			NormalizedEmail = "PATIENT@EXAMPLE.COM",
			NormalizedUserName = "PATIENT",
			FirstName = "Pat",
			LastName = "Patient",
			Gender = "other",
		});

		_context.Clients.Add(new User
		{
			Id = OutsiderId,
			Email = "outsider@example.com",
			UserName = "outsider",
			NormalizedEmail = "OUTSIDER@EXAMPLE.COM",
			NormalizedUserName = "OUTSIDER",
			FirstName = "Ola",
			LastName = "Outsider",
			Gender = "other",
		});

		_room = new ChatRoom
		{
			Name = "Anxiety support",
			Description = "A quiet room.",
			CreatedById = DoctorId,
			CreatedAtUtc = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc),
		};
		_context.ChatRooms.Add(_room);
		_context.SaveChanges();

		_context.ChatRoomMembers.Add(new ChatRoomMember
		{
			RoomId = _room.Id,
			UserId = PatientId,
			Role = ChatRoomRole.Member,
			DisplayName = "Pat",
			IsDoctor = false,
			JoinedAtUtc = new DateTime(2026, 1, 1, 9, 1, 0, DateTimeKind.Utc),
		});
		_context.ChatRoomMembers.Add(new ChatRoomMember
		{
			RoomId = _room.Id,
			UserId = DoctorId,
			Role = ChatRoomRole.Owner,
			DisplayName = "Dr. Dana",
			IsDoctor = true,
			JoinedAtUtc = new DateTime(2026, 1, 1, 9, 0, 30, DateTimeKind.Utc),
		});
		_context.SaveChanges();
	}

	public void Dispose() => _context.Dispose();

	[Fact]
	public async Task A_member_reaction_is_stored_reported_and_broadcast()
	{
		var message = AddMessage(PatientId, "Feeling better today.");
		var member = MembershipOf(PatientId);

		var result = await CreateService(PatientId).SetReactionAsync(message.Id, "love");

		result.IsSuccess.Should().BeTrue();
		result.Value!.MyReaction.Should().Be("love");
		result.Value.Reactions.Should().ContainSingle()
			.Which.Count.Should().Be(1);

		_context.ChatMessageReactions.Should().ContainSingle()
			.Which.Reaction.Should().Be("love");

		_notifier.LastUpdate!.ActorMemberId.Should().Be(member.Id);
		_notifier.LastUpdate.ActorReaction.Should().Be("love");
		_notifier.LastUpdate.RoomId.Should().Be(_room.Id);
	}

	[Fact]
	public async Task A_second_reaction_replaces_the_first_instead_of_stacking()
	{
		var message = AddMessage(PatientId, "One more thing.");

		await CreateService(PatientId).SetReactionAsync(message.Id, "haha");
		var replaced = await CreateService(PatientId).SetReactionAsync(message.Id, "sad");

		replaced.IsSuccess.Should().BeTrue();
		replaced.Value!.MyReaction.Should().Be("sad");
		replaced.Value.Reactions.Should().ContainSingle()
			.Which.Count.Should().Be(1);
		_context.ChatMessageReactions.Should().ContainSingle()
			.Which.Reaction.Should().Be("sad");
	}

	[Fact]
	public async Task Two_members_choosing_differently_aggregate_without_leaking_identity()
	{
		var message = AddMessage(PatientId, "Any advice?");

		await CreateService(PatientId).SetReactionAsync(message.Id, "like");
		var second = await CreateService(DoctorId).SetReactionAsync(message.Id, "love");

		second.Value!.Reactions.Should().HaveCount(2);
		second.Value.Reactions.Sum(reaction => reaction.Count).Should().Be(2);
		second.Value.Reactions.Should().NotContain(reaction => reaction.Reaction == "wow");

		var third = await CreateService(DoctorId).GetMessagesAsync(
			_room.Id,
			new ChatMessageRequestParameters { Limit = 20 });
		third.Value!.Messages.Single().MyReaction.Should().Be("love");
	}

	[Fact]
	public async Task Removing_a_reaction_drops_the_row_and_broadcasts_the_gap()
	{
		var message = AddMessage(PatientId, "Thanks all.");
		await CreateService(PatientId).SetReactionAsync(message.Id, "wow");

		var removed = await CreateService(PatientId).RemoveReactionAsync(message.Id);

		removed.IsSuccess.Should().BeTrue();
		removed.Value!.MyReaction.Should().BeNull();
		removed.Value.Reactions.Should().BeEmpty();
		_context.ChatMessageReactions.Should().BeEmpty();
		_notifier.LastUpdate!.ActorReaction.Should().BeNull();
	}

	[Fact]
	public async Task Removing_nothing_is_not_an_error()
	{
		var message = AddMessage(PatientId, "No reaction here.");

		var removed = await CreateService(PatientId).RemoveReactionAsync(message.Id);

		removed.IsSuccess.Should().BeTrue();
		removed.Value!.Reactions.Should().BeEmpty();
	}

	[Fact]
	public async Task An_outsider_cannot_react_on_a_message_they_cannot_read()
	{
		var message = AddMessage(PatientId, "Members only.");

		var result = await CreateService(OutsiderId).SetReactionAsync(message.Id, "like");

		result.IsFailure.Should().BeTrue();
		result.Error!.Code.Should().Be("Chat.MessageNotFound");
		_context.ChatMessageReactions.Should().BeEmpty();
	}

	[Fact]
	public async Task A_removed_message_cannot_be_reacted_to()
	{
		var message = AddMessage(PatientId, "Taken down.");
		message.IsDeleted = true;
		message.DeletedAtUtc = DateTime.UtcNow;
		message.DeletedById = DoctorId;
		_context.SaveChanges();

		var react = await CreateService(DoctorId).SetReactionAsync(message.Id, "sad");
		var remove = await CreateService(PatientId).RemoveReactionAsync(message.Id);

		react.Error!.Code.Should().Be("Chat.CannotReactToDeletedMessage");
		remove.Error!.Code.Should().Be("Chat.CannotReactToDeletedMessage");
	}

	[Fact]
	public async Task A_reaction_outside_the_vocabulary_is_refused_before_any_lookup()
	{
		var message = AddMessage(PatientId, "Angry is not offered.");

		var result = await CreateService(PatientId).SetReactionAsync(message.Id, "angry");

		result.Error!.Code.Should().Be("Reactions.InvalidReaction");
		_notifier.LastUpdate.Should().BeNull();
	}

	[Fact]
	public async Task A_voice_note_is_accepted_as_media_without_any_text()
	{
		var result = await CreateService(PatientId).SendMessageAsync(_room.Id, new SendChatMessageRequest
		{
			Content = "",
			MediaUrl = "https://res.cloudinary.com/demo/video/upload/chat/voice-1.mp3",
			MediaType = "audio",
			ClientMessageId = Guid.NewGuid(),
		});

		result.IsSuccess.Should().BeTrue();
		result.Value!.MediaType.Should().Be("audio");

		var stored = await _context.ChatMessages.SingleAsync();
		stored.MediaType.Should().Be("audio");
		stored.Content.Should().BeEmpty();
	}

	[Fact]
	public async Task An_unmanaged_media_url_is_still_refused_for_voice()
	{
		var result = await CreateService(PatientId).SendMessageAsync(_room.Id, new SendChatMessageRequest
		{
			MediaUrl = "https://evil.example/voice.mp3",
			MediaType = "audio",
			ClientMessageId = Guid.NewGuid(),
		});

		result.Error!.Code.Should().Be("Chat.InvalidMessageMedia");
		_context.ChatMessages.Should().BeEmpty();
	}

	[Fact]
	public async Task A_deleted_message_reports_no_reactions_to_the_reader()
	{
		var message = AddMessage(PatientId, "React then remove.");
		await CreateService(DoctorId).SetReactionAsync(message.Id, "like");

		var deleted = await CreateService(DoctorId).DeleteMessageAsync(message.Id);

		deleted.IsSuccess.Should().BeTrue();
		deleted.Value!.Reactions.Should().BeEmpty();
		deleted.Value.MyReaction.Should().BeNull();
	}

	private ChatRoomMember MembershipOf(string userId) =>
		_context.ChatRoomMembers.Single(member => member.UserId == userId);

	private ChatMessage AddMessage(string senderId, string content)
	{
		var sender = MembershipOf(senderId);
		var message = new ChatMessage
		{
			RoomId = _room.Id,
			SenderId = senderId,
			SenderMemberId = sender.Id,
			SenderDisplayName = sender.DisplayName,
			SenderIsDoctor = sender.IsDoctor,
			Content = content,
			ClientMessageId = Guid.NewGuid(),
			SentAtUtc = DateTime.UtcNow,
		};

		_context.ChatMessages.Add(message);
		_context.SaveChanges();
		return message;
	}

	private ChatService CreateService(string userId) =>
		new(
			new RepositoryManager(_context),
			new FakeClaims(userId),
			CreateUserManager(),
			new PassModerator(),
			_notifier,
			new FakePassphraseHasher(),
			new FakeJoinThrottle(),
			_storage,
			new FakeNotificationSender());

	private UserManager<BaseUser> CreateUserManager() =>
		new(
			new LookupOnlyUserStore(_context),
			Options.Create(new IdentityOptions()),
			new PasswordHasher<BaseUser>(),
			Array.Empty<IUserValidator<BaseUser>>(),
			Array.Empty<IPasswordValidator<BaseUser>>(),
			new UpperInvariantLookupNormalizer(),
			new IdentityErrorDescriber(),
			null!,
			NullLogger<UserManager<BaseUser>>.Instance);

	/// <summary>
	/// The chat service reads the signed-in account by id and its role, and
	/// nothing else, so the store only has to answer those two questions.
	/// </summary>
	private sealed class LookupOnlyUserStore : IUserStore<BaseUser>, IUserRoleStore<BaseUser>
	{
		private readonly AppDbContext _context;

		public LookupOnlyUserStore(AppDbContext context) => _context = context;

		public Task<BaseUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default) =>
			_context.Set<BaseUser>().FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

		public Task<IList<string>> GetRolesAsync(BaseUser user, CancellationToken cancellationToken = default) =>
			Task.FromResult<IList<string>>(user is Doctor ? new[] { "Doctor" } : new[] { "Client" });

		public Task<bool> IsInRoleAsync(BaseUser user, string role, CancellationToken cancellationToken = default) =>
			Task.FromResult(user is Doctor && role == "Doctor");

		public Task AddToRoleAsync(BaseUser user, string normalizedRoleName, CancellationToken cancellationToken = default) =>
			throw new NotSupportedException();

		public Task<IList<BaseUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default) =>
			Task.FromResult<IList<BaseUser>>(Array.Empty<BaseUser>());

		public Task RemoveFromRoleAsync(BaseUser user, string normalizedRoleName, CancellationToken cancellationToken = default) =>
			throw new NotSupportedException();

		public Task<BaseUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default) =>
			throw new NotSupportedException();

		public Task<string> GetUserIdAsync(BaseUser user, CancellationToken cancellationToken = default) =>
			Task.FromResult(user.Id);

		public Task<string?> GetUserNameAsync(BaseUser user, CancellationToken cancellationToken = default) =>
			Task.FromResult<string?>(user.UserName);

		public Task<string?> GetNormalizedUserNameAsync(BaseUser user, CancellationToken cancellationToken = default) =>
			Task.FromResult<string?>(user.NormalizedUserName);

		public Task SetUserNameAsync(BaseUser user, string? userName, CancellationToken cancellationToken = default) =>
			throw new NotSupportedException();

		public Task SetNormalizedUserNameAsync(BaseUser user, string? normalizedName, CancellationToken cancellationToken = default) =>
			throw new NotSupportedException();

		public Task<IdentityResult> CreateAsync(BaseUser user, CancellationToken cancellationToken = default) =>
			throw new NotSupportedException();

		public Task<IdentityResult> UpdateAsync(BaseUser user, CancellationToken cancellationToken = default) =>
			throw new NotSupportedException();

		public Task<IdentityResult> DeleteAsync(BaseUser user, CancellationToken cancellationToken = default) =>
			throw new NotSupportedException();

		public void Dispose()
		{
		}

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	private sealed class FakeClaims : IUserClaimsService
	{
		private readonly string _userId;

		public FakeClaims(string userId) => _userId = userId;

		public string GetUserId() => _userId;
		public string GetUserName() => "Test Member";
		public string GetRole() => "Client";
		public string GetPhotoUrl() => string.Empty;
		public string GetEmail() => "patient@example.com";
	}

	private sealed class PassModerator : IChatContentModerator
	{
		public Task<Result> EnsureAllowedAsync(string content) => Task.FromResult(Result.Success());
	}

	private sealed class FakePassphraseHasher : IRoomPassphraseHasher
	{
		public string Hash(string passphrase) => passphrase;
		public bool Verify(string passphrase, string? storedHash) => true;
	}

	private sealed class FakeJoinThrottle : IRoomJoinThrottle
	{
		public bool IsBlocked(string userId, int roomId) => false;
		public void RegisterFailure(string userId, int roomId)
		{
		}
		public void Reset(string userId, int roomId)
		{
		}
	}

	private sealed class FakeStorageService : IStorageService
	{
		public Task<Result<string>> UploadPhoto(IFormFile photo) =>
			Task.FromResult<Result<string>>("https://res.cloudinary.com/demo/chat/photo.jpg");

		public Task<Result<StoredMedia>> UploadMedia(IFormFile file) =>
			Task.FromResult<Result<StoredMedia>>(new StoredMedia(
				"https://res.cloudinary.com/demo/video/upload/chat/voice-1.mp3",
				"audio",
				"chat/voice-1"));

		public bool IsManagedMediaUrl(string url) =>
			url.StartsWith("https://res.cloudinary.com/", StringComparison.Ordinal);

		public Task<Result> DeleteMediaAsync(string publicId, string mediaType) =>
			Task.FromResult(Result.Success());
	}

	private sealed class FakeRealtimeNotifier : IChatRealtimeNotifier
	{
		public ChatMessageReactionsUpdatedEvent? LastUpdate { get; private set; }

		public Task MessageReactionsUpdatedAsync(int roomId, ChatMessageReactionsUpdatedEvent update)
		{
			LastUpdate = update;
			return Task.CompletedTask;
		}

		public Task MessageCreatedAsync(int roomId, ChatMessageResponse message) => Task.CompletedTask;
		public Task MessageDeletedAsync(int roomId, ChatMessageResponse message) => Task.CompletedTask;
		public Task MemberUpdatedAsync(int roomId, ChatRoomMemberResponse member) => Task.CompletedTask;
		public Task MemberLeftAsync(int roomId, int memberId, string userId) => Task.CompletedTask;
		public Task RoomUpdatedAsync(int roomId, ChatRoomResponse room) => Task.CompletedTask;
		public Task RoomArchivedAsync(int roomId, ChatRoomResponse room) => Task.CompletedTask;
	}
}
