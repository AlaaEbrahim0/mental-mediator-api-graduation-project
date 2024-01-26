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
                color: #fff;
                background-color: #007bff;
                text-decoration: none;
                border-radius: 5px;
                transition: background-color 0.3s ease;
                cursor: pointer;
            }}
            .confirmation-link:hover {{
                background-color: #0056b3;
            }}
        </style>
    </head>
    <body>
        <div class='container'>
            <h2>Email Confirmation</h2>
            <p>Dear {username},</p>
            <p>Thank you for registering! Please click the following button to confirm your email address:</p>
            <p>
                <a href='{confirmationLink}' class='confirmation-link' onclick='animateButton()'>CONFIRM EMAIL</a>
            </p>
            <p>If you didn't request this confirmation, you can ignore this email.</p>
            <p>Best regards,<br>Your Application Team</p>
        </div>

        <script>
            function animateButton() {{
                var button = document.querySelector('.confirmation-link');
                button.style.backgroundColor = '#0056b3';
                setTimeout(function() {{
                    button.style.backgroundColor = '#007bff';
                }}, 300);
            }}
        </script>
    </body>
    </html>";

        return htmlTemplate;
    }

}
