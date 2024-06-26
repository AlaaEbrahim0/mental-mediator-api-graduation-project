﻿using Application.Dtos.NotificationDtos;
using Shared;

namespace Application.Services;
public interface IMailService
{
    Task SendEmailAsync(MailRequest mailRequest);
}
