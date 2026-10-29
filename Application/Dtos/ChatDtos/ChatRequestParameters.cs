using Domain.Enums;
using Shared.RequestParameters;

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

/// <summary>
/// Directory query for the redesigned rooms page. Offset paging rather than the
/// cursor the append-only list uses, because a sorted and filtered list has no
/// stable key to page from.
/// </summary>
public class RoomBrowseParameters : RequestParameters
{
	/// <summary>Matches room names, and descriptions of public rooms only.</summary>
	public string? Search { get; set; }

	public RoomBrowseTab Tab { get; set; } = RoomBrowseTab.All;

	public RoomBrowseSort Sort { get; set; } = RoomBrowseSort.Recent;
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
