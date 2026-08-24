using GestionProjetSocota.Data;
using GestionProjetSocota.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionProjetSocota.Services
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationBackgroundService> _logger;

        public NotificationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await VerifierNotifications(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Erreur lors de la vérification des notifications.");
                }

                // Vérification toutes les heures
                await Task.Delay(
                    TimeSpan.FromHours(1),
                    stoppingToken);
            }
        }

        private async Task VerifierNotifications(
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var notificationService = scope.ServiceProvider
                .GetRequiredService<NotificationService>();

            var maintenant = DateTime.Now;

            var projets = await context.Projets
                .Include(p => p.OwnerIt)
                .Where(p =>
                    p.Deadline.HasValue &&
                    p.Statut != StatutProjet.Closed &&
                    p.Statut != StatutProjet.Cancelled)
                .ToListAsync(cancellationToken);

            foreach (var projet in projets)
            {
                if (!projet.Deadline.HasValue)
                    continue;

                if (projet.OwnerIt == null)
                    continue;

                var destinataire = projet.OwnerIt.Email;

                if (string.IsNullOrWhiteSpace(destinataire))
                    continue;

                var deadline = projet.Deadline.Value.Date;
                var aujourdHui = maintenant.Date;

                var joursAvantDeadline =
                    (deadline - aujourdHui).Days;

                // J-7
                if (joursAvantDeadline == 7)
                {
                    await notificationService.EnvoyerNotificationProjet(
                        projet,
                        "DEADLINE_J7",
                        destinataire,
                        $"Deadline dans 7 jours - {projet.Nom}",
                        $"""
                        Bonjour {projet.OwnerIt.Nom},

                        Le projet "{projet.Nom}" arrive à sa deadline
                        dans 7 jours.

                        Deadline : {deadline:dd/MM/yyyy}
                        Statut : {projet.Statut}
                        Avancement : {projet.PourcentageAvancement}%

                        Merci de vérifier l'avancement du projet.

                        Gestion Projet SOCOTA
                        """);
                }

                // J-3
                if (joursAvantDeadline == 3)
                {
                    await notificationService.EnvoyerNotificationProjet(
                        projet,
                        "DEADLINE_J3",
                        destinataire,
                        $"Deadline dans 3 jours - {projet.Nom}",
                        $"""
                        Bonjour {projet.OwnerIt.Nom},

                        Attention : le projet "{projet.Nom}"
                        arrive à sa deadline dans 3 jours.

                        Deadline : {deadline:dd/MM/yyyy}
                        Statut : {projet.Statut}
                        Avancement : {projet.PourcentageAvancement}%

                        Merci de prendre les mesures nécessaires.

                        Gestion Projet SOCOTA
                        """);
                }

                // Deadline dépassée
                if (deadline < aujourdHui)
                {
                    await notificationService.EnvoyerNotificationProjet(
                        projet,
                        "DEADLINE_DEPASSEE",
                        destinataire,
                        $"Deadline dépassée - {projet.Nom}",
                        $"""
                        Bonjour {projet.OwnerIt.Nom},

                        La deadline du projet "{projet.Nom}"
                        est dépassée.

                        Deadline : {deadline:dd/MM/yyyy}
                        Statut : {projet.Statut}
                        Avancement : {projet.PourcentageAvancement}%

                        Merci de mettre à jour la situation du projet.

                        Gestion Projet SOCOTA
                        """);
                }
            }
        }
    }
}