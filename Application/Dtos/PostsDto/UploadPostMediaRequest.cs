using Microsoft.AspNetCore.Http;

namespace Application.Dtos.PostsDto;

public class UploadPostMediaRequest
{
	public IFormFile? File { get; set; }
}
