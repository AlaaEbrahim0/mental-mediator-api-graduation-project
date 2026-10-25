using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

public interface IChatMessageReportRepository
{
	Task<ChatMessageReport?> GetReportByIdAsync(int reportId, bool trackChanges);
	Task<ChatMessageReport?> GetReportAsync(long messageId, string reporterId, bool trackChanges);
	Task<IReadOnlyList<ChatMessageReport>> GetReportsForRoomAsync(int roomId, ChatReportStatus? status, int limit, bool trackChanges);
	void CreateReport(ChatMessageReport report);
	void UpdateReport(ChatMessageReport report);

	/// <summary>
	/// Persists the report unless the reporter already reported the message.
	/// Returns null when the report was created, or the existing report when
	/// the unique index rejected a concurrent duplicate.
	/// </summary>
	Task<ChatMessageReport?> CreateReportIfAbsentAsync(ChatMessageReport report);
}
