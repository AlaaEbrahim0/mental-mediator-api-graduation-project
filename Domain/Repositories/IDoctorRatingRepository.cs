using Domain.Entities;

namespace Domain.Repositories;

/// <summary>Average rounded to one decimal plus the vote count. Null when a
/// doctor has no ratings.</summary>
public sealed record DoctorRatingSummary(double Average, int Count);

public interface IDoctorRatingRepository
{
	Task<DoctorRating?> GetForRaterAsync(string doctorId, string raterUserId, bool trackChanges);
	Task<DoctorRatingSummary?> GetSummaryAsync(string doctorId);

	/// <summary>Summaries only for doctors that have at least one rating.</summary>
	Task<IReadOnlyDictionary<string, DoctorRatingSummary>> GetSummariesAsync(IEnumerable<string> doctorIds);

	Task<IReadOnlyList<DoctorRating>> GetRecentAsync(string doctorId, int take);

	void Create(DoctorRating rating);
	void Delete(DoctorRating rating);
}
