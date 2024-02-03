using Shared;
using Shared.PostsDto;

namespace Application.Services;
public interface IPostService
{
    Task<Result<IEnumerable<PostResponse>>> GetPosts(RequestParameters parameters);
    Task<Result<PostResponse?>> GetPostById(int id);
    Task<Result<PostResponse>> UpdatePost(int id, UpdatePostRequest updatePostRequest);
    Task<Result<PostResponse>> DeletePost(int id);
    Task<Result<PostResponse>> CreatePostAsync(CreatePostRequest postRequest);
}
