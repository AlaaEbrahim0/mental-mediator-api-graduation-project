using Application.Community;
using Application.Contracts;
using Application.Dtos.PostsDto;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.RequestParameters;

namespace Application.Services
{
	public class PostService : IPostService
	{
		private static readonly TimeSpan OrphanMediaLifetime = TimeSpan.FromHours(24);

		private readonly IRepositoryManager _repos;
		private readonly IMapper _mapper;
		private readonly IUserClaimsService _userClaimsService;
		private readonly IHateSpeechDetector _hateSpeechDetector;
		private readonly IStorageService _storageService;
		private readonly ICacheService _cacheService;
		private readonly ILogger<PostService>? _logger;

		public PostService(IRepositoryManager repos, IMapper mapper, IUserClaimsService userClaimsService, IHateSpeechDetector hateSpeechDetector, ICacheService cacheService, IStorageService storageService, ILogger<PostService>? logger = null)
		{
			_repos = repos;
			_mapper = mapper;
			_userClaimsService = userClaimsService;
			_hateSpeechDetector = hateSpeechDetector;
			_cacheService = cacheService;
			_storageService = storageService;
			_logger = logger;
		}

		public async Task<Result<IEnumerable<PostResponse>>> GetPosts(PostRequestParameters parameters)
		{
			var userId = _userClaimsService.GetUserId();
			var role = _userClaimsService.GetRole();
			var audience = await BuildAudienceAsync(userId, role);

			// The audience filter is decided by the caller's role, never by the
			// query string: the request parameters are bound from the URL first.
			parameters.IncludeDoctorsOnly = audience.CanReadDoctorsOnly;

			var posts = await _repos.Posts.GetAllPosts(parameters, false, audience);
			var responses = posts.Select(post => ToResponse(post, userId, role)).ToList();
			await PopulateLikeStatesAsync(posts, responses);
			return responses;
		}

		public async Task<Result<PostResponse?>> GetPostById(int id)
		{
			var userId = _userClaimsService.GetUserId();
			var role = _userClaimsService.GetRole();
			var post = await _repos.Posts.GetPostById(id, false);

			if (post is null || !await CanReadAsync(post, userId, role))
			{
				// A restricted or draft post answers exactly like a missing one,
				// so the endpoint does not confirm that it exists.
				return PostErrors.NotFound(id);
			}

			var response = ToResponse(post, userId, role);
			await PopulateLikeStateAsync(post, response);
			return response;
		}

		public async Task<Result<PostResponse?>> GetLatestDraft()
		{
			var userId = _userClaimsService.GetUserId();
			if (string.IsNullOrEmpty(userId))
			{
				return Error.Forbidden("Posts.Unauthorized", "Sign in to load a draft.");
			}

			var draft = await _repos.Posts.GetLatestDraft(userId, false);
			if (draft is null)
			{
				return Result<PostResponse?>.Success(null);
			}

			var response = ToResponse(draft, userId, _userClaimsService.GetRole());
			await PopulateLikeStateAsync(draft, response);
			return response;
		}

