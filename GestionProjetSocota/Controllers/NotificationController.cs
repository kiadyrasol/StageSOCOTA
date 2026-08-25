using GestionProjetSocota.Data;
using GestionProjetSocota.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionProjetSocota.Controllers
{
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<Utilisateur?> GetCurrentUserAsync()
        {
            var nomAD = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(nomAD)) return null;

            return await _context.Utilisateurs
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.NomADUtilisateur == nomAD);
        }

        public async Task<IActionResult> Index()
        {
            var utilisateur = await GetCurrentUserAsync();
            if (utilisateur == null) return Unauthorized();

            var notifications = await _context.Notifications
                .Include(n => n.Projet)
                .Where(n => n.UtilisateurId == utilisateur.Id)
                .OrderByDescending(n => n.DateCreation)
                .AsNoTracking()
                .ToListAsync();

            return View(notifications ?? new List<Notification>());
        }

        [HttpGet]
        public async Task<IActionResult> NombreNonLues()
        {                                                                                                         
            var utilisateur = await GetCurrentUserAsync();
            if (utilisateur == null) return Json(0);
                                           
            var nombre = await _context.Notifications
                .CountAsync(n => n.UtilisateurId == utilisateur.Id && !n.EstLue);

            return Json(nombre);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarquerCommeLue(int id)
        {
            var utilisateur = await GetCurrentUserAsync();
            if (utilisateur == null) return Unauthorized();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UtilisateurId == utilisateur.Id);

            if (notification == null) return NotFound();

            notification.EstLue = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Supprimer(int id)
        {
            var utilisateur = await GetCurrentUserAsync();
            if (utilisateur == null) return Unauthorized();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UtilisateurId == utilisateur.Id);

            if (notification == null) return NotFound();

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToutMarquerCommeLu()
        {
            var utilisateur = await GetCurrentUserAsync();
            if (utilisateur == null) return Unauthorized();

            var notificationsNonLues = await _context.Notifications
                .Where(n => n.UtilisateurId == utilisateur.Id && !n.EstLue)
                .ToListAsync();

            if (notificationsNonLues.Any())
            {
                foreach (var notif in notificationsNonLues)
                {
                    notif.EstLue = true;
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
