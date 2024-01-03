using Infrastructure.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/posts")]
public class PostController : ControllerBase
{
    private readonly IPostRepository _postRepository;

    public PostController(IPostRepository postRepository)
    {
        _postRepository = postRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPosts()
    {
        var posts = await _postRepository.GetAllPosts(true);
        return Ok(posts);
    }
}
