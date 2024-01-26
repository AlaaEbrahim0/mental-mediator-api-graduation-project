namespace Application.Utilities;
public static class NotificationMessageTemplates
{
    public static string EmailConfirmationMessage(string username, string confirmationLink)
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
                margin: 0;
                padding: 0;
            }}
            .container {{
                max-width: 600px;
                margin: 0 auto;
                padding: 20px;
                background-color: #fff;
                border-radius: 5px;
                box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
            }}
            h2 {{
                color: #007bff;
            }}
            p {{
                margin-bottom: 15px;
            }}
            .confirmation-link {{
                display: inline-block;
                padding: 15px 30px;
                font-size: 18px;
                color: white;
                background-color: #3A88DC;
                text-decoration: none;
                border-radius: 5px;
                transition: background-color 0.3s ease;
                cursor: pointer;
            }}
            
        </style>
    </head>
    <body>
        <div class='container'>
            <h2>Email Confirmation</h2>
            <p>Dear {username},</p>
            <p>Thank you for registering! Please click the following button to confirm your email address:</p>
            <p>
                <a href='{confirmationLink}' class='confirmation-link'>CONFIRM EMAIL</a>
            </p>
            <p>If you didn't request this confirmation, you can ignore this email.</p>
            <p>Best regards,<br>Your Application Team</p>
        </div>

    </body>
    </html>";

        return htmlTemplate;
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
