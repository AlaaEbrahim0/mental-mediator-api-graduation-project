using Domain.Entities;

namespace Application.Contracts;

public interface INotificationSender
{
	Task SendNotificationAsync(Notification notification);
}
