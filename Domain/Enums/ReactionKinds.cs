namespace Domain.Enums;

/// <summary>
/// The fixed set of reactions the API accepts. Tokens are used on the wire
/// instead of raw emoji so storage, validation and JSON stay unambiguous.
/// </summary>
public static class ReactionKinds
{
	public const string Like = "like";
	public const string Love = "love";
	public const string Haha = "haha";
	public const string Wow = "wow";
	public const string Sad = "sad";

	public static readonly IReadOnlyList<string> All = new[]
	{
		Like,
		Love,
		Haha,
		Wow,
		Sad
	};

	public static bool IsValid(string? reaction) =>
		!string.IsNullOrWhiteSpace(reaction) &&
		All.Contains(reaction.Trim().ToLowerInvariant());

	public static string Normalize(string reaction) => reaction.Trim().ToLowerInvariant();
}
