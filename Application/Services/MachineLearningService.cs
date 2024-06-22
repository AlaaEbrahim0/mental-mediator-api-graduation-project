using Application.Contracts;
using Application.Dtos;
using Shared;

namespace Application.Services;
public class MachineLearningService
{
	private readonly IDepressionDetector _depressionDetector;

	public MachineLearningService(IDepressionDetector depressionDetector)
	{
		_depressionDetector = depressionDetector;
	}

	public async Task<Result<bool>> IsDepressed(DepressionTestRequest request)
	{
		return await _depressionDetector.IsDepressed(request);
	}
}
