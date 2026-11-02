using Application.Dtos.DoctorRatings;
using Shared;

namespace Application.Contracts;

public interface IDoctorRatingService
{
	Task<Result<DoctorRatingResponse>> UpsertAsync(string doctorId, UpsertDoctorRatingRequest request);
	Task<Result<DoctorRatingResponse>> GetMineAsync(string doctorId);
	Task<Result> DeleteAsync(string doctorId);
	Task<Result<IReadOnlyList<DoctorRatingResponse>>> GetRecentAsync(string doctorId);
	Task<Result<DoctorRatingSummaryResponse>> GetSummaryAsync(string doctorId);
}
