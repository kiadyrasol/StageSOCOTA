using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionProjetSocota.Data;
using GestionProjetSocota.Models;
using GestionProjetSocota.ViewModels;

namespace GestionProjetSocota.Controllers
{
    public class CommentaireController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CommentaireController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Administrateur,ChefDeProjet,PowerUser")]
        [HttpPost]
        public async Task<IActionResult> Create(CommentaireCreateViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Contenu))
            {
                return RedirectToAction("Details", "Projet", new { id = model.ProjetId });
            }

            var nomAD = User.Identity?.Name;
            var auteur = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.NomADUtilisateur == nomAD);

            var commentaire = new Commentaire
            {
                ProjetId = model.ProjetId,
                AuteurId = auteur?.Id ?? 0,
                Contenu = model.Contenu,
                DatePublication = DateTime.Now
            };

            _context.Commentaires.Add(commentaire);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Projet", new { id = model.ProjetId });
        }

        [Authorize(Roles = "Administrateur")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var commentaire = await _context.Commentaires.FindAsync(id);
            if (commentaire == null)
            {
                return NotFound();
            }

            var projetId = commentaire.ProjetId;
            _context.Commentaires.Remove(commentaire);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Projet", new { id = projetId });
        }
    }
}