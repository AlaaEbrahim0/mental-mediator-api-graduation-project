using Application.Dtos.DepressionTestDtos;
using Shared;
using Shared.RequestParameters;

namespace Application.Contracts;
public interface IDepressionTestService
{
	Task<Result<DepressionTestResultResponse>> CreateAndGetDepressionTestResult(DepressionTestRequest request);
	Task<Result<DepressionTestResultResponseWithCount>> GetAllTestResults(DepressionTestsRequestParameters parameters);
}