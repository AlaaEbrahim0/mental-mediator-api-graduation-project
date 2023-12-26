using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared;
public static class EmailConfirmationMessageTemplate
{
    public static string GenerateConfirmationEmail(string username, string confirmationLink)
    {
        string htmlTemplate = @$"
        <html>
        <head>
            <style>
                /* Add your styles here */
                body {{
                    font-family: Arial, sans-serif;
                    background-color: #f4f4f4;
                    color: #333;
                }}
                .container {{
                    max-width: 600px;
                    margin: 0 auto;
                    padding: 20px;
                    background-color: #fff;
                    border-radius: 5px;
                    box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
                }}
                .confirmation-link {{
                    color: #007bff;
                    text-decoration: none;
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <h2>Email Confirmation</h2>
                <p>Dear {username},</p>
                <p>Thank you for registering! Please click the following link to confirm your email address:</p>
                <p><a href='{confirmationLink}' class='confirmation-link'>{confirmationLink}</a></p>
                <p>If you didn't request this confirmation, you can ignore this email.</p>
                <p>Best regards,<br>Your Application Team</p>
            </div>
        </body>
        </html>";

        return htmlTemplate;

    }
}
