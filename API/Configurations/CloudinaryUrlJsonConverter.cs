using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Dtos.PostsDto;

namespace API.Configurations;

/// <summary>
/// Legacy rows can contain plain-HTTP Cloudinary URLs. Rewriting only that
/// host at serialization time fixes every endpoint (posts, profiles, SignalR)
/// without a destructive data migration or touching unrelated external URLs.
/// </summary>
public sealed class CloudinaryUrlJsonConverter : JsonConverter<string>
{
	public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		reader.GetString();

	public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
		writer.WriteStringValue(PostMediaCodec.NormalizeSecureUrl(value));
}
