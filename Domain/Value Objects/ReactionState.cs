namespace Domain.Value_Objects;

public sealed record ReactionCount(string Reaction, int Count);

public sealed class ReactionState
{
	public IReadOnlyList<ReactionCount> Counts { get; init; } = Array.Empty<ReactionCount>();

	public string? MyReaction { get; init; }

	/// <summary>
	/// Groups raw (reaction, user) rows into ordered counts and resolves the
	/// caller's own reaction. Keeping this in one place keeps post, comment,
	/// reply and chat responses consistent.
	/// </summary>
	public static ReactionState From(
		IEnumerable<(string Reaction, string UserId)> rows,
		string? userId)
	{
		var materialized = rows.ToList();

		var counts = materialized
			.GroupBy(row => row.Reaction)
			.Select(group => new ReactionCount(group.Key, group.Count()))
			.OrderByDescending(count => count.Count)
			.ThenBy(count => count.Reaction)
			.ToList();

		var myReaction = string.IsNullOrEmpty(userId)
			? null
			: materialized
				.FirstOrDefault(row => string.Equals(row.UserId, userId, StringComparison.Ordinal))
				.Reaction;

		return new ReactionState
		{
			Counts = counts,
			MyReaction = string.IsNullOrEmpty(myReaction) ? null : myReaction
		};
	}
}
