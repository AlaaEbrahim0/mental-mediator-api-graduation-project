namespace Application.Contracts;

/// <summary>
/// Resolves the display names the doctor workspace shows next to notes,
/// follow-ups and activity. A batched projection instead of a navigation,
/// because <c>BaseUser</c> carries a soft-delete query filter that would make a
/// note vanish from a join when its patient's account is deleted.
/// </summary>
public interface IPatientDirectoryLookup
{
	Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(
		IReadOnlyCollection<string> userIds);

	Task<bool> ExistsAsync(string userId);
}
