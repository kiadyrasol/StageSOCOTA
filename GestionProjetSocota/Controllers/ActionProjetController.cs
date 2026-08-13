using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionProjetSocota.Data;
using GestionProjetSocota.Models;
using GestionProjetSocota.ViewModels;


namespace GestionProjetSocota.Controllers
{
    public class ActionProjetController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActionProjetController(ApplicationDbContext context)
        {
            _context = context;
        }


        // Create
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpGet]
        public async Task<IActionResult> Create(int projetId)
        {
            var model = new ActionCreateViewModel
            {
                ProjetId = projetId,
                UtilisateursDisponibles = await _context.Utilisateurs.ToListAsync()
            };

            return View(model);
        }

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> Create(ActionCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.UtilisateursDisponibles = await _context.Utilisateurs.ToListAsync();
                return View(model);
            }

            var action = new ActionProjet
            {
                ProjetId = model.ProjetId,
                Description = model.Description,
                ResponsableId = model.ResponsableId,
                DateEcheance = model.DateEcheance,
                Statut = StatutAction.Open
            };

            _context.Actions.Add(action);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Projet", new { id = model.ProjetId });
        }


        // Statut
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> ChangerStatut(int id, StatutAction nouveauStatut)
        {
            var action = await _context.Actions.FindAsync(id);
            if (action == null)
            {
                return NotFound();
            }

            action.Statut = nouveauStatut;
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Projet", new { id = action.ProjetId });
        }


        // Delete
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var action = await _context.Actions.FindAsync(id);
            if (action == null)
            {
                return NotFound();
            }

            var projetId = action.ProjetId;
            _context.Actions.Remove(action);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Projet", new { id = projetId });
        }
    }
}