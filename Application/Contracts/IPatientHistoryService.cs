using Application.Dtos.HistoryDtos;
using Shared;

namespace Application.Contracts;

public interface IPatientHistoryService
{
	/// <summary>The signed-in patient's own authorized history.</summary>
	Task<Result<PatientHistoryResponse>> GetMyHistoryAsync(
		int pageNumber,
		int pageSize,
		string? category = null,
		DateTime? fromUtc = null,
		DateTime? toUtc = null);

	/// <summary>
	/// One patient's history as visible to a doctor with an existing care
	/// relationship. Private notes from other doctors are never included.
	/// </summary>
	Task<Result<PatientHistoryResponse>> GetPatientHistoryAsync(
		string patientId,
		int pageNumber,
		int pageSize,
		string? category = null,
		DateTime? fromUtc = null,
		DateTime? toUtc = null);
}
