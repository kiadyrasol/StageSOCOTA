using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionProjetSocota.Data;
using Microsoft.AspNetCore.Authorization;

namespace GestionProjetSocota.Controllers
{
    [Authorize(Roles = "Administrateur,ChefDeProjet")]
    public class UtilisateurController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UtilisateurController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Index
        public async Task<IActionResult> Index(string? recherche)
        {
            var query = _context.Utilisateurs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(recherche))
            {
                var termeRecherche = recherche.Trim().ToLower();

                query = query.Where(u =>
                    u.Nom.ToLower().Contains(termeRecherche));
            }

            var utilisateurs = await query
                .OrderBy(u => u.Nom)
                .ToListAsync();

            ViewBag.RechercheActuelle = recherche;

            return View(utilisateurs);
        }

        // Activer / Désactiver
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActif(int id)
        {
            var utilisateur = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.Id == id);

            if (utilisateur == null)
            {
                return NotFound();
            }

            utilisateur.EstActif = !utilisateur.EstActif;

            await _context.SaveChangesAsync();

            TempData["Succes"] =
                utilisateur.EstActif
                    ? $"\"{utilisateur.Nom}\" a été réactivé."
                    : $"\"{utilisateur.Nom}\" a été désactivé.";

            return RedirectToAction("Index");
        }

    }
    
}