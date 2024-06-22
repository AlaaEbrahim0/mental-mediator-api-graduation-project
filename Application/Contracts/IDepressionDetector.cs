using Application.Dtos;
using Shared;

namespace Application.Contracts;

public interface IDepressionDetector
{
	Task<Result<bool>> IsDepressed(DepressionTestRequest request);
}

