using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionProjetSocota.Data;
using GestionProjetSocota.Models;
using GestionProjetSocota.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace GestionProjetSocota.Controllers
{
    public class ProjetController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjetController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var projets = await _context.Projets
                .Include(p => p.OwnerIt)
                .Include(p => p.PowerUser)
                .ToListAsync();

            return View(projets);
        }

    [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ProjetCreateViewModel
            {
                UtilisateursDisponibles = await _context.Utilisateurs.ToListAsync()
            };

            return View(model);
        }

    [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> Create(ProjetCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.UtilisateursDisponibles = await _context.Utilisateurs.ToListAsync();
                return View(model);
            }

            var projet = new Projet
            {
                TicketId = model.TicketId,
                Reference = model.Reference,
                Nom = model.Nom,
               Description = model.Description ?? string.Empty,
                Unite = model.Unite,
                Departement = model.Departement,
                Type = model.Type,
                Plateforme = model.Plateforme,
                Priorite = model.Priorite,
                Deadline = model.Deadline,
                OwnerItId = model.OwnerItId,
                PowerUserId = model.PowerUserId,
                Statut = StatutProjet.WaitingRFC
            };

            _context.Projets.Add(projet);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }


    [Authorize(Roles = "Administrateur,ChefDeProjet")]
    [HttpGet]
public async Task<IActionResult> Edit(int id)
{
    var projet = await _context.Projets.FindAsync(id);
    if (projet == null)
    {
        return NotFound();
    }

    var model = new ProjetEditViewModel
    {
        Id = projet.Id,
        TicketId = projet.TicketId,
        Reference = projet.Reference,
        Nom = projet.Nom,
        Description = projet.Description,
        Unite = projet.Unite,
        Departement = projet.Departement,
        Type = projet.Type,
        Plateforme = projet.Plateforme,
        Statut = projet.Statut,
        Priorite = projet.Priorite,
        Deadline = projet.Deadline,
        PourcentageAvancement = projet.PourcentageAvancement,
        OwnerItId = projet.OwnerItId,
        PowerUserId = projet.PowerUserId,
        UtilisateursDisponibles = await _context.Utilisateurs.ToListAsync()
    };

    return View(model);
}


[Authorize(Roles = "Administrateur,ChefDeProjet")]
[HttpPost]
public async Task<IActionResult> Edit(ProjetEditViewModel model)
{
    if (!ModelState.IsValid)
    {
        model.UtilisateursDisponibles = await _context.Utilisateurs.ToListAsync();
        return View(model);
    }

    var projet = await _context.Projets.FindAsync(model.Id);
    if (projet == null)
    {
        return NotFound();
    }

    projet.TicketId = model.TicketId;
    projet.Reference = model.Reference;
    projet.Nom = model.Nom;
  projet.Description = model.Description ?? string.Empty;
    projet.Unite = model.Unite;
    projet.Departement = model.Departement;
    projet.Type = model.Type;
    projet.Plateforme = model.Plateforme;
    projet.StatutPrecedent = projet.Statut;
    projet.Statut = model.Statut;
    projet.Priorite = model.Priorite;
    projet.Deadline = model.Deadline;
    projet.PourcentageAvancement = model.PourcentageAvancement;
    projet.OwnerItId = model.OwnerItId;
    projet.PowerUserId = model.PowerUserId;

    await _context.SaveChangesAsync();

    return RedirectToAction("Index");
}


[Authorize(Roles = "Administrateur")]
[HttpGet]
public async Task<IActionResult> Delete(int id)
{
    var projet = await _context.Projets
        .Include(p => p.OwnerIt)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (projet == null)
    {
        return NotFound();
    }

    return View(projet);
}

[Authorize(Roles = "Administrateur")]
[HttpPost, ActionName("Delete")]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var projet = await _context.Projets.FindAsync(id);
    if (projet != null)
    {
        _context.Projets.Remove(projet);
        await _context.SaveChangesAsync();
    }

    return RedirectToAction("Index");
}
    }
}