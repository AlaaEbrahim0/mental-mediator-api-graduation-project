using Domain.Enums;

namespace Domain.Entities;
public class HateSpeechReport
{
	public int Id { get; set; }
	public ReportType ReportType { get; set; }
	public int PostId { get; set; }
	public int CommentId { get; set; }
	public int ReplyId { get; set; }
}