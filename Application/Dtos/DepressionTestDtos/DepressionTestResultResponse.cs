using Domain.Enums;

namespace Application.Dtos.DepressionTestDtos;

public class DepressionTestResultResponseWithCount
{
	public List<DepressionTestResultResponse> Results { get; set; } = new();
	public int TotalCount { get; set; }
}

public class DepressionTestResultResponse
{
	public int Id { get; set; }
	public string? UserId { get; set; }
	public string Result { get; set; } = string.Empty;
	public DateTime Date { get; set; }
	public int Age { get; set; }
	public Gender Gender { get; set; }
}