		public async Task<Result<PostResponse>> CreatePostAsync(CreatePostRequest postRequest)
		{
			var userId = _userClaimsService.GetUserId();
			var role = _userClaimsService.GetRole();
			var visibility = postRequest.Visibility ?? PostVisibility.Public;

			if (!postRequest.IsDraft &&
				await IsRestrictedContentAsync($"{postRequest.Title} {postRequest.Content}", "post"))
			{
				return Error.Forbidden("Content.Forbidden", "Your post violates our policy against hate speech and could not be published");
			}

			var audienceError = await ValidateAudienceAsync(visibility, postRequest.TargetGroupId, userId, role, postRequest.IsDraft);
			if (audienceError is not null)
			{
				return audienceError;
			}

			if (!postRequest.IsDraft && string.IsNullOrWhiteSpace(postRequest.Content))
			{
				return Error.Validation("Posts.ContentRequired", "Post content is required.");
			}

			var lengthError = ValidateLengths(postRequest.Title, postRequest.Content);
			if (lengthError is not null)
			{
				return lengthError;
			}

			if (!string.IsNullOrWhiteSpace(postRequest.Category) &&
				!PostTaxonomy.IsCategory(postRequest.Category))
			{
				return Error.Validation("Posts.InvalidCategory", "Choose one of the predefined categories.");
			}

			if (!PostTaxonomy.TryNormalizeTags(PostTaxonomy.SplitTags(postRequest.Tags), out var normalisedTags, out var tagError))
			{
				return Error.Validation("Posts.InvalidTags", tagError!);
			}

			if (postRequest.Visibility is not null &&
				visibility == PostVisibility.SpecificGroup &&
				postRequest.TargetGroupId is null &&
				!postRequest.IsDraft)
			{
				return PostErrors.SpecificGroupRequired();
			}

			var post = _mapper.Map<Post>(postRequest);
			post.Title = postRequest.Title?.Trim() ?? string.Empty;
			post.Content = postRequest.Content ?? string.Empty;
			post.Category = PostTaxonomy.NormalizeCategory(postRequest.Category);
			post.Tags = normalisedTags;
			post.Visibility = visibility;
			post.TargetChatRoomId = visibility == PostVisibility.SpecificGroup ? postRequest.TargetGroupId : null;
			post.IsDraft = postRequest.IsDraft;
			post.IsAnonymous = postRequest.IsAnonymous ?? false;

			var mediaResult = await ResolveMediaAsync(postRequest.Media, postRequest.PhotoPost, Array.Empty<PostMediaRequest>());
			if (mediaResult.IsFailure)
			{
				return mediaResult.Error;
			}
			ApplyMedia(post, mediaResult.Value);

			post.AppUserId = userId;
			post.PostedOn = DateTime.UtcNow;

			if (!(postRequest.IsAnonymous ?? false))
			{
				post.Username = _userClaimsService.GetUserName();
			}

			_repos.Posts.CreatePost(post);
			await _repos.SaveAsync();

			await AttachMediaAsync(post.Id, userId, mediaResult.Value);
			await _repos.SaveAsync();

			var response = ToResponse(post, userId, role);
			await PopulateLikeStateAsync(post, response);
			return response;
		}

		public async Task<Result<PostResponse>> DeletePost(int id)
		{
			var userId = _userClaimsService.GetUserId();
			var role = _userClaimsService.GetRole();

			var post = await _repos.Posts.GetPostById(id, true);
			if (post is null)
			{
				return PostErrors.NotFound(id);
			}
			if (!post.AppUserId!.Equals(userId) && !IsModerator(role))
			{
				return PostErrors.Forbidden(post.Id);
			}

			await DeleteTrackedMediaForPostAsync(post);
			_repos.Posts.DeletePost(post);
			await _repos.SaveAsync();

			var response = ToResponse(post, userId, role);
			await PopulateLikeStateAsync(post, response);
			return response;
		}

