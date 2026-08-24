using GestionProjetSocota.Data;
using GestionProjetSocota.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace GestionProjetSocota.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailSettings _emailSettings;

        public NotificationService(
            ApplicationDbContext context,
            EmailSettings emailSettings)
        {
            _context = context;
            _emailSettings = emailSettings;
        }

        public async Task EnvoyerNotificationProjet(
            Projet projet,
            string type,
            string destinataire,
            string sujet,
            string message)
        {
            if (string.IsNullOrWhiteSpace(destinataire))
                return;

            // Vérifier si cette notification a déjà été envoyée
            var dejaEnvoyee = await _context.Notifications
                .AnyAsync(n =>
                    n.ProjetId == projet.Id &&
                    n.Type == type &&
                    n.Destinataire == destinataire);

            if (dejaEnvoyee)
                return;

            var email = new MimeMessage();

            email.From.Add(
                new MailboxAddress(
                    _emailSettings.FromName,
                    _emailSettings.From));

            email.To.Add(
                MailboxAddress.Parse(destinataire));

            email.Subject = sujet;

            email.Body = new TextPart("plain")
            {
                Text = message
            };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                _emailSettings.Host,
                _emailSettings.Port,
                _emailSettings.EnableSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None);

            await smtp.AuthenticateAsync(
                _emailSettings.UserName,
                _emailSettings.Password);

            await smtp.SendAsync(email);

            await smtp.DisconnectAsync(true);

            // Historiser l'envoi
            _context.Notifications.Add(new Notification
            {
                ProjetId = projet.Id,
                Type = type,
                Destinataire = destinataire,
                DateEnvoi = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }
    }
}