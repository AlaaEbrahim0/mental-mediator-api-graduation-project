using System.Security.Claims;

namespace Infrastructure.Repositories;
public class UserRepository : IUserRepository
{
    private readonly ClaimsPrincipal claimsPrincipal;

    public UserRepository(ClaimsPrincipal claimsPrincipal)
    {
        this.claimsPrincipal = claimsPrincipal;
    }

    public string GetUserId()
    {
        var id = claimsPrincipal.FindFirstValue("uid")!;
        return id;
    }
}
