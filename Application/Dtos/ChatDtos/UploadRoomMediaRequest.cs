using Microsoft.AspNetCore.Http;

namespace Application.Dtos.ChatDtos;

/// <summary>Multipart body for a room media upload; the field name is "file".</summary>
public class UploadRoomMediaRequest
{
	public IFormFile? File { get; set; }
}
