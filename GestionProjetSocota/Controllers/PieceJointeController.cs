using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionProjetSocota.Data;
using GestionProjetSocota.Models;
using GestionProjetSocota.ViewModels;

namespace GestionProjetSocota.Controllers
{
    public class PieceJointeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PieceJointeController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> Create(PieceJointeCreateViewModel model)
        {
            if (model.Fichier == null || model.Fichier.Length == 0)
            {
                TempData["Erreur"] = "Aucun fichier sélectionné.";
                return RedirectToAction("Details", "Projet", new { id = model.ProjetId });
            }

            const long tailleMaxOctets = 20 * 1024 * 1024; // 20 Mo
            if (model.Fichier.Length > tailleMaxOctets)
            {
                TempData["Erreur"] = "Le fichier dépasse la taille maximale autorisée (20 Mo).";
                return RedirectToAction("Details", "Projet", new { id = model.ProjetId });
            }

            var dossierUploads = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(dossierUploads))
            {
                Directory.CreateDirectory(dossierUploads);
            }

            var nomFichierUnique = $"{Guid.NewGuid()}_{model.Fichier.FileName}";
            var cheminComplet = Path.Combine(dossierUploads, nomFichierUnique);

            using (var stream = new FileStream(cheminComplet, FileMode.Create))
            {
                await model.Fichier.CopyToAsync(stream);
            }

            var pieceJointe = new PieceJointe
            {
                ProjetId = model.ProjetId,
                NomFichier = model.Fichier.FileName,
                CheminStockage = $"/uploads/{nomFichierUnique}",
                Type = model.Type,
                DateAjout = DateTime.Now
            };

            _context.PiecesJointes.Add(pieceJointe);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Projet", new { id = model.ProjetId });
        }

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var pieceJointe = await _context.PiecesJointes.FindAsync(id);
            if (pieceJointe == null)
            {
                return NotFound();
            }

            var cheminComplet = Path.Combine(_environment.WebRootPath, pieceJointe.CheminStockage.TrimStart('/'));
            if (System.IO.File.Exists(cheminComplet))
            {
                System.IO.File.Delete(cheminComplet);
            }

            var projetId = pieceJointe.ProjetId;
            _context.PiecesJointes.Remove(pieceJointe);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Projet", new { id = projetId });
        }
    }
}