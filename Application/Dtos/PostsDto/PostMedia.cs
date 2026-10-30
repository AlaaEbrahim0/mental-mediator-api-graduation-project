using System.Text.Json;

namespace Application.Dtos.PostsDto;

public class PostMediaRequest
{
	public string Url { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty;
}

public class PostMediaResponse
{
	public string Url { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty;
}

/// <summary>
/// The post media column is a JSON array of URLs returned by the upload
/// endpoint. The client never sends raw files with the post itself: media is
/// uploaded one item at a time so the composer can show real progress, and the
/// stored list is re-validated here before it is saved.
/// </summary>
public static class PostMediaCodec
{
	public const int MaxItems = 5;
	public const int MaxImages = 4;
	public const int MaxVideos = 1;
	public const int MaxUrlLength = 1000;
	public const int MaxJsonLength = 4000;

	private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

	public static bool TryParse(string? json, out List<PostMediaRequest> items, out string? error)
	{
		items = new List<PostMediaRequest>();
		error = null;

		if (string.IsNullOrWhiteSpace(json)) return true;

		if (json.Length > MaxJsonLength)
		{
			error = "The media list is too long.";
			return false;
		}

		try
		{
			items = JsonSerializer.Deserialize<List<PostMediaRequest>>(json, Options)
				?? new List<PostMediaRequest>();
		}
		catch (JsonException)
		{
			error = "Media must be a JSON array of { url, type } items.";
			items = new List<PostMediaRequest>();
			return false;
		}

		var images = 0;
		var videos = 0;
		var seen = new HashSet<string>(StringComparer.Ordinal);

		foreach (var item in items)
		{
			var url = NormalizeSecureUrl(item.Url?.Trim() ?? string.Empty);
			var type = item.Type?.Trim().ToLowerInvariant();

			if (string.IsNullOrWhiteSpace(url) ||
				url.Length > MaxUrlLength ||
				!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
				uri.Scheme != Uri.UriSchemeHttps)
			{
				error = "Every media item needs an absolute https URL.";
				return false;
			}

			if (type == "image") images++;
			else if (type == "video") videos++;
			else
			{
				error = "Media type must be 'image' or 'video'.";
				return false;
			}

			if (!seen.Add(url))
			{
				error = "The same media item was added more than once.";
				return false;
			}

			item.Url = url;
			item.Type = type!;
		}

		if (images > MaxImages)
		{
			error = $"A post can include at most {MaxImages} images.";
			return false;
		}
		if (videos > MaxVideos)
		{
			error = $"A post can include at most {MaxVideos} video.";
			return false;
		}

		return true;
	}

	public static string Serialize(IEnumerable<PostMediaRequest> items) =>
		JsonSerializer.Serialize(items, Options);

	/// <summary>
	/// Existing rows can predate the secure-URL setting. Only Cloudinary URLs
	/// are rewritten; unrelated external URLs (legacy data) are left alone.
	/// </summary>
	public static string NormalizeSecureUrl(string url) =>
		url.StartsWith("http://res.cloudinary.com/", StringComparison.OrdinalIgnoreCase)
			? string.Concat("https://", url.AsSpan("http://".Length))
			: url;

	public static List<PostMediaResponse> ToResponse(string? json) =>
		TryParse(json, out var items, out _)
			? items.Select(item => new PostMediaResponse
			{
				Url = NormalizeSecureUrl(item.Url),
				Type = item.Type
			}).ToList()
			: new List<PostMediaResponse>();

	public static List<PostMediaRequest> ToRequestList(string? json) =>
		TryParse(json, out var items, out _) ? items : new List<PostMediaRequest>();
}