		public async Task<Result<PostResponse>> UpdatePost(int id, UpdatePostRequest updatePostRequest)
		{
			var userId = _userClaimsService.GetUserId();
			var role = _userClaimsService.GetRole();

			var post = await _repos.Posts.GetPostById(id, true);
			if (post is null)
			{
				return PostErrors.NotFound(id);
			}
			if (!post.AppUserId!.Equals(userId))
			{
				return PostErrors.Forbidden(id);
			}

			var visibility = updatePostRequest.Visibility ?? post.Visibility;
			var targetGroupId = updatePostRequest.Visibility is null
				? post.TargetChatRoomId
				: updatePostRequest.TargetGroupId;
			var becomesDraft = updatePostRequest.IsDraft;

			if (!becomesDraft &&
				await IsRestrictedContentAsync($"{updatePostRequest.Title} {updatePostRequest.Content}", "post"))
			{
				return Error.Forbidden("Content.Forbidden", "Your post violates our policy against hate speech and could not be published");
			}

			var audienceError = await ValidateAudienceAsync(visibility, targetGroupId, userId, role, becomesDraft);
			if (audienceError is not null)
			{
				return audienceError;
			}

			var lengthError = ValidateLengths(updatePostRequest.Title, updatePostRequest.Content);
			if (lengthError is not null)
			{
				return lengthError;
			}

			if (!becomesDraft)
			{
				if (string.IsNullOrWhiteSpace(updatePostRequest.Content))
				{
					return Error.Validation("Posts.ContentRequired", "Post content is required.");
				}
				if (updatePostRequest.Category is not null &&
					!PostTaxonomy.IsCategory(updatePostRequest.Category))
				{
					return Error.Validation("Posts.InvalidCategory", "Choose one of the predefined categories.");
				}
				if (updatePostRequest.Category is null &&
					!PostTaxonomy.IsCategory(post.Category))
				{
					// Preserve compatibility with edit clients that only send
					// title/content; the stored category still has to be valid.
					return Error.Validation("Posts.InvalidCategory", "Choose one of the predefined categories.");
				}
				if (visibility == PostVisibility.SpecificGroup && targetGroupId is null)
				{
					return PostErrors.SpecificGroupRequired();
				}
			}

			if (updatePostRequest.Category is not null)
			{
				if (!string.IsNullOrWhiteSpace(updatePostRequest.Category) &&
					!PostTaxonomy.IsCategory(updatePostRequest.Category))
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

			post.Title = updatePostRequest.Title?.Trim() ?? string.Empty;
			post.Content = updatePostRequest.Content ?? string.Empty;
			post.Visibility = visibility;
			post.TargetChatRoomId = visibility == PostVisibility.SpecificGroup ? targetGroupId : null;
			post.IsDraft = becomesDraft;

			var previousMedia = PostMediaCodec.ToRequestList(post.MediaJson);
			if (updatePostRequest.Media is not null)
			{
				var mediaResult = await ResolveMediaAsync(updatePostRequest.Media, updatePostRequest.PhotoPost, previousMedia);
				if (mediaResult.IsFailure)
				{
					return mediaResult.Error;
				}
				ApplyMedia(post, mediaResult.Value);
			}
			else if (updatePostRequest.PhotoPost is not null)
			{
				// Legacy edit clients send one file and no media list: append it.
				var mediaResult = await ResolveMediaAsync(null, updatePostRequest.PhotoPost, previousMedia);
				if (mediaResult.IsFailure)
				{
					return mediaResult.Error;
				}
				ApplyMedia(post, mediaResult.Value);
			}

			var currentMedia = PostMediaCodec.ToRequestList(post.MediaJson);
			await DetachAndDeleteRemovedMediaAsync(post, previousMedia, currentMedia);

			if (updatePostRequest.IsAnonymous.HasValue)
			{
				post.IsAnonymous = updatePostRequest.IsAnonymous.Value;
			}

			_repos.Posts.UpdatePost(post);
			await _repos.SaveAsync();

			await AttachMediaAsync(post.Id, userId, currentMedia);
			await _repos.SaveAsync();

			var response = ToResponse(post, userId, role);
			await PopulateLikeStateAsync(post, response);
			return response;
		}

		public async Task<Result<IEnumerable<PostResponse>>> GetPostsByUserId(string userId, PostRequestParameters parameters)
		{
			var currentUserId = _userClaimsService.GetUserId();
			if (userId != currentUserId)
			{
				return Error.Forbidden("Users.Forbidden", "You don't have access to the required resource");
			}

			var role = _userClaimsService.GetRole();
			parameters.IncludeDoctorsOnly = CanReadDoctorsOnly(role);

			var posts = await _repos.Posts.GetPostsByUserId(userId, parameters, false);
			var responses = posts.Select(post => ToResponse(post, currentUserId, role)).ToList();
			await PopulateLikeStatesAsync(posts, responses);
			return responses;
		}

		public async Task<Result<PostResponse>> SetLike(int id, bool liked)
		{
			var userId = _userClaimsService.GetUserId();
			var role = _userClaimsService.GetRole();

			var post = await _repos.Posts.GetPostById(id, false);
			if (post is null || post.IsDraft || !await CanReadAsync(post, userId, role))
			{
				return PostErrors.NotFound(id);
			}

			await _repos.Posts.SetLike(id, userId, liked);
			await _repos.SaveAsync();

			var response = ToResponse(post, userId, role);
			await PopulateLikeStateAsync(post, response);
			return response;
		}

		public async Task<Result<StoredMedia>> UploadMediaAsync(IFormFile file)
		{
			var userId = _userClaimsService.GetUserId();
			if (string.IsNullOrEmpty(userId))
			{
				return Error.Forbidden("Posts.Unauthorized", "Sign in to upload media.");
			}

			var upload = await _storageService.UploadMedia(file);
			if (upload.IsFailure)
			{
				return upload.Error;
			}

			_repos.PostMediaAssets.Add(new PostMediaAsset
			{
				Url = upload.Value.Url,
				PublicId = upload.Value.PublicId ?? string.Empty,
				MediaType = upload.Value.Type,
				OwnerId = userId,
				CreatedAtUtc = DateTime.UtcNow,
			});
			await _repos.SaveAsync();

			await CleanupOrphanedMediaAsync(userId);
			return upload.Value;
		}

		public async Task<Result> DeleteUnattachedMediaAsync(string url)
		{
			var userId = _userClaimsService.GetUserId();
			if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(url))
			{
				return Result.Success();
			}

			var asset = await _repos.PostMediaAssets.GetByUrlAsync(url, true);
			if (asset is null || !asset.OwnerId.Equals(userId, StringComparison.Ordinal))
			{
				// Never reveal or delete another user's upload.
				return Result.Success();
			}
			if (asset.PostId is not null)
			{
				return PostErrors.MediaAttached();
			}

			var deleted = await _storageService.DeleteMediaAsync(asset.PublicId, asset.MediaType);
			if (deleted.IsFailure)
			{
				return deleted.Error;
			}

			_repos.PostMediaAssets.Remove(asset);
			await _repos.SaveAsync();
			return Result.Success();
		}

