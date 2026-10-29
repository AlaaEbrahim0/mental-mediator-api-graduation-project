using Application.Contracts;
using Application.Dtos.NotificationDtos;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Domain.Repositories;
using FluentAssertions;
using Shared;
using Shared.RequestParameters;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// Verifies that notification access is strictly scoped to the
/// authenticated user — cross-user reads, marks, and counts are denied.
/// Uses hand-rolled fakes instead of a mocking framework.
/// </summary>
public class NotificationOwnershipTests
{
	private const string AliceId = "alice-111";
	private const string BobId = "bob-222";

	private sealed class FakeUserClaims : IUserClaimsService
	{
		public string CurrentUserId { get; set; } = AliceId;
		public string GetUserId() => CurrentUserId;
		public string GetUserName() => "Test User";
		public string GetRole() => "User";
		public string GetPhotoUrl() => "https://example.com/photo.png";
		public string GetEmail() => "test@example.com";
	}

	private sealed class FakeNotificationRepository : INotificationRepository
	{
		public readonly Dictionary<int, Notification> Store = new();
		public int NextId = 1;

		public void CreateNotification(Notification notification)
		{
			notification.Id = NextId++;
			Store[notification.Id] = notification;
		}

		public void UpdateNotification(Notification notification) =>
			Store[notification.Id] = notification;

	 public  Task<Notification?> GetById(int notificationId, bool trackChanges) =>
			Task.FromResult(Store.GetValueOrDefault(notificationId));

		public Task<IEnumerable<Notification>> GetByUserId(string userId, RequestParameters request, bool trackChanges) =>
			Task.FromResult<IEnumerable<Notification>>(
				Store.Values.Where(n => n.AppUserId == userId));

		public Task<int> GetUnreadCountAsync(string userId) =>
			Task.FromResult(Store.Values.Count(n => n.AppUserId == userId && !n.IsRead));

		public Task MarkAllAsRead(string userId)
		{
			foreach (var n in Store.Values.Where(n => n.AppUserId == userId && !n.IsRead))
			{
				n.IsRead = true;
				n.ReadAt = DateTimeOffset.UtcNow;
			}
			return Task.CompletedTask;
		}

		public Task<bool> MarkMultipleAsReadAsync(IEnumerable<int> ids, string userId)
		{
			foreach (var n in Store.Values.Where(n =>
				ids.Contains(n.Id) && n.AppUserId == userId && !n.IsRead))
			{
				n.IsRead = true;
				n.ReadAt = DateTimeOffset.UtcNow;
			}
			return Task.FromResult(true);
		}
	}

	private sealed class FakeNotificationSender : INotificationSender
	{
		public int Calls { get; private set; }
		public Notification? Last { get; private set; }

	 public  Task SendNotificationAsync(Notification notification)
		{
			Calls++;
			Last = notification;
			return Task.CompletedTask;
		}
	}

	private static IMapper CreateMapper() =>
	new MapperConfiguration(cfg =>
	{
		cfg.CreateMap<Notification, NotificationResponse>()
			.ForMember(dest => dest.Resources, src => src.MapFrom(m => m.ResourcesObject));
	}).CreateMapper();

	private static Notification MakeNotification(int id, string userId) => new()
	{
		Id = id,
		AppUserId = userId,
		Title = "Test",
		Message = "Hello",
		DateCreated = DateTime.UtcNow,
		Type = NotificationType.Comment.ToString(),
		Resources = "{}"
	};

	private static NotificationService CreateService(
		FakeNotificationRepository repos,
		FakeUserClaims claims,
		FakeNotificationSender sender) =>
		new(new FakeRepoManager(repos), claims, CreateMapper(), sender);

	private sealed class FakeRepoManager : IRepositoryManager
	{
		private readonly FakeNotificationRepository _notifications;

	 public  FakeRepoManager(FakeNotificationRepository notifications) => _notifications = notifications;

		public INotificationRepository Notifications => _notifications;
		public Task SaveAsync() => Task.CompletedTask;

	 public  IPostRepository Posts => throw new NotImplementedException();
		public ICommentRepository Comments => throw new NotImplementedException();
		public IReplyRepository Replies => throw new NotImplementedException();
		public IChatRoomRepository ChatRooms => throw new NotImplementedException();
		public IChatMessageRepository ChatMessages => throw new NotImplementedException();
		public IChatMessageReportRepository ChatMessageReports => throw new NotImplementedException();
		public IDoctorRepository Doctors => throw new NotImplementedException();
		public IUserRepository Users => throw new NotImplementedException();
		public IProfileRepository Profiles => throw new NotImplementedException();
		public IAppointmentRepository Appointments => throw new NotImplementedException();
		public IDoctorSchedulingRepository DoctorScheduling => throw new NotImplementedException();
		public IDepressionTestResultRepository DepressionTestResults => throw new NotImplementedException();
	}

	[Fact]
	public async Task get_notification_by_id_denies_access_to_other_users()
	{
		var repos = new FakeNotificationRepository();
		repos.CreateNotification(MakeNotification(1, BobId));

		var service = CreateService(repos, new FakeUserClaims { CurrentUserId = AliceId }, new FakeNotificationSender());

		var result = await service.GetNotificationById(1);

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Notifications.Forbidden");
	}

	[Fact]
	public async Task get_notification_by_id_allows_access_to_owner()
	{
		var repos = new FakeNotificationRepository();
		repos.CreateNotification(MakeNotification(1, AliceId));

		var service = CreateService(repos, new FakeUserClaims { CurrentUserId = AliceId }, new FakeNotificationSender());

		var result = await service.GetNotificationById(1);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task get_notification_by_id_returns_not_found_for_real_owner_when_missing()
	{
		var repos = new FakeNotificationRepository();
		var service = CreateService(repos, new FakeUserClaims { CurrentUserId = AliceId }, new FakeNotificationSender());

		var result = await service.GetNotificationById(999);

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Notifications.NotFound");
	}

	[Fact]
	public async Task mark_as_read_denies_access_to_other_users()
	{
		var repos = new FakeNotificationRepository();
		repos.CreateNotification(MakeNotification(1, BobId));

		var service = CreateService(repos, new FakeUserClaims { CurrentUserId = AliceId }, new FakeNotificationSender());

		var result = await service.MarkAsReadAsync(1);

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Notifications.Forbidden");
	}

	[Fact]
	public async Task get_unread_count_is_scoped_to_current_user()
	{
		var repos = new FakeNotificationRepository();
		repos.CreateNotification(MakeNotification(1, AliceId)); // unread for Alice
		repos.CreateNotification(MakeNotification(2, BobId));   // unread for Bob

		var service = CreateService(repos, new FakeUserClaims { CurrentUserId = AliceId }, new FakeNotificationSender());

		var result = await service.GetUnreadCountAsync();

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().Be(1);
	}

	[Fact]
	public async Task get_current_user_notifications_returns_only_own_notifications()
	{
		var repos = new FakeNotificationRepository();
		repos.CreateNotification(MakeNotification(1, AliceId));
		repos.CreateNotification(MakeNotification(2, BobId));

		var service = CreateService(repos, new FakeUserClaims { CurrentUserId = AliceId }, new FakeNotificationSender());

		var result = await service.GetCurrentUserNotifications(new RequestParameters());

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().ContainSingle();
		result.Value.First().Id.Should().Be(1);
	}
}
