using Shared;
using Shared.PostsDto;

namespace Application.Services;
public interface IPostService
{
    Task<Result<IEnumerable<PostResponse>>> GetPosts();
    Task<Result<PostResponse?>> GetPostById(int id);
    Task<Result<string>> UpdatePost(int id, UpdatePostRequest updatePostRequest);
    Task<Result<string>> DeletePost(int id);
    Task<Result<PostResponse>> CreatePostAsync(CreatePostRequest postRequest);

}
