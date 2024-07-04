namespace Domain.Entities;
public class DepressionTestResult
{
	public int Id { get; set; }
	public string? AppUserId { get; set; }
	public bool IsDepressed { get; set; }
	public DateTime Date { get; set; }
}
