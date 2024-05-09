using Shared;

namespace Application.Contracts;

public interface IHateSpeechDetector
{

	Task<Result<bool>> IsHateSpeech(string content);
}
