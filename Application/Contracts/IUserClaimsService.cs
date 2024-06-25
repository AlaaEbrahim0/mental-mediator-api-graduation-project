namespace Application.Contracts;

public interface IUserClaimsService
{
	string GetUserId();
	string GetUserName();
	string GetRole();
	string GetPhotoUrl();
	string GetEmail();
}