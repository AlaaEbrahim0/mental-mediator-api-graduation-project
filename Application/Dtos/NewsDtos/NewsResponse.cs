namespace Application.Dtos.NewsDtos;

public class NewsResponse
{
	public string Status { get; set; } = string.Empty;
	public int TotalResults { get; set; }
	public List<ArticleResponse> Articles { get; set; } = [];
}