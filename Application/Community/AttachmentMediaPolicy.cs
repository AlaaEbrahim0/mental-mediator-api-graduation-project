using Application.Contracts;
using Application.Dtos.PostsDto;
using Domain.Entities;
using Domain.Repositories;
using Shared;

namespace Application.Community;

/// <summary>
/// Validates and resolves a single image/video attachment submitted with a
/// comment or reply. Attachments are uploaded first through the post media
/// endpoint, so this only accepts a URL the same user already owns and that is
/// not attached anywhere else.
/// </summary>
public static class AttachmentMediaPolicy
{
	private const string Image = "image";
	private const string Video = "video";

	public static async Task<Result<PostMediaAsset?>> ResolveAsync(
		IPostMediaAssetRepository assets,
		IStorageService? storage,
		string? mediaUrl,
		string? mediaType,
		string userId,
		Error invalidMedia)
	{
		var hasUrl = !string.IsNullOrWhiteSpace(mediaUrl);
		var hasType = !string.IsNullOrWhiteSpace(mediaType);

		if (!hasUrl && !hasType)
		{
			return Result<PostMediaAsset?>.Success(null);
		}

		if (!hasUrl || !hasType || string.IsNullOrEmpty(userId))
		{
			return invalidMedia;
		}

		var normalizedType = mediaType!.Trim().ToLowerInvariant();
		if (normalizedType is not (Image or Video))
		{
			return invalidMedia;
		}

		var normalizedUrl = PostMediaCodec.NormalizeSecureUrl(mediaUrl!.Trim());
		if (storage is null || !storage.IsManagedMediaUrl(normalizedUrl))
		{
			return invalidMedia;
		}

		var asset = await assets.GetByUrlAsync(normalizedUrl, true);
		if (asset is null && !string.Equals(normalizedUrl, mediaUrl.Trim(), StringComparison.Ordinal))
		{
			// Tolerate a legacy http spelling without accepting any other host.
			asset = await assets.GetByUrlAsync(mediaUrl.Trim(), true);
		}

		if (asset is null ||
			!asset.OwnerId.Equals(userId, StringComparison.Ordinal) ||
			asset.PostId is not null ||
			asset.CommentId is not null ||
			asset.ReplyId is not null ||
			!asset.MediaType.Equals(normalizedType, StringComparison.OrdinalIgnoreCase))
		{
			return invalidMedia;
		}

		return Result<PostMediaAsset?>.Success(asset);
	}
}
