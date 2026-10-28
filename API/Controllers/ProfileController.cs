using Application.Contracts;
using Application.Dtos.ProfileDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Rich profiles: the public view, the owner's editor, and the shared
/// certificate/qualification collections. Patients and doctors use the same
/// endpoints; doctor-specific fields are returned when the account is a doctor.
/// </summary>
[ApiController]
[Route("api/profiles")]
[Authorize]
public class ProfileController : ControllerBase
{
	private readonly IProfileService _profileService;

	public ProfileController(IProfileService profileService)
	{
		_profileService = profileService;
	}

	[HttpGet("{id}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetPublicProfile(string id)
	{
		var result = await _profileService.GetPublicProfile(id);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpGet("me")]
	public async Task<IActionResult> GetMyProfile()
	{
		var result = await _profileService.GetMyProfile();
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpPut("me")]
	public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
	{
		var result = await _profileService.UpdateMyProfile(request);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpPut("me/photo")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> UpdateMyPhoto([FromForm] UpdateProfilePhotoRequest request)
	{
		var result = await _profileService.UpdateMyPhoto(request);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpPost("me/certificates")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> AddCertificate([FromForm] CertificateRequest request)
	{
		var result = await _profileService.AddCertificate(request);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpPut("me/certificates/{certificateId:int}")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> UpdateCertificate(int certificateId, [FromForm] CertificateRequest request)
	{
		var result = await _profileService.UpdateCertificate(certificateId, request);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpDelete("me/certificates/{certificateId:int}")]
	public async Task<IActionResult> DeleteCertificate(int certificateId)
	{
		var result = await _profileService.DeleteCertificate(certificateId);
		return result.IsFailure ? result.ToProblemDetails() : NoContent();
	}

	[HttpPost("me/qualifications")]
	public async Task<IActionResult> AddQualification([FromBody] QualificationRequest request)
	{
		var result = await _profileService.AddQualification(request);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpPut("me/qualifications/{qualificationId:int}")]
	public async Task<IActionResult> UpdateQualification(int qualificationId, [FromBody] QualificationRequest request)
	{
		var result = await _profileService.UpdateQualification(qualificationId, request);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpDelete("me/qualifications/{qualificationId:int}")]
	public async Task<IActionResult> DeleteQualification(int qualificationId)
	{
		var result = await _profileService.DeleteQualification(qualificationId);
		return result.IsFailure ? result.ToProblemDetails() : NoContent();
	}
}
