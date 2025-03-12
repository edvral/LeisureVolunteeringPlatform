using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Net;
using MailKit.Security;

namespace LeisureVolunteeringPlatform.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendVerificationEmail(string email, string token)
        {
            Console.WriteLine($"[DEBUG] Token Sent in Email: {token}");

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(
                _configuration["EmailSettings:SenderName"],
                _configuration["EmailSettings:SenderEmail"]
            ));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = "Verify Your Email";

            var verificationLink = $"https://localhost:7177/api/auth/verify-email?token={token}";

            emailMessage.Body = new TextPart("html")
            {
                Text = $@"
                <h2>Patvirtinkite savo el. paštą</h2>
                 <p>Spustelėkite žemiau esantį mygtuką, kad patvirtintumėte savo paskyrą:</p>
                <a href='{verificationLink}' style='padding: 10px 20px; background-color: blue; color: white; text-decoration: none; border-radius: 5px;'>Patvirtinti el. paštą</a>
                <p>Jei negalite paspausti nuorodos, nukopijuokite ir įklijuokite šį adresą į naršyklę:</p>
                <p>{verificationLink}</p>
                "
            };

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(
                    _configuration["EmailSettings:SmtpServer"],
                    int.Parse(_configuration["EmailSettings:Port"]),
                    MailKit.Security.SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(
                    _configuration["EmailSettings:SenderEmail"],
                    _configuration["EmailSettings:Password"]
                );

                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task SendPasswordResetEmail(string userEmail, string resetLink)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("LeisureVolunteeringPlatform", "leisurevolunteeringplatform@gmail.com"));
            message.To.Add(new MailboxAddress("", userEmail));
            message.Subject = "Slaptažodžio Atkūrimas";

            message.Body = new TextPart("plain")
            {
                Text = $"Sveiki,\n\nNorėdami atkurti slaptažodį, spauskite šią nuorodą: {resetLink}\n\nJei to neprašėte, galite ignoruoti šį laišką."
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync("leisurevolunteeringplatform@gmail.com", "etqw bfxv zbbk mtvf");
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

    }
}
