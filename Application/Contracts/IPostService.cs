using Application.Dtos.PostsDto;
using Microsoft.AspNetCore.Http;
using Shared;
using Shared.RequestParameters;

namespace Application.Contracts;
public interface IPostService
{
	Task<Result<IEnumerable<PostResponse>>> GetPosts(PostRequestParameters parameters);
	Task<Result<IEnumerable<PostResponse>>> GetPostsByUserId(string userId, PostRequestParameters parameters);
	Task<Result<PostResponse?>> GetPostById(int id);
	Task<Result<PostResponse?>> GetLatestDraft();
	Task<Result<StoredMedia>> UploadMediaAsync(IFormFile file);
	Task<Result> DeleteUnattachedMediaAsync(string url);
	Task<Result<PostResponse>> UpdatePost(int id, UpdatePostRequest updatePostRequest);
	Task<Result<PostResponse>> DeletePost(int id);
	Task<Result<PostResponse>> CreatePostAsync(CreatePostRequest postRequest);
	Task<Result<PostResponse>> SetLike(int id, bool liked);
}