		private async Task<PostAudience> BuildAudienceAsync(string userId, string role)
		{
			var groupIds = string.IsNullOrEmpty(userId)
				? Array.Empty<int>()
				: await _repos.ChatRooms.GetActiveRoomIdsAsync(userId);

			return new PostAudience(CanReadDoctorsOnly(role), groupIds);
		}

		private async Task<Error?> ValidateAudienceAsync(
			PostVisibility visibility,
			int? targetGroupId,
			string userId,
			string role,
			bool isDraft)
		{
			if (visibility == PostVisibility.DoctorsOnly && !CanReadDoctorsOnly(role))
			{
				return PostErrors.DoctorsOnlyForbidden();
			}

			if (visibility != PostVisibility.SpecificGroup)
			{
				return null;
			}

			if (targetGroupId is null)
			{
				return isDraft ? null : PostErrors.SpecificGroupRequired();
			}

			var membership = await _repos.ChatRooms.GetMembershipAsync(targetGroupId.Value, userId, false);
			if (membership is null || membership.LeftAtUtc is not null)
			{
				return PostErrors.SpecificGroupForbidden();
			}

			var room = await _repos.ChatRooms.GetRoomByIdAsync(targetGroupId.Value, false);
			if (room is null || room.IsArchived)
			{
				return PostErrors.SpecificGroupNotFound();
			}

			return null;
		}

		private Task<bool> CanReadAsync(Post post, string userId, string role) =>
			PostAccessPolicy.CanReadAsync(post, userId, role, _repos.ChatRooms);

		private static bool CanReadDoctorsOnly(string role) => PostAccessPolicy.IsDoctorOrAdmin(role);

		private static bool IsModerator(string role) => PostAccessPolicy.IsModerator(role);

		private static Error? ValidateLengths(string? title, string? content)
		{
			if ((title?.Trim().Length ?? 0) > PostTaxonomy.MaxTitleLength)
			{
				return Error.Validation(
					"Posts.TitleTooLong",
					$"Post titles are limited to {PostTaxonomy.MaxTitleLength} characters.");
			}

			if ((content?.Length ?? 0) > PostTaxonomy.MaxContentLength)
			{
				return Error.Validation(
					"Posts.ContentTooLong",
					$"Post content is limited to {PostTaxonomy.MaxContentLength} characters.");
			}

			return null;
		}

		private async Task<bool> IsRestrictedContentAsync(string content, string contentType)
		{
			var result = await _hateSpeechDetector.IsHateSpeech(content);

			if (result.IsFailure)
			{
				_logger?.LogWarning(
					"Content moderation unavailable for a {ContentType}; allowing publication for review. {Code}: {Description}",
					contentType,
					result.Error.Code,
					result.Error.Description);
				return false;
			}

			return result.Value;
		}

