namespace Application.Community;

/// <summary>
/// The vocabulary a post may use. Both portals offer exactly these values, and
/// the API rejects anything else so the feed cannot drift into free-text
/// categories again (the seed data still contains legacy values and is left
/// untouched).
/// </summary>
public static class PostTaxonomy
{
	public const int MaxSelectedTags = 5;
	public const int MaxTagLength = 40;

	public static readonly IReadOnlyList<string> Categories = new[]
	{
		"Clinical Case",
		"General Discussion",
		"Medical News",
		"Patient Care",
		"Platform Feedback"
	};

	public static readonly IReadOnlyList<string> Tags = new[]
	{
		"Anxiety",
		"Depression",
		"Stress",
		"Sleep",
		"Relationships",
		"Family",
		"Therapy",
		"Medication",
		"Coping",
		"Mindfulness",
		"Self-Care",
		"Trauma",
		"Grief",
		"ADHD",
		"Autism",
		"Bipolar Disorder",
		"OCD",
		"PTSD",
		"Neurology",
		"Nutrition",
		"Exercise",
		"Students",
		"Workplace",
		"Research",
		"Support",
		"Community"
	};

	public static bool IsCategory(string? value) =>
		!string.IsNullOrWhiteSpace(value) &&
		Categories.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);

	public static string NormalizeCategory(string? value) =>
		Categories.FirstOrDefault(category =>
			string.Equals(category, value?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

	public static string[] SplitTags(string? tags) =>
		string.IsNullOrWhiteSpace(tags)
			? Array.Empty<string>()
			: tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	/// <summary>
	/// Maps the submitted tags onto the standard vocabulary, removes duplicates
	/// and enforces the selection cap. Returns the canonical CSV that is stored.
	/// </summary>
	public static bool TryNormalizeTags(IEnumerable<string>? tags, out string csv, out string? error)
	{
		csv = string.Empty;
		error = null;
		if (tags is null) return true;

		var canonical = Tags.ToDictionary(tag => tag, StringComparer.OrdinalIgnoreCase);
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var selected = new List<string>();

		foreach (var raw in tags)
		{
			var tag = raw?.Trim();
			if (string.IsNullOrEmpty(tag)) continue;

			if (tag.Length > MaxTagLength)
			{
				error = $"Tags must be at most {MaxTagLength} characters.";
				return false;
			}

			if (!canonical.TryGetValue(tag, out var standard))
			{
				error = $"'{tag}' is not one of the standard tags.";
				return false;
			}

			if (!seen.Add(standard)) continue;

			if (selected.Count == MaxSelectedTags)
			{
				error = $"Choose at most {MaxSelectedTags} tags.";
				return false;
			}

			selected.Add(standard);
		}

		csv = string.Join(',', selected);
		return true;
	}

	public static string NormalizeTagsCsv(string? tags) =>
		TryNormalizeTags(SplitTags(tags), out var csv, out _) ? csv : string.Empty;
}
