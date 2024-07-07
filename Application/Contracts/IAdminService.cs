using Application.Dtos.ReportsDtos;
using Shared;

namespace Application.Contracts;

public interface IAdminService
{
	Task<Result<AdminReportResponse>> GetSystemSummary();
}