		private async Task<Result<List<PostMediaRequest>>> ResolveMediaAsync(
			string? mediaJson,
			IFormFile? photo,
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
				item.Url = PostMediaCodec.NormalizeSecureUrl(item.Url);
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

		private async Task AttachMediaAsync(int postId, string userId, IReadOnlyCollection<PostMediaRequest> media)
		{
			if (media.Count == 0)
			{
				return;
			}

			var urls = media.Select(item => item.Url).ToArray();
			var assets = await _repos.PostMediaAssets.GetByUrlsAsync(urls, true);
			foreach (var asset in assets)
			{
				if (asset.OwnerId.Equals(userId, StringComparison.Ordinal) && asset.PostId is null)
				{
					asset.PostId = postId;
				}
			}
		}

		private async Task DetachAndDeleteRemovedMediaAsync(
			Post post,
			IReadOnlyCollection<PostMediaRequest> previous,
			IReadOnlyCollection<PostMediaRequest> current)
		{
			var currentUrls = current.Select(item => item.Url).ToHashSet(StringComparer.Ordinal);
			var removedUrls = previous.Select(item => item.Url)
				.Where(url => !currentUrls.Contains(url))
				.ToArray();

			if (removedUrls.Length == 0)
			{
				return;
			}

			var assets = await _repos.PostMediaAssets.GetAttachedToPostAsync(post.Id, true);
			foreach (var asset in assets.Where(asset => removedUrls.Contains(asset.Url, StringComparer.Ordinal)))
			{
				var deleted = await _storageService.DeleteMediaAsync(asset.PublicId, asset.MediaType);
				if (deleted.IsSuccess)
				{
					_repos.PostMediaAssets.Remove(asset);
				}
			}
		}

		private async Task DeleteTrackedMediaForPostAsync(Post post)
		{
			var assets = await _repos.PostMediaAssets.GetAttachedToPostAsync(post.Id, true);
			foreach (var asset in assets)
			{
				await _storageService.DeleteMediaAsync(asset.PublicId, asset.MediaType);
				_repos.PostMediaAssets.Remove(asset);
			}
		}

		private async Task CleanupOrphanedMediaAsync(string userId)
		{
			var cutoff = DateTime.UtcNow.Subtract(OrphanMediaLifetime);
			var orphans = await _repos.PostMediaAssets.GetOrphansAsync(userId, cutoff, 10, true);

			foreach (var asset in orphans)
			{
				var deleted = await _storageService.DeleteMediaAsync(asset.PublicId, asset.MediaType);
				if (deleted.IsSuccess)
				{
					_repos.PostMediaAssets.Remove(asset);
				}
			}

			if (orphans.Count > 0)
			{
				await _repos.SaveAsync();
			}
		}

		private PostResponse ToResponse(Post post, string currentUserId, string role)
		{
			var response = _mapper.Map<PostResponse>(post);

			response.PostPhotoUrl = response.PostPhotoUrl is null
				? null
				: PostMediaCodec.NormalizeSecureUrl(response.PostPhotoUrl);
			foreach (var item in response.Media)
			{
				item.Url = PostMediaCodec.NormalizeSecureUrl(item.Url);
			}
			response.PhotoUrl = response.PhotoUrl is null
				? null
				: PostMediaCodec.NormalizeSecureUrl(response.PhotoUrl);

			response.IsOwnPost = !string.IsNullOrEmpty(currentUserId) &&
				string.Equals(post.AppUserId, currentUserId, StringComparison.Ordinal);

			if (post.IsAnonymous)
			{
				// Moderators retain ownership for audit and enforcement; everyone
				// else, including the author, sees no identity in the payload.
				if (!IsModerator(role))
				{
					response.AppUserId = null;
				}
				response.Username = null;
				response.PhotoUrl = null;
			}

			return response;
		}

		private async Task PopulateLikeStateAsync(Post post, PostResponse response)
		{
			if (post.IsDraft)
			{
				response.LikesCount = 0;
				response.IsLikedByCurrentUser = false;
				return;
			}

			var (count, liked) = await _repos.Posts.GetLikeState(post.Id, _userClaimsService.GetUserId());
			response.LikesCount = count;
			response.IsLikedByCurrentUser = liked;
		}

		private async Task PopulateLikeStatesAsync(IEnumerable<Post> posts, List<PostResponse> responses)
		{
			var published = posts.Where(post => !post.IsDraft).ToList();
			if (published.Count == 0)
			{
				foreach (var response in responses)
				{
					response.LikesCount = 0;
					response.IsLikedByCurrentUser = false;
				}
				return;
			}

			var states = await _repos.Posts.GetLikeStates(
				published.Select(post => post.Id),
				_userClaimsService.GetUserId());

			foreach (var response in responses)
			{
				if (states.TryGetValue(response.Id, out var state))
				{
					response.LikesCount = state.Count;
					response.IsLikedByCurrentUser = state.IsLiked;
				}
			}
		}
	}
}
