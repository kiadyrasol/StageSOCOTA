
using GestionProjetSocota.Data;
using GestionProjetSocota.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionProjetSocota.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task CreerNotificationAsync(
            int projetId,
            int utilisateurId,
            string type,
            string message)
        {
            var notification = new Notification
            {
                ProjetId = projetId,
                UtilisateurId = utilisateurId,
                Type = type,
                Message = message,
                DateCreation = DateTime.Now,
                EstLue = false
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
        }


        public async Task<List<Notification>> GetNotificationsUtilisateurAsync(
            int utilisateurId)
        {
            return await _context.Notifications
                .Include(n => n.Projet)
                .Where(n => n.UtilisateurId == utilisateurId)
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();
        }

        public async Task<int> GetNombreNonLuesAsync(
            int utilisateurId)
        {
            return await _context.Notifications
                .CountAsync(n =>
                    n.UtilisateurId == utilisateurId &&
                    !n.EstLue);
        }

        public async Task MarquerCommeLueAsync(int id, int utilisateurId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.Id == id &&
                    n.UtilisateurId == utilisateurId);

            if (notification == null)
                return;

            notification.EstLue = true;

            await _context.SaveChangesAsync();
        }

        public async Task MarquerToutesCommeLuesAsync(
            int utilisateurId)
        {
            var notifications = await _context.Notifications
                .Where(n =>
                    n.UtilisateurId == utilisateurId &&
                    !n.EstLue)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.EstLue = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}

