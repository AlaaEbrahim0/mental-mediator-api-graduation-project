namespace Domain.Enums;

/// <summary>
/// Directory filter for the rooms page. Query vocabulary, not persisted state.
/// </summary>
public enum RoomBrowseTab
{
	All,

	/// <summary>Something has been said in the last 24 hours.</summary>
	Active,

	Private,

	/// <summary>A scheduled start that has not happened yet.</summary>
	Scheduled,

	/// <summary>The owner membership belongs to a patient rather than a doctor.</summary>
	PatientCreated
}
