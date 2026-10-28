using Application.Contracts;
using Application.Dtos.PostsDto;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Shared;
using Shared.RequestParameters;

namespace Application.Services
{
    public class PostService : IPostService
	{
		private readonly IRepositoryManager _repos;
		private readonly IMapper _mapper;
		private readonly IUserClaimsService _userClaimsService;
		private readonly IHateSpeechDetector _hateSpeechDetector;
		private readonly IStorageService _storageService;
		private readonly ICacheService _cacheService;

		public PostService(IRepositoryManager repos, IMapper mapper, IUserClaimsService userClaimsService, IHateSpeechDetector hateSpeechDetector, ICacheService cacheService, IStorageService storageService)
		{
			_repos = repos;
			_mapper = mapper;
			_userClaimsService = userClaimsService;
			_hateSpeechDetector = hateSpeechDetector;
			_cacheService = cacheService;
			_storageService = storageService;
		}

		public async Task<Result<IEnumerable<PostResponse>>> GetPosts(PostRequestParameters parameters)
		{
			var posts = await _repos.Posts.GetAllPosts(parameters, false);
			var postResponse = _mapper.Map<IEnumerable<PostResponse>>(posts);
			await PopulateLikeState(posts, postResponse);
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
			await PopulateLikeState(post, postResponse);
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

			if (postRequest.PhotoPost is not null)
			{
				// An unchecked upload result wrote null over the photo URL, so the
				// post was saved as "successful" with the image silently dropped.
				var uploadResult = await _storageService.UploadPhoto(postRequest.PhotoPost);
				if (uploadResult.IsFailure)
				{
					return uploadResult.Error;
				}

				post.PostPhotoUrl = uploadResult.Value;
			}

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
			await PopulateLikeState(post, postResponse);
			return postResponse;
		}

		public async Task<Result<PostResponse>> DeletePost(int id)
		{
			var userId = _userClaimsService.GetUserId();
			var userRole = _userClaimsService.GetRole();

			var post = await _repos.Posts.GetPostById(id, true);
			if (post is null)
			{
				return PostErrors.NotFound(id);
			}
			if (!post.AppUserId!.Equals(userId) && userRole != "Admin")
			{
				return PostErrors.Forbidden(post.Id);
			}

			_repos.Posts.DeletePost(post);
			await _repos.SaveAsync();

			var postResponse = _mapper.Map<PostResponse>(post);
			await PopulateLikeState(post, postResponse);
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

			if (updatePostRequest.PhotoPost is not null)
			{
				// Fail before touching the entity: a failed upload used to null out
				// the photo URL of an existing post, destroying a valid reference.
				var uploadResult = await _storageService.UploadPhoto(updatePostRequest.PhotoPost);
				if (uploadResult.IsFailure)
				{
					return uploadResult.Error;
				}

				post.PostPhotoUrl = uploadResult.Value;
			}

			var existingCategory = post.Category;
			var existingTags = post.Tags;
			_mapper.Map(updatePostRequest, post);
			post.Category = updatePostRequest.Category ?? existingCategory;
			post.Tags = updatePostRequest.Tags ?? existingTags;
			_repos.Posts.UpdatePost(post);
			await _repos.SaveAsync();


			var postResponse = _mapper.Map<PostResponse>(post);
			await PopulateLikeState(post, postResponse);
			return postResponse;
		}

		public async Task<Result<IEnumerable<PostResponse>>> GetPostsByUserId(string userId, PostRequestParameters parameters)
		{
			var currentUserId = _userClaimsService.GetUserId();
			if (userId != currentUserId)
			{
				return Error.Forbidden("Users.Forbidden", "You don't have access to the required resource");
			}

			var posts = await _repos.Posts.GetPostsByUserId(userId, parameters, false);
			var postResponse = _mapper.Map<IEnumerable<PostResponse>>(posts);
			await PopulateLikeState(posts, postResponse);
			return postResponse.ToList();
		}

		public async Task<Result<PostResponse>> SetLike(int id, bool liked)
		{
			var userId = _userClaimsService.GetUserId();

			var post = await _repos.Posts.GetPostById(id, false);
			if (post is null) return PostErrors.NotFound(id);

			await _repos.Posts.SetLike(id, userId, liked);
			await _repos.SaveAsync();

			// Read the like state *after* the write has been committed, otherwise the
			// response reports the previous count and the client renders stale state.
			var response = _mapper.Map<PostResponse>(post);
			await PopulateLikeState(post, response);
			return response;
		}

		private async Task PopulateLikeState(Post post, PostResponse response)
		{
			var (count, liked) = await _repos.Posts.GetLikeState(post.Id, _userClaimsService.GetUserId());
			response.LikesCount = count;
			response.IsLikedByCurrentUser = liked;
		}

		private async Task PopulateLikeState(IEnumerable<Post> posts, IEnumerable<PostResponse> responses)
		{
			var responseById = responses.ToDictionary(x => x.Id);
			foreach (var post in posts)
				if (responseById.TryGetValue(post.Id, out var response)) await PopulateLikeState(post, response);
		}

	}
}
