using Domain.Enums;

namespace Application.Dtos.ChatDtos;

public class ChatRoomRequestParameters
{
	private int _limit = 20;

	public int? BeforeRoomId { get; set; }

	public int Limit
	{
		get => _limit;
		set => _limit = Math.Clamp(value, 1, 50);
	}
}

public class ChatMessageRequestParameters
{
	private int _limit = 50;

	public long? BeforeId { get; set; }

	public long? AfterId { get; set; }

	public int Limit
	{
		get => _limit;
		set => _limit = Math.Clamp(value, 1, 100);
	}
}

public class ChatReportRequestParameters
{
	private int _limit = 50;

	public ChatReportStatus? Status { get; set; }

	public int Limit
	{
		get => _limit;
		set => _limit = Math.Clamp(value, 1, 100);
	}
}
