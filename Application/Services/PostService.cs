using Application.Contracts;
using Application.Dtos.PostsDto;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Shared;


namespace Application.Services;
public class PostService : IPostService
{
	private readonly IRepositoryManager _repos;
	private readonly IMapper _mapper;
	private readonly IUserClaimsService _userClaimsService;
	private readonly IHateSpeechDetector _hateSpeechDetector;
	private readonly ICacheService _cacheService;


	public PostService(IRepositoryManager repos, IMapper mapper, IUserClaimsService userClaimsService, IHateSpeechDetector hateSpeechDetector, ICacheService cacheService)
	{
		_repos = repos;
		_mapper = mapper;
		_userClaimsService = userClaimsService;
		_hateSpeechDetector = hateSpeechDetector;
		_cacheService = cacheService;
	}

	public async Task<Result<IEnumerable<PostResponse>>> GetPosts(
		RequestParameters parameters)
	{
		string postPageKey = $"posts_{parameters.PageNumber}_{parameters.PageSize}";

		var cachedPosts = await _cacheService.GetAsync<List<PostResponse>>(postPageKey);

		if (cachedPosts is not null)
		{
			return cachedPosts;
		}
		var posts = await _repos.Posts.GetAllPosts(parameters, false);

		var postResponse = _mapper.Map<IEnumerable<PostResponse>>(posts);

		await _cacheService.SetAsync(postPageKey, postResponse, TimeSpan.FromMinutes(1));

		return postResponse.ToList();
	}

	public async Task<Result<PostResponse?>> GetPostById(int id)
	{
		var post = await _repos.Posts.GetPostById(id, false);
		if (post is null)
		{
			return PostErrors.NotFound(id);
		}
		var postResponse = _mapper.Map<PostResponse>(post);
		return postResponse;
	}



	public async Task<Result<PostResponse>> CreatePostAsync(CreatePostRequest postRequest)
	{
		var isHateSpeechResult = await _hateSpeechDetector.IsHateSpeech($"{postRequest.Title} {postRequest.Content}"!);

		if (isHateSpeechResult.IsFailure)
		{
			return isHateSpeechResult.Error;
		}
		if (isHateSpeechResult.Value)
		{
			return Error.Forbidden("Content.Forbidden", "Your post violates our policy against hate speech and could not be published");
		}

		var post = _mapper.Map<Post>(postRequest);

		var userId = _userClaimsService.GetUserId();
		post.AppUserId = userId;
		post.PostedOn = DateTime.UtcNow;

		if (!postRequest.IsAnonymous)
		{
			var userName = _userClaimsService.GetUserName();
			post.Username = userName;
		}

		_repos.Posts.CreatePost(post);
		await _repos.SaveAsync();

		var postResponse = _mapper.Map<PostResponse>(post);
		return postResponse;
	}

	public async Task<Result<PostResponse>> DeletePost(int id)
	{
		var userId = _userClaimsService.GetUserId();

		var post = await _repos.Posts.GetPostById(id, true);
		if (post is null)
		{
			return PostErrors.NotFound(id);
		}
		if (!post.AppUserId!.Equals(userId))
		{
			return PostErrors.Forbidden(post.Id);
		}

		_repos.Posts.DeletePost(post);
		await _repos.SaveAsync();

		var postResponse = _mapper.Map<PostResponse>(post);
		return postResponse;

	}

	public async Task<Result<PostResponse>> UpdatePost(int id, UpdatePostRequest updatePostRequest)
	{
		var isHateSpeechResult = await _hateSpeechDetector.IsHateSpeech($"{updatePostRequest.Title} {updatePostRequest.Content}"!);

		if (isHateSpeechResult.IsFailure)
		{
			return isHateSpeechResult.Error;
		}
		if (isHateSpeechResult.Value)
		{
			return Error.Forbidden("Content.Forbidden", "Your post violates our policy against hate speech and could not be published");
		}

		var userId = _userClaimsService.GetUserId();

		var post = await _repos.Posts.GetPostById(id, true);

		if (post is null)
		{
			return PostErrors.NotFound(id);
		}
		if (!post.AppUserId!.Equals(userId))
		{
			return PostErrors.Forbidden(id);
		}

		_mapper.Map(updatePostRequest, post);
		_repos.Posts.UpdatePost(post);
		await _repos.SaveAsync();

		var postResponse = _mapper.Map<PostResponse>(post);
		return postResponse;
	}

	public async Task<Result<IEnumerable<PostResponse>>> GetPostsByUserId(string userId, RequestParameters parameters)
	{
		var currentUserId = _userClaimsService.GetUserId();
		if (userId != currentUserId)
		{
			return Error.Forbidden("Users.Forbidden", "You don't have access on the required resource");
		}

		var posts = await _repos.Posts.GetPostsByUserId(userId, parameters, false);
		var postResponse = _mapper.Map<IEnumerable<PostResponse>>(posts);
		return postResponse.ToList();
	}
}