using Application.Dtos.HistoryDtos;
using Shared;

namespace Application.Contracts;

public interface IPatientHistoryService
{
	/// <summary>The signed-in patient's own authorized history.</summary>
	Task<Result<PatientHistoryResponse>> GetMyHistoryAsync(int pageNumber, int pageSize);

	/// <summary>
	/// One patient's history as visible to a doctor with an existing care
	/// relationship. Private notes from other doctors are never included.
	/// </summary>
	Task<Result<PatientHistoryResponse>> GetPatientHistoryAsync(
		string patientId,
		int pageNumber,
		int pageSize);
}
