using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// Unread counts are derived from the membership read mark rather than stored,
/// so the boundary rules are asserted directly against the repository query:
/// own messages, deleted rows and anything before the mark must not count.
/// </summary>
public class ChatUnreadTests : IDisposable
{
	private const string PatientId = "patient-1";
	private const string OtherUserId = "patient-2";
	private static readonly DateTime JoinedAt = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);

	private readonly AppDbContext _context;
	private readonly ChatMessageRepository _repository;
	private int _nextRoomId;

	public ChatUnreadTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"chat-unread-{Guid.NewGuid()}")
			.Options;

		_context = new AppDbContext(options);
		_repository = new ChatMessageRepository(_context);
	}

	public void Dispose() => _context.Dispose();

	private ChatRoom AddRoom(string name)
	{
		var room = new ChatRoom
		{
			Id = ++_nextRoomId,
			Name = name,
			Description = string.Empty,
			CreatedById = OtherUserId,
			CreatedAtUtc = JoinedAt
		};

		_context.ChatRooms.Add(room);
		_context.SaveChanges();
		return room;
	}

	private void AddMember(
		ChatRoom room,
		string userId,
		DateTime? joinedAt = null,
		DateTime? lastReadAt = null,
		DateTime? leftAt = null)
	{
		_context.ChatRoomMembers.Add(new ChatRoomMember
		{
			Room = room,
			UserId = userId,
			Role = ChatRoomRole.Member,
			DisplayName = "Patient test",
			JoinedAtUtc = joinedAt ?? JoinedAt,
			LastReadAtUtc = lastReadAt,
			LeftAtUtc = leftAt
		});
		_context.SaveChanges();
	}

	private void AddMessage(ChatRoom room, string senderId, DateTime sentAt, bool deleted = false)
	{
		_context.ChatMessages.Add(new ChatMessage
		{
			Room = room,
			SenderId = senderId,
			SenderMemberId = senderId == PatientId ? 1 : 2,
			SenderDisplayName = "Sender",
			Content = "Message",
			ClientMessageId = Guid.NewGuid(),
			SentAtUtc = sentAt,
			IsDeleted = deleted
		});
		_context.SaveChanges();
	}

	[Fact]
	public async Task Counts_only_other_messages_after_the_read_mark()
	{
		var room = AddRoom("Anxiety support");
		AddMember(room, PatientId, lastReadAt: JoinedAt.AddMinutes(10));

		AddMessage(room, OtherUserId, JoinedAt.AddMinutes(5));
		AddMessage(room, OtherUserId, JoinedAt.AddMinutes(15));
		AddMessage(room, PatientId, JoinedAt.AddMinutes(20));
		AddMessage(room, OtherUserId, JoinedAt.AddMinutes(25), deleted: true);

		var counts = await _repository.GetUnreadCountsAsync(PatientId, new[] { room.Id });

		counts.Should().ContainKey(room.Id).WhoseValue.Should().Be(1);
	}

	[Fact]
	public async Task A_missing_read_mark_counts_from_the_join_time()
	{
		var room = AddRoom("Sleep and rest");
		AddMember(room, PatientId, joinedAt: JoinedAt, lastReadAt: null);

		AddMessage(room, OtherUserId, JoinedAt.AddMinutes(-1));
		AddMessage(room, OtherUserId, JoinedAt.AddMinutes(1));
		AddMessage(room, OtherUserId, JoinedAt.AddMinutes(2));

		var counts = await _repository.GetUnreadCountsAsync(PatientId, new[] { room.Id });

		counts[room.Id].Should().Be(2);
	}

	[Fact]
	public async Task Rooms_without_an_active_membership_are_not_counted()
	{
		var leftRoom = AddRoom("Left room");
		AddMember(leftRoom, PatientId, leftAt: JoinedAt.AddMinutes(1));
		AddMessage(leftRoom, OtherUserId, JoinedAt.AddMinutes(30));

		var strangerRoom = AddRoom("Stranger room");
		AddMessage(strangerRoom, OtherUserId, JoinedAt.AddMinutes(30));

		var counts = await _repository.GetUnreadCountsAsync(
			PatientId,
			new[] { leftRoom.Id, strangerRoom.Id });

		counts.Should().BeEmpty();
	}

	[Fact]
	public async Task Counts_are_grouped_per_room()
	{
		var first = AddRoom("First");
		var second = AddRoom("Second");
		AddMember(first, PatientId, lastReadAt: JoinedAt);
		AddMember(second, PatientId, lastReadAt: JoinedAt);

		AddMessage(first, OtherUserId, JoinedAt.AddMinutes(5));
		AddMessage(first, OtherUserId, JoinedAt.AddMinutes(6));
		AddMessage(second, OtherUserId, JoinedAt.AddMinutes(7));

		var counts = await _repository.GetUnreadCountsAsync(
			PatientId,
			new[] { first.Id, second.Id });

		counts[first.Id].Should().Be(2);
		counts[second.Id].Should().Be(1);
	}

	[Fact]
	public async Task Own_messages_alone_leave_a_room_out_of_the_result()
	{
		var room = AddRoom("My room");
		AddMember(room, PatientId, lastReadAt: JoinedAt);
		AddMessage(room, PatientId, JoinedAt.AddMinutes(5));

		var counts = await _repository.GetUnreadCountsAsync(PatientId, new[] { room.Id });

		counts.Should().BeEmpty();
	}
}
