using Application.Contracts;
using Application.Dtos.ReportsDtos;
using AutoMapper;
using Shared;

namespace Application.Services;

public class AdminService : IAdminService
{
	private readonly IRepositoryManager _repoManager;
	private readonly IUserClaimsService
		_userClaimsService;
	private readonly IStorageService _storageService;
	private readonly IMapper _mapper;

	public AdminService(IUserClaimsService userClaimsService, IStorageService storageService, IMapper mapper,
		IRepositoryManager repoManager)
	{
		_userClaimsService = userClaimsService;
		_storageService = storageService;
		_mapper = mapper;
		_repoManager = repoManager;
	}

	public async Task<Result<AdminReportResponse>> GetSystemSummary()
	{
		var totalAppointments = await _repoManager.Appointments.GetTotalAppointments();
		var totalPostsCount = await _repoManager.Posts.GetCount();
		var totalDoctorsCount = await _repoManager.Doctors.GetCount();
		var totalUsersCount = await _repoManager.Users.GetCount();
		var depressionTestCount = await _repoManager.DepressionTestResults.CalculateResultCounts();
		var depressionTestAgeGroupDistribution = await _repoManager.DepressionTestResults.CalculateAgeGroupDistribution();
		var depressionTestGenderDistribution = await _repoManager.DepressionTestResults.CalculateGenderDistribution();

		var response = new AdminReportResponse()
		{
			TotalAppointmentsCount = totalAppointments,
			TotalPostCount = totalPostsCount,
			TestResultsAgeGroupDistributions = depressionTestAgeGroupDistribution,
			TestResultsCount = depressionTestCount,
			TestResultsGenderDistributions = depressionTestGenderDistribution,
			TotalDoctorCount = totalDoctorsCount,
			TotalUserCount = totalUsersCount
		};

		return response;
	}
}