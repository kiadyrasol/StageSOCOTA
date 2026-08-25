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
        private const string DossierStockage =
             @"\\fileserver\IT\Digitalisation";

        public PieceJointeController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        //Create
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> Create(PieceJointeCreateViewModel model)
        {
            if (model.Fichier == null || model.Fichier.Length == 0)
            {
                TempData["Erreur"] = "Aucun fichier sélectionné.";

                return RedirectToAction(
                    "Details",
                    "Projet",
                    new { id = model.ProjetId });
            }

            const long tailleMaxOctets = 20 * 1024 * 1024;

            if (model.Fichier.Length > tailleMaxOctets)
            {
                TempData["Erreur"] =
                    "Le fichier dépasse la taille maximale autorisée (20 Mo).";

                return RedirectToAction(
                    "Details",
                    "Projet",
                    new { id = model.ProjetId });
            }

            var projetExiste = await _context.Projets
                .AnyAsync(p => p.Id == model.ProjetId);

            if (!projetExiste)
            {
                return NotFound("Projet introuvable.");
            }

            var dossierProjet = Path.Combine(
                DossierStockage,
                $"Projet_{model.ProjetId}");


            var dossierType = Path.Combine(
                dossierProjet,
                model.Type.ToString());

            if (!Directory.Exists(dossierType))
            {
                Directory.CreateDirectory(dossierType);
            }

            var extension = Path.GetExtension(
                model.Fichier.FileName);

            var nomFichierUnique =
                $"{Guid.NewGuid()}{extension}";

            var cheminComplet = Path.Combine(
                dossierType,
                nomFichierUnique);


            using (var stream = new FileStream(
                cheminComplet,
                FileMode.Create))
            {
                await model.Fichier.CopyToAsync(stream);
            }

            var pieceJointe = new PieceJointe
            {
                ProjetId = model.ProjetId,

                NomFichier = model.Fichier.FileName,

                CheminStockage = cheminComplet,

                Type = model.Type,

                DateAjout = DateTime.Now
            };

            _context.PiecesJointes.Add(pieceJointe);

            await _context.SaveChangesAsync();

            TempData["Succes"] =
                "Fichier importé avec succès.";

            return RedirectToAction(
                "Details",
                "Projet",
                new { id = model.ProjetId });
        }


        //Delete
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var pieceJointe = await _context.PiecesJointes
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pieceJointe == null)
            {
                return NotFound();
            }

            var cheminComplet = pieceJointe.CheminStockage;

            if (!string.IsNullOrWhiteSpace(cheminComplet) &&
                System.IO.File.Exists(cheminComplet))
            {
                System.IO.File.Delete(cheminComplet);
            }

            var projetId = pieceJointe.ProjetId;

            _context.PiecesJointes.Remove(pieceJointe);

            await _context.SaveChangesAsync();

            TempData["Succes"] = "Pièce jointe supprimée.";

            return RedirectToAction(
                "Details",
                "Projet",
                new { id = projetId });
        }


        //Dowload
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var pieceJointe = await _context.PiecesJointes
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pieceJointe == null)
            {
                return NotFound();
            }

            var cheminComplet = pieceJointe.CheminStockage;

            if (string.IsNullOrWhiteSpace(cheminComplet))
            {
                return NotFound("Chemin du fichier introuvable.");
            }

            if (!System.IO.File.Exists(cheminComplet))
            {
                return NotFound(
                    "Le fichier n'existe pas sur le serveur de fichiers.");
            }

            var extension = Path.GetExtension(
                pieceJointe.NomFichier)
                .ToLowerInvariant();

            var contentType = extension switch
            {
                ".pdf" => "application/pdf",

                ".doc" => "application/msword",

                ".docx" =>
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

                ".xls" => "application/vnd.ms-excel",

                ".xlsx" =>
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                ".ppt" => "application/vnd.ms-powerpoint",

                ".pptx" =>
                    "application/vnd.openxmlformats-officedocument.presentationml.presentation",

                ".jpg" => "image/jpeg",

                ".jpeg" => "image/jpeg",

                ".png" => "image/png",

                ".gif" => "image/gif",

                ".txt" => "text/plain",

                _ => "application/octet-stream"
            };

            var contenu = await System.IO.File.ReadAllBytesAsync(
                cheminComplet);

            return File(
                contenu,
                contentType,
                pieceJointe.NomFichier);
        }



    }
}