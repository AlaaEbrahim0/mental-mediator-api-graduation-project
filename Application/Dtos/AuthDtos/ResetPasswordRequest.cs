﻿namespace Application.Dtos.AuthDtos;
public class ResetPasswordRequest
{
	public string Email { get; set; } = string.Empty;
	public string Token { get; set; } = string.Empty;
	public string NewPassword { get; set; } = string.Empty;
}