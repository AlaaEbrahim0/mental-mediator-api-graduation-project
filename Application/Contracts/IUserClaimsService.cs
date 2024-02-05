namespace Application.Contracts;

public interface IUserClaimsService
{
    string GetUserId();
    string GetUserName();
}