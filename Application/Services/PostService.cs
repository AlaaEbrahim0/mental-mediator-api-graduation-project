using Application.Community;
using Application.Contracts;
using Application.Dtos.PostsDto;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
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
			var currentUserId = _userClaimsService.GetUserId();

			// The audience filter is decided by the caller's role, never by the
			// query string: the request parameters are bound from the URL first,
			// so the flag has to be overwritten here.
			parameters.IncludeDoctorsOnly = CanReadDoctorsOnly(_userClaimsService.GetRole());

			var posts = await _repos.Posts.GetAllPosts(parameters, false);
			var postResponse = posts.Select(post => ToResponse(post, currentUserId)).ToList();
			await PopulateLikeState(posts, postResponse);
			return postResponse;
		}

		public async Task<Result<PostResponse?>> GetPostById(int id)
		{
			var post = await _repos.Posts.GetPostById(id, false);
			if (post is null || !CanRead(post, _userClaimsService.GetRole()))
			{
				// A Doctors Only post answers exactly like a missing one, so the
				// endpoint does not confirm that it exists.
				return PostErrors.NotFound(id);
			}

			var postResponse = ToResponse(post, _userClaimsService.GetUserId());
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

			if (postRequest.Visibility == PostVisibility.SpecificGroup)
			{
				return PostErrors.VisibilityUnsupported();
			}
			if (postRequest.Visibility == PostVisibility.DoctorsOnly &&
				!CanReadDoctorsOnly(_userClaimsService.GetRole()))
			{
				return PostErrors.DoctorsOnlyForbidden();
			}

			var post = _mapper.Map<Post>(postRequest);
			post.Title = postRequest.Title?.Trim() ?? string.Empty;
			post.Category = PostTaxonomy.NormalizeCategory(postRequest.Category);
			post.Tags = PostTaxonomy.NormalizeTagsCsv(postRequest.Tags);
			post.Visibility = postRequest.Visibility;

			var mediaResult = await ResolveMediaAsync(postRequest.Media, postRequest.PhotoPost, Array.Empty<PostMediaRequest>());
			if (mediaResult.IsFailure)
			{
				return mediaResult.Error;
			}
			ApplyMedia(post, mediaResult.Value);

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

			var postResponse = ToResponse(post, userId);
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

			var postResponse = ToResponse(post, userId);
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

			if (string.IsNullOrWhiteSpace(updatePostRequest.Content))
			{
				return Error.Validation("Posts.ContentRequired", "Post content is required.");
			}

			if (updatePostRequest.Category is not null)
			{
				if (!PostTaxonomy.IsCategory(updatePostRequest.Category))
				{
					return Error.Validation("Posts.InvalidCategory", "Choose one of the predefined categories.");
				}
				post.Category = PostTaxonomy.NormalizeCategory(updatePostRequest.Category);
			}

			if (updatePostRequest.Tags is not null)
			{
				if (!PostTaxonomy.TryNormalizeTags(PostTaxonomy.SplitTags(updatePostRequest.Tags), out var csv, out var tagError))
				{
					return Error.Validation("Posts.InvalidTags", tagError!);
				}
				post.Tags = csv;
			}

			// The audience cannot be changed after publishing in this iteration;
			// the edit modal does not send it and the property is ignored here.
			post.Title = updatePostRequest.Title?.Trim() ?? string.Empty;
			post.Content = updatePostRequest.Content;

			if (updatePostRequest.Media is not null)
			{
				var existing = PostMediaCodec.ToRequestList(post.MediaJson);
				var mediaResult = await ResolveMediaAsync(updatePostRequest.Media, updatePostRequest.PhotoPost, existing);
				if (mediaResult.IsFailure)
				{
					return mediaResult.Error;
				}
				ApplyMedia(post, mediaResult.Value);
			}
			else if (updatePostRequest.PhotoPost is not null)
			{
				// Legacy edit clients send one file and no media list: append it.
				var existing = PostMediaCodec.ToRequestList(post.MediaJson);
				var mediaResult = await ResolveMediaAsync(null, updatePostRequest.PhotoPost, existing);
				if (mediaResult.IsFailure)
				{
					return mediaResult.Error;
				}
				ApplyMedia(post, mediaResult.Value);
			}

			_repos.Posts.UpdatePost(post);
			await _repos.SaveAsync();

			var postResponse = ToResponse(post, userId);
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

			parameters.IncludeDoctorsOnly = CanReadDoctorsOnly(_userClaimsService.GetRole());

			var posts = await _repos.Posts.GetPostsByUserId(userId, parameters, false);
			var postResponse = posts.Select(post => ToResponse(post, currentUserId)).ToList();
			await PopulateLikeState(posts, postResponse);
			return postResponse;
		}

		public async Task<Result<PostResponse>> SetLike(int id, bool liked)
		{
			var userId = _userClaimsService.GetUserId();

			var post = await _repos.Posts.GetPostById(id, false);
			if (post is null || !CanRead(post, _userClaimsService.GetRole())) return PostErrors.NotFound(id);

			await _repos.Posts.SetLike(id, userId, liked);
			await _repos.SaveAsync();

			// Read the like state *after* the write has been committed, otherwise the
			// response reports the previous count and the client renders stale state.
			var response = ToResponse(post, userId);
			await PopulateLikeState(post, response);
			return response;
		}

		private static bool IsDoctorOrAdmin(string role) =>
			string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

		private static bool CanReadDoctorsOnly(string role) => IsDoctorOrAdmin(role);

		private static bool CanRead(Post post, string role) =>
			post.Visibility != PostVisibility.DoctorsOnly || CanReadDoctorsOnly(role);

		/// <summary>
		/// Builds the final media list from the submitted JSON and an optional
		/// legacy single photo. Every URL must belong to the configured
		/// Cloudinary cloud, so a post cannot embed arbitrary external assets.
		/// </summary>
		private async Task<Result<List<PostMediaRequest>>> ResolveMediaAsync(
			string? mediaJson,
			Microsoft.AspNetCore.Http.IFormFile? photo,
			IReadOnlyCollection<PostMediaRequest> existing)
		{
			var items = existing.ToList();

			if (!string.IsNullOrWhiteSpace(mediaJson))
			{
				if (!PostMediaCodec.TryParse(mediaJson, out var submitted, out var mediaError))
				{
					return Error.Validation("Posts.InvalidMedia", mediaError!);
				}
				items = submitted;
			}

			if (photo is not null)
			{
				var uploadResult = await _storageService.UploadPhoto(photo);
				if (uploadResult.IsFailure)
				{
					return uploadResult.Error;
				}
				items.Add(new PostMediaRequest { Url = uploadResult.Value, Type = "image" });
			}

			foreach (var item in items)
			{
				if (!_storageService.IsManagedMediaUrl(item.Url))
				{
					return Error.Validation(
						"Posts.InvalidMedia",
						"Media must be uploaded through the post media endpoint first.");
				}
			}

			if (!PostMediaCodec.TryParse(PostMediaCodec.Serialize(items), out var normalised, out var error))
			{
				return Error.Validation("Posts.InvalidMedia", error!);
			}

			return normalised;
		}

		private static void ApplyMedia(Post post, List<PostMediaRequest> media)
		{
			post.MediaJson = media.Count > 0 ? PostMediaCodec.Serialize(media) : null;
			post.PostPhotoUrl = media.FirstOrDefault(item => item.Type == "image")?.Url;
		}

		private PostResponse ToResponse(Post post, string currentUserId)
		{
			var response = _mapper.Map<PostResponse>(post);

			// Anonymous posts must not expose the author's id to anybody except
			// the author themselves, who manages the post through IsOwnPost.
			response.IsOwnPost = !string.IsNullOrEmpty(currentUserId) &&
			                     string.Equals(post.AppUserId, currentUserId, StringComparison.Ordinal);
			if (post.IsAnonymous)
			{
				response.AppUserId = null;
				response.Username = null;
				response.PhotoUrl = null;
			}

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
