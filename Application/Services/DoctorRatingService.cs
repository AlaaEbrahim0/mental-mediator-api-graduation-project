using Application.Contracts;
using Application.Dtos.DoctorRatings;
using Domain.Entities;
using Domain.Errors;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Services;

/// <summary>
/// Ratings are one row per (doctor, patient): POST edits the existing vote
/// instead of duplicating it, so there is no "already rated" failure to
/// surface. Comments run through the same hate-speech gate as posts, with
/// the same allow-on-unavailable behaviour.
/// </summary>
public class DoctorRatingService : IDoctorRatingService
{
	private const int RecentWindow = 20;

	private readonly IRepositoryManager _repos;
	private readonly IUserClaimsService _userClaimsService;
	private readonly IHateSpeechDetector _hateSpeechDetector;
	private readonly ILogger<DoctorRatingService> _logger;

	public DoctorRatingService(
		IRepositoryManager repos,
		IUserClaimsService userClaimsService,
		IHateSpeechDetector hateSpeechDetector,
		ILogger<DoctorRatingService> logger)
	{
		_repos = repos;
		_userClaimsService = userClaimsService;
		_hateSpeechDetector = hateSpeechDetector;
		_logger = logger;
	}

	public async Task<Result<DoctorRatingResponse>> UpsertAsync(string doctorId, UpsertDoctorRatingRequest request)
	{
		var userId = _userClaimsService.GetUserId();
		var role = _userClaimsService.GetRole();

		if (!role.Equals("User", StringComparison.OrdinalIgnoreCase))
		{
			return DoctorRatingErrors.PatientsOnly();
		}

		if (string.Equals(doctorId, userId, StringComparison.Ordinal))
		{
			return DoctorRatingErrors.SelfRating();
		}

		var doctor = await _repos.Doctors.GetById(doctorId, false);
		if (doctor is null)
		{
			return DoctorRatingErrors.DoctorNotFound(doctorId);
		}

		var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
		if (comment is not null && await IsRestrictedCommentAsync(comment))
		{
			return Error.Forbidden(
				"Content.Forbidden", "Your review violates our policy against hate speech and could not be published");
		}

		var existing = await _repos.DoctorRatings.GetForRaterAsync(doctorId, userId, true);
		DoctorRating rating;
		if (existing is null)
		{
			rating = new DoctorRating
			{
				DoctorId = doctorId,
				RaterUserId = userId,
				DisplayName = _userClaimsService.GetUserName(),
				Stars = request.Stars,
				Comment = comment,
				CreatedAtUtc = DateTime.UtcNow,
			};
			_repos.DoctorRatings.Create(rating);
		}
		else
		{
			existing.Stars = request.Stars;
			existing.Comment = comment;
			existing.UpdatedAtUtc = DateTime.UtcNow;
			rating = existing;
		}

		await _repos.SaveAsync();
		return ToResponse(rating);
	}

	public async Task<Result<DoctorRatingResponse>> GetMineAsync(string doctorId)
	{
		var userId = _userClaimsService.GetUserId();
		var rating = await _repos.DoctorRatings.GetForRaterAsync(doctorId, userId, false);
		if (rating is null)
		{
			return DoctorRatingErrors.NotRated();
		}

		return ToResponse(rating);
	}

	public async Task<Result> DeleteAsync(string doctorId)
	{
		var userId = _userClaimsService.GetUserId();
		var rating = await _repos.DoctorRatings.GetForRaterAsync(doctorId, userId, true);
		if (rating is null)
		{
			return DoctorRatingErrors.NotRated();
		}

		_repos.DoctorRatings.Delete(rating);
		await _repos.SaveAsync();
		return Result.Success();
	}

	public async Task<Result<IReadOnlyList<DoctorRatingResponse>>> GetRecentAsync(string doctorId)
	{
		var ratings = await _repos.DoctorRatings.GetRecentAsync(doctorId, RecentWindow);
		return ratings.Select(ToResponse).ToList();
	}

	public async Task<Result<DoctorRatingSummaryResponse>> GetSummaryAsync(string doctorId)
	{
		var summary = await _repos.DoctorRatings.GetSummaryAsync(doctorId);
		return new DoctorRatingSummaryResponse
		{
			Average = summary?.Average,
			Count = summary?.Count ?? 0,
		};
	}

	private async Task<bool> IsRestrictedCommentAsync(string comment)
	{
		var result = await _hateSpeechDetector.IsHateSpeech(comment);

		if (result.IsFailure)
		{
			_logger.LogWarning(
				"Content moderation unavailable for a rating comment; allowing publication for review. {Code}: {Description}",
				result.Error.Code,
				result.Error.Description);
			return false;
		}

		return result.Value;
	}

	private static DoctorRatingResponse ToResponse(DoctorRating rating) => new()
	{
		Id = rating.Id,
		DoctorId = rating.DoctorId,
		DisplayName = rating.DisplayName,
		Stars = rating.Stars,
		Comment = rating.Comment,
		CreatedAtUtc = rating.CreatedAtUtc,
		UpdatedAtUtc = rating.UpdatedAtUtc,
	};
}
