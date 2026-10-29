namespace Domain.Enums;

public enum RoomBrowseSort
{
	Recent,

	/// <summary>Rooms whose scheduled start is closest, unscheduled rooms last.</summary>
	Soonest,

	Members,

	Name
}
