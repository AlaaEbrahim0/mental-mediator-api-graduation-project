using Application.Dtos;
using Shared;

namespace Application.Contracts;

public interface IDepressionDetector
{
	Task<Result<string>> IsDepressed(DepressionTestRequest request);
}

