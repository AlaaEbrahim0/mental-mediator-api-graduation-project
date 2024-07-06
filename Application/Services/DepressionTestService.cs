using Application.Contracts;
using Application.Dtos;
using Application.Dtos.DepressionTestDtos;
using AutoMapper;
using Domain.Entities;
using Shared;

namespace Application.Services;
public class DepressionTestService : IDepressionTestService
{
	private readonly MachineLearningService _machineLearningService;
	private readonly IMapper _mapper;
	private readonly IRepositoryManager _repositoryManager;
	private readonly IUserClaimsService _userClaimsService;

	public DepressionTestService(MachineLearningService machineLearningService, IRepositoryManager repositoryManager, IMapper mapper, IUserClaimsService userClaimsService)
	{
		_machineLearningService = machineLearningService;
		_repositoryManager = repositoryManager;
		_mapper = mapper;
		_userClaimsService = userClaimsService;
	}

	public async Task<Result<DepressionTestResultResponse>> CreateAndGetDepressionTestResult(DepressionTestRequest request)
	{
		var userId = _userClaimsService.GetUserId();
		var result = await _machineLearningService.IsDepressed(request);
		if (result.IsFailure)
		{
			return result.Error;
		}

		var depressionTestResult = new DepressionTestResult
		{
			Result = result.Value,
			Date = DateTime.UtcNow,
			Gender = request.Gender,
			Age = request.Age,
			UserId = string.IsNullOrEmpty(userId) ? null : userId
		};

		_repositoryManager.DepressionTestResults.CreateDepressionTest(depressionTestResult);
		await _repositoryManager.SaveAsync();

		var response = _mapper.Map<DepressionTestResultResponse>(depressionTestResult);
		return response;
	}

	public async Task<Result<DepressionTestResultResponseWithCount>> GetAllTestResults(
		DepressionTestsRequestParameters parameters)
	{
		var results = await _repositoryManager.DepressionTestResults.GetAll(parameters, false);
		var mappedResults = _mapper.Map<List<DepressionTestResultResponse>>(results);
		var resultsCount = await _repositoryManager.DepressionTestResults.GetCount(false);
		var response = new DepressionTestResultResponseWithCount
		{
			TotalCount = resultsCount,
			Results = mappedResults
		};
		return response;
	}


}