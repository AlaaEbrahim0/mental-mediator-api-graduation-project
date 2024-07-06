using Application.Dtos;
using Application.Dtos.DepressionTestDtos;
using Shared;

namespace Application.Contracts;
public interface IDepressionTestService
{
	Task<Result<DepressionTestResultResponse>> CreateAndGetDepressionTestResult(DepressionTestRequest request);
	Task<Result<DepressionTestResultResponseWithCount>> GetAllTestResults(DepressionTestsRequestParameters parameters);
}