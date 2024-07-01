using Application.Dtos.NewsDtos;
using Shared;

namespace Application.Contracts;
public interface INewsService
{
	Task<Result<NewsResponse?>> GetNews(NewsRequestParameters parameters);
}

