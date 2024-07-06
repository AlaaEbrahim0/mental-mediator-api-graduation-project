using Domain.Enums;

namespace Domain.Entities;
public class DepressionTestResult
{
	public int Id { get; set; }
	public string? UserId { get; set; }
	public User? User { get; set; }
	public string Result { get; set; } = string.Empty;
	public DateTime Date { get; set; }
	public Gender Gender { get; set; }
	public int Age { get; set; }
}
