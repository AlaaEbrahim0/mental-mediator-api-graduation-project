using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Utilities;
public static class NotificationMessageTemplates
{
    public static string EmailConfirmationMessage(string userName, string confirmationLink)
    {
        return $@"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <style>
                    body {{
                        font-family: 'Arial', sans-serif;
                        background-color: #f4f4f4;
                        color: #333;
                        line-height: 1.6;
                        margin: 0;
                        padding: 0;
                    }}
                    .container {{
                        max-width: 600px;
                        margin: 20px auto;
                        background-color: #fff;
                        padding: 20px;
                        border-radius: 5px;
                        box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
                    }}
                    h1 {{
                        color: #007bff;
                    }}
                    p {{
                        margin-bottom: 20px;
                    }}
                    a {{
                        color: #007bff;
                        text-decoration: none;
                    }}
                    a:hover {{
                        text-decoration: underline;
                    }}
                </style>
            </head>
            <body>
                <div class=""container"">
                    <h1>Hi {userName},</h1>
                    <p>Thank you for registering with Mental Mediator! Please confirm your email by clicking the link below:</p>
                    <p><a href=""{confirmationLink}"">{confirmationLink}</a></p>
                    <p>If you didn't register, you can safely ignore this email.</p>
                    <p>Best regards,<br>The Mental Mediator Team</p>
                </div>
            </body>
            </html>
        ";
    }
    public static string WelcomeMessage(string userName)
    {
        return $@"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <style>
                    body {{
                        font-family: 'Arial', sans-serif;
                        background-color: #f4f4f4;
                        color: #333;
                        line-height: 1.6;
                        margin: 0;
                        padding: 0;
                    }}
                    .container {{
                        max-width: 600px;
                        margin: 20px auto;
                        background-color: #fff;
                        padding: 20px;
                        border-radius: 5px;
                        box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
                    }}
                    h1 {{
                        color: #007bff;
                    }}
                    p {{
                        margin-bottom: 20px;
                    }}
                    a {{
                        color: #007bff;
                        text-decoration: none;
                    }}
                    a:hover {{
                        text-decoration: underline;
                    }}
                </style>
            </head>
            <body>
                <div class=""container"">
                    <h1>Welcome, {userName}, to Mental Mediator!</h1>
                    <p>Thank you for joining us. We're excited to have you as a part of our Mental Mediator community.</p>
                    <p>Feel free to explore and let us know if you have any questions.</p>
                    <p>Best regards,<br>The Mental Mediator Team</p>
                </div>
            </body>
            </html>
        ";
    }
}
