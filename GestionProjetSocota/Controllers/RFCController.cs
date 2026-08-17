using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionProjetSocota.Data;
using GestionProjetSocota.Models;
using GestionProjetSocota.ViewModels;


namespace GestionProjetSocota.Controllers
{
    public class RFCController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RFCController(ApplicationDbContext context)
        {
            _context = context;
        }


        // Create
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpGet]
        public async Task<IActionResult> Create(int projetId)
        {
            var projet = await _context.Projets.FindAsync(projetId);
            if (projet == null)
            {
                return NotFound();
            }

            var model = new RFCCreateViewModel
            {
                ProjetId = projetId,
                UtilisateursDisponibles = await _context.Utilisateurs.ToListAsync()
            };

            ViewBag.ProjetNom = projet.Nom;
            TempData["Succes"] = "Le RFC a été créé avec succès.";

            return View(model);
        }

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> Create(RFCCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.UtilisateursDisponibles = await _context.Utilisateurs.ToListAsync();
                return View(model);
            }

            var rfc = new RFC
            {
                ProjetId = model.ProjetId,
                BusinessCase = model.BusinessCase,
                RoiEstime = model.RoiEstime,
                GainsAttendus = model.GainsAttendus ?? string.Empty,
                ChampionId = model.ChampionId,
                SponsorId = model.SponsorId,
                Priorite = model.Priorite ?? string.Empty,
                EstValide = false
            };

            _context.RFCs.Add(rfc);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Projet", new { id = model.ProjetId });
        }


        // Valider
        [Authorize(Roles = "Administrateur,ChefDeProjet,PowerUser")]
        [HttpPost]
        public async Task<IActionResult> Valider(int id)
        {
            var rfc = await _context.RFCs.FindAsync(id);
            if (rfc == null)
            {
                return NotFound();
            }

            rfc.EstValide = true;

            var projet = await _context.Projets.FindAsync(rfc.ProjetId);
            if (projet != null && projet.Statut == StatutProjet.WaitingRFC)
            {
                projet.StatutPrecedent = projet.Statut;
                projet.Statut = StatutProjet.RFCApproved;
            }

            await _context.SaveChangesAsync();
            TempData["Succes"] = "Le RFC a été validé avec succès.";

            return RedirectToAction("Details", "Projet", new { id = rfc.ProjetId });
        }


        // Annuler
        [Authorize(Roles = "Administrateur,ChefDeProjet,PowerUser")]
        [HttpPost]
        public async Task<IActionResult> AnnulerValidation(int id)
        {
            var rfc = await _context.RFCs.FindAsync(id);
            if (rfc == null)
            {
                return NotFound();
            }

            var projet = await _context.Projets.FindAsync(rfc.ProjetId);

            if (projet != null && projet.Statut == StatutProjet.RFCApproved)
            {
                rfc.EstValide = false;
                projet.Statut = StatutProjet.WaitingRFC;

                await _context.SaveChangesAsync();
            }
            else
            {
                TempData["Erreur"] = "Impossible d'annuler : le projet a déjà avancé au-delà de l'approbation du RFC.";
            }

            return RedirectToAction("Details", "Projet", new { id = rfc.ProjetId });
        }
    }
}