using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionProjetSocota.Data;
using GestionProjetSocota.Models;
using GestionProjetSocota.ViewModels;
using Microsoft.AspNetCore.Authorization;
using GestionProjetSocota.Services;
using QuestPDF.Fluent;

namespace GestionProjetSocota.Controllers
{
    public class ProjetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly WorkflowService _workflowService;
        private readonly GeminiService _geminiService;
        private readonly ScoreRisqueService _scoreRisqueService;
        private readonly NotificationService _notificationService;

        public ProjetController(
            ApplicationDbContext context,
            WorkflowService workflowService,
            GeminiService geminiService,
            ScoreRisqueService scoreRisqueService,
            NotificationService notificationService)
        {
            _context = context;
            _workflowService = workflowService;
            _geminiService = geminiService;
            _scoreRisqueService = scoreRisqueService;
            _notificationService = notificationService;
        }


        // =========================================================
        // AVANCEMENT AUTOMATIQUE
        // =========================================================

        private static readonly Dictionary<StatutProjet, int> AvancementParStatut = new()
        {
            { StatutProjet.WaitingRFC, 0 },
            { StatutProjet.RFCApproved, 10 },
            { StatutProjet.Analyse, 20 },
            { StatutProjet.DevStarted, 30 },
            { StatutProjet.Testing, 60 },
            { StatutProjet.Debugging, 75 },
            { StatutProjet.Formation, 90 },
            { StatutProjet.GoLive, 100 },
            { StatutProjet.Support, 100 },
            { StatutProjet.Closed, 100 }
        };


        private static int ObtenirAvancementAutomatique(
            StatutProjet statut,
            int avancementActuel)
        {
            if (statut == StatutProjet.Suspendu ||
                statut == StatutProjet.Cancelled)
            {
                return avancementActuel;
            }

            return AvancementParStatut.TryGetValue(
                statut,
                out var valeur)
                ? valeur
                : avancementActuel;
        }

        // ==========================
        // INDEX
        // ==========================

        public async Task<IActionResult> Index(
            string? recherche,
            int[]? unite,
            int[]? departement,
            StatutProjet[]? statut,
            int[]? type,
            int[]? ownerItId,
            bool statutFiltreActif = false)
        {
            var query = _context.Projets
                .Include(p => p.OwnerIt)
                .Include(p => p.PowerUser)
                .Include(p => p.UniteProjet)
                .Include(p => p.DepartementProjet)
                .Include(p => p.TypeProjetReference)
                .Include(p => p.PlateformeProjet)
                .AsQueryable();


            // ============================================================
            // RECHERCHE GLOBALE
            // ============================================================

            if (!string.IsNullOrWhiteSpace(recherche))
            {
                recherche = recherche.Trim();

                query = query.Where(p =>
                    p.Reference.Contains(recherche) ||
                    p.TicketId.Contains(recherche) ||
                    p.Nom.Contains(recherche) ||
                    p.Description.Contains(recherche) ||

                    (p.UniteProjet != null &&
                     p.UniteProjet.Nom.Contains(recherche)) ||

                    (p.DepartementProjet != null &&
                     p.DepartementProjet.Nom.Contains(recherche)) ||

                    (p.TypeProjetReference != null &&
                     p.TypeProjetReference.Nom.Contains(recherche)) ||

                    (p.PlateformeProjet != null &&
                     p.PlateformeProjet.Nom.Contains(recherche)) ||

                    (p.OwnerIt != null &&
                     p.OwnerIt.Nom.Contains(recherche)) ||

                    (p.PowerUser != null &&
                     p.PowerUser.Nom.Contains(recherche)) ||

                    p.Statut.ToString().Contains(recherche) ||

                    p.Priorite.ToString().Contains(recherche) ||

                    p.DevVolume.Contains(recherche) ||

                    p.Commentaire.Contains(recherche)
                );
            }


            // ============================================================
            // FILTRE SOCIÉTÉ
            // ============================================================

            var toutesLesUnites = await _context.UnitesProjets
                .Where(u => u.Actif)
                .OrderBy(u => u.Nom)
                .ToListAsync();

            if (unite == null)
            {
                unite = toutesLesUnites
                    .Select(u => u.Id)
                    .ToArray();
            }

            if (unite.Length > 0)
            {
                query = query.Where(p =>
                    p.UniteProjetId.HasValue &&
                    unite.Contains(p.UniteProjetId.Value));
            }


            // ============================================================
            // FILTRE DÉPARTEMENT
            // ============================================================

            if (departement != null && departement.Length > 0)
            {
                query = query.Where(p =>
                    p.DepartementProjetId.HasValue &&
                    departement.Contains(p.DepartementProjetId.Value));
            }


            // ============================================================
            // FILTRE STATUT
            // ============================================================

            var tousLesStatuts =
                Enum.GetValues<StatutProjet>();

            /*
             * IMPORTANT :
             *
             * statutFiltreActif = true
             * signifie que l'utilisateur a réellement envoyé
             * le formulaire de filtres.
             *
             * Dans ce cas, on respecte EXACTEMENT son choix.
             *
             * Si false :
             * première ouverture de la page,
             * on sélectionne tous les statuts SAUF :
             * - GoLive
             * - Cancelled
             */

            if (!statutFiltreActif)
            {
                statut = tousLesStatuts
                    .Where(s =>
                        s != StatutProjet.GoLive &&
                        s != StatutProjet.Cancelled)
                    .ToArray();

                query = query.Where(p =>
                    statut.Contains(p.Statut));
            }
            else
            {
                /*
                 * L'utilisateur a utilisé le filtre statut.
                 *
                 * Si aucun statut n'est sélectionné :
                 * aucun projet ne doit être affiché.
                 */

                if (statut != null && statut.Length > 0)
                {
                    query = query.Where(p =>
                        statut.Contains(p.Statut));
                }
                else
                {
                    query = query.Where(p => false);
                }
            }


            // ============================================================
            // FILTRE TYPE
            // ============================================================

            if (type != null && type.Length > 0)
            {
                query = query.Where(p =>
                    p.TypeProjetReferenceId.HasValue &&
                    type.Contains(p.TypeProjetReferenceId.Value));
            }


            // ============================================================
            // FILTRE RESPONSABLE IT
            // ============================================================

            if (ownerItId != null && ownerItId.Length > 0)
            {
                query = query.Where(p =>
                    p.OwnerItId.HasValue &&
                    ownerItId.Contains(p.OwnerItId.Value));
            }


            // ============================================================
            // RÉSULTATS
            // ============================================================

            var projets = await query
                .OrderBy(p => p.Nom)
                .ToListAsync();


            // ============================================================
            // LISTE DES UTILISATEURS
            // ============================================================

            ViewBag.Utilisateurs = await _context.Utilisateurs
                .Where(u => u.EstActif)
                .OrderBy(u => u.Nom)
                .ToListAsync();


            // ============================================================
            // LISTE DES SOCIÉTÉS
            // ============================================================

            ViewBag.Unites = toutesLesUnites;


            // ============================================================
            // LISTE DES DÉPARTEMENTS
            // ============================================================

            ViewBag.Departements = await _context.DepartementsProjets
                .Where(d => d.Actif)
                .OrderBy(d => d.Nom)
                .ToListAsync();


            // ============================================================
            // LISTE DES TYPES
            // ============================================================

            ViewBag.Types = await _context.TypesProjets
                .Where(t => t.Actif)
                .OrderBy(t => t.Nom)
                .ToListAsync();


            // ============================================================
            // FILTRES ACTIFS
            // ============================================================

            ViewBag.FiltresActifs = new
            {
                unite,
                departement,
                statut,
                type,
                ownerItId,
                statutFiltreActif
            };


            // ============================================================
            // DERNIERS COMMENTAIRES
            // ============================================================

            var derniersCommentaires = await _context.Commentaires
                .GroupBy(c => c.ProjetId)
                .Select(g => new
                {
                    ProjetId = g.Key,
                    Date = g.Max(c => c.DatePublication)
                })
                .ToDictionaryAsync(
                    x => x.ProjetId,
                    x => (DateTime?)x.Date
                );


            // ============================================================
            // CALCUL DES SCORES DE RISQUE
            // ============================================================

            var scores =
                new Dictionary<int, (int score, string couleur)>();


            foreach (var p in projets)
            {
                derniersCommentaires.TryGetValue(
                    p.Id,
                    out var dernierCommentaire
                );

                var score =
                    _scoreRisqueService.CalculerScore(
                        p,
                        dernierCommentaire
                    );

                var couleur =
                    _scoreRisqueService.ObtenirCouleur(score);

                scores[p.Id] =
                    (score, couleur);
            }


            ViewBag.Scores = scores;


            // ============================================================
            // RETOUR DE LA VUE
            // ============================================================

            return View(projets);
        }
        // ============================================================
        // EXPORT EXCEL
        // ============================================================

        public async Task<IActionResult> ExporterExcel(
            string? recherche,
            int[]? unite,
            int[]? departement,
            StatutProjet[]? statut,
            int[]? type,
            int[]? ownerItId,
            bool statutFiltreActif = false)
        {
            var query = _context.Projets

                .Include(p => p.OwnerIt)
                .Include(p => p.PowerUser)

                .Include(p => p.UniteProjet)
                .Include(p => p.DepartementProjet)
                .Include(p => p.TypeProjetReference)
                .Include(p => p.PlateformeProjet)

                .AsQueryable();


            // ============================================================
            // RECHERCHE GLOBALE
            // ============================================================

            if (!string.IsNullOrWhiteSpace(recherche))
            {
                recherche = recherche.Trim();

                query = query.Where(p =>
                    p.Reference.Contains(recherche) ||
                    p.TicketId.Contains(recherche) ||
                    p.Nom.Contains(recherche) ||
                    p.Description.Contains(recherche) ||

                    (p.UniteProjet != null &&
                     p.UniteProjet.Nom.Contains(recherche)) ||

                    (p.DepartementProjet != null &&
                     p.DepartementProjet.Nom.Contains(recherche)) ||

                    (p.TypeProjetReference != null &&
                     p.TypeProjetReference.Nom.Contains(recherche)) ||

                    (p.PlateformeProjet != null &&
                     p.PlateformeProjet.Nom.Contains(recherche)) ||

                    (p.OwnerIt != null &&
                     p.OwnerIt.Nom.Contains(recherche)) ||

                    (p.PowerUser != null &&
                     p.PowerUser.Nom.Contains(recherche)) ||

                    p.DevVolume.Contains(recherche) ||

                    p.Commentaire.Contains(recherche)
                );
            }


            // ============================================================
            // FILTRE SOCIÉTÉ
            // ============================================================

            if (unite != null && unite.Length > 0)
            {
                query = query.Where(p =>
                    p.UniteProjetId.HasValue &&
                    unite.Contains(p.UniteProjetId.Value));
            }


            // ============================================================
            // FILTRE DÉPARTEMENT
            // ============================================================

            if (departement != null && departement.Length > 0)
            {
                query = query.Where(p =>
                    p.DepartementProjetId.HasValue &&
                    departement.Contains(p.DepartementProjetId.Value));
            }


            // ============================================================
            // FILTRE STATUT
            // ============================================================

            if (!statutFiltreActif)
            {
                var statutsParDefaut =
                    Enum.GetValues<StatutProjet>()
                        .Where(s =>
                            s != StatutProjet.GoLive &&
                            s != StatutProjet.Closed)
                        .ToArray();

                query = query.Where(p =>
                    statutsParDefaut.Contains(p.Statut));
            }
            else
            {
                if (statut != null && statut.Length > 0)
                {
                    query = query.Where(p =>
                        statut.Contains(p.Statut));
                }
                else
                {
                    query = query.Where(p => false);
                }
            }


            // ============================================================
            // FILTRE TYPE
            // ============================================================

            if (type != null && type.Length > 0)
            {
                query = query.Where(p =>
                    p.TypeProjetReferenceId.HasValue &&
                    type.Contains(p.TypeProjetReferenceId.Value));
            }


            // ============================================================
            // FILTRE RESPONSABLE IT
            // ============================================================

            if (ownerItId != null && ownerItId.Length > 0)
            {
                query = query.Where(p =>
                    p.OwnerItId.HasValue &&
                    ownerItId.Contains(p.OwnerItId.Value));
            }


            // ============================================================
            // RÉSULTATS
            // ============================================================

            var projets = await query
                .OrderBy(p => p.Nom)
                .ToListAsync();


            // ============================================================
            // CRÉATION DU FICHIER EXCEL
            // ============================================================

            using var workbook =
                new ClosedXML.Excel.XLWorkbook();


            var feuille =
                workbook.Worksheets.Add("Projets");


            // ============================================================
            // EN-TÊTES
            // ============================================================

            feuille.Cell(1, 1).Value = "Référence";
            feuille.Cell(1, 2).Value = "Ticket ID";
            feuille.Cell(1, 3).Value = "Nom";
            feuille.Cell(1, 4).Value = "Société";
            feuille.Cell(1, 5).Value = "Département";
            feuille.Cell(1, 6).Value = "Statut";
            feuille.Cell(1, 7).Value = "Owner IT";
            feuille.Cell(1, 8).Value = "Power User";
            feuille.Cell(1, 9).Value = "Type";
            feuille.Cell(1, 10).Value = "Plateforme";
            feuille.Cell(1, 11).Value = "Début";
            feuille.Cell(1, 12).Value = "Fin";
            feuille.Cell(1, 13).Value = "Avancement";
            feuille.Cell(1, 14).Value = "Priorité";


            feuille.Row(1).Style.Font.Bold = true;


            // ============================================================
            // DONNÉES
            // ============================================================

            for (int i = 0; i < projets.Count; i++)
            {
                var p = projets[i];

                var ligne = i + 2;


                feuille.Cell(ligne, 1).Value =
                    string.IsNullOrWhiteSpace(p.Reference)
                        ? "-"
                        : p.Reference;


                feuille.Cell(ligne, 2).Value =
                    p.TicketId;


                feuille.Cell(ligne, 3).Value =
                    p.Nom;


                feuille.Cell(ligne, 4).Value =
                    p.UniteProjet?.Nom ?? "-";


                feuille.Cell(ligne, 5).Value =
                    p.DepartementProjet?.Nom ?? "-";


                feuille.Cell(ligne, 6).Value =
                    p.Statut.ToString();


                feuille.Cell(ligne, 7).Value =
                    p.OwnerIt?.Nom ?? "-";


                feuille.Cell(ligne, 8).Value =
                    p.PowerUser?.Nom ?? "-";


                feuille.Cell(ligne, 9).Value =
                    p.TypeProjetReference?.Nom ?? "-";


                feuille.Cell(ligne, 10).Value =
                    p.PlateformeProjet?.Nom ?? "-";


                feuille.Cell(ligne, 11).Value =
                    p.DateDebut.HasValue
                        ? p.DateDebut.Value.ToString("dd/MM/yyyy")
                        : "-";


                feuille.Cell(ligne, 12).Value =
                    p.DateFin.HasValue
                        ? p.DateFin.Value.ToString("dd/MM/yyyy")
                        : "-";


                feuille.Cell(ligne, 13).Value =
                    p.PourcentageAvancement + "%";


                feuille.Cell(ligne, 14).Value =
                    p.Priorite.ToString();
            }


            // ============================================================
            // STYLE
            // ============================================================

            feuille.Columns().AdjustToContents();


            // ============================================================
            // RETOUR DU FICHIER
            // ============================================================

            using var stream =
                new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "projets.xlsx"
            );
        }

        // ============================================================
        // EXPORT PDF
        // ============================================================

        public async Task<IActionResult> ExporterPdf(
            string? recherche,
            int[]? unite,
            int[]? departement,
            StatutProjet[]? statut,
            int[]? type,
            int[]? ownerItId,
            bool statutFiltreActif = false)
        {
            var query = _context.Projets

                .Include(p => p.OwnerIt)
                .Include(p => p.PowerUser)

                .Include(p => p.UniteProjet)
                .Include(p => p.DepartementProjet)
                .Include(p => p.TypeProjetReference)
                .Include(p => p.PlateformeProjet)

                .AsQueryable();


            // ============================================================
            // RECHERCHE GLOBALE
            // ============================================================

            if (!string.IsNullOrWhiteSpace(recherche))
            {
                recherche = recherche.Trim();

                query = query.Where(p =>
                    p.Reference.Contains(recherche) ||
                    p.TicketId.Contains(recherche) ||
                    p.Nom.Contains(recherche) ||
                    p.Description.Contains(recherche) ||

                    (p.UniteProjet != null &&
                     p.UniteProjet.Nom.Contains(recherche)) ||

                    (p.DepartementProjet != null &&
                     p.DepartementProjet.Nom.Contains(recherche)) ||

                    (p.TypeProjetReference != null &&
                     p.TypeProjetReference.Nom.Contains(recherche)) ||

                    (p.PlateformeProjet != null &&
                     p.PlateformeProjet.Nom.Contains(recherche)) ||

                    (p.OwnerIt != null &&
                     p.OwnerIt.Nom.Contains(recherche)) ||

                    (p.PowerUser != null &&
                     p.PowerUser.Nom.Contains(recherche)) ||

                    p.DevVolume.Contains(recherche) ||

                    p.Commentaire.Contains(recherche)
                );
            }


            // ============================================================
            // FILTRE SOCIÉTÉ
            // ============================================================

            if (unite != null && unite.Length > 0)
            {
                query = query.Where(p =>
                    p.UniteProjetId.HasValue &&
                    unite.Contains(p.UniteProjetId.Value));
            }


            // ============================================================
            // FILTRE DÉPARTEMENT
            // ============================================================

            if (departement != null && departement.Length > 0)
            {
                query = query.Where(p =>
                    p.DepartementProjetId.HasValue &&
                    departement.Contains(p.DepartementProjetId.Value));
            }


            // ============================================================
            // FILTRE STATUT
            // ============================================================

            if (!statutFiltreActif)
            {
                var statutsParDefaut =
                    Enum.GetValues<StatutProjet>()
                        .Where(s =>
                            s != StatutProjet.GoLive &&
                            s != StatutProjet.Closed)
                        .ToArray();

                query = query.Where(p =>
                    statutsParDefaut.Contains(p.Statut));
            }
            else
            {
                if (statut != null && statut.Length > 0)
                {
                    query = query.Where(p =>
                        statut.Contains(p.Statut));
                }
                else
                {
                    query = query.Where(p => false);
                }
            }


            // ============================================================
            // FILTRE TYPE
            // ============================================================

            if (type != null && type.Length > 0)
            {
                query = query.Where(p =>
                    p.TypeProjetReferenceId.HasValue &&
                    type.Contains(p.TypeProjetReferenceId.Value));
            }


            // ============================================================
            // FILTRE RESPONSABLE IT
            // ============================================================

            if (ownerItId != null && ownerItId.Length > 0)
            {
                query = query.Where(p =>
                    p.OwnerItId.HasValue &&
                    ownerItId.Contains(p.OwnerItId.Value));
            }


            // ============================================================
            // RÉSULTATS
            // ============================================================

            var projets = await query
                .OrderBy(p => p.Nom)
                .ToListAsync();


            // ============================================================
            // CRÉATION DU PDF
            // ============================================================

            var document =
                QuestPDF.Fluent.Document.Create(conteneur =>
                {
                    conteneur.Page(page =>
                    {
                        page.Size(842, 595);

                        page.Margin(25);

                        // ==================================================
                        // HEADER
                        // ==================================================

                        page.Header()
                            .Text(
                                "Liste des projets - GestionProjetSocota")
                            .FontSize(16)
                            .Bold()
                            .FontColor("#155D15");


                        // ==================================================
                        // CONTENU
                        // ==================================================

                        page.Content()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(colonnes =>
                                {
                                    colonnes.RelativeColumn(1.2f);
                                    colonnes.RelativeColumn(1f);
                                    colonnes.RelativeColumn(2f);
                                    colonnes.RelativeColumn(1f);
                                    colonnes.RelativeColumn(1.2f);
                                    colonnes.RelativeColumn(1.4f);
                                    colonnes.RelativeColumn(1.5f);
                                });


                                // ==========================================
                                // HEADER TABLEAU
                                // ==========================================

                                table.Header(entete =>
                                {
                                    entete.Cell()
                                        .Text("Référence")
                                        .Bold();

                                    entete.Cell()
                                        .Text("Ticket ID")
                                        .Bold();

                                    entete.Cell()
                                        .Text("Nom")
                                        .Bold();

                                    entete.Cell()
                                        .Text("Société")
                                        .Bold();

                                    entete.Cell()
                                        .Text("Statut")
                                        .Bold();

                                    entete.Cell()
                                        .Text("Owner IT")
                                        .Bold();

                                    entete.Cell()
                                        .Text("Type")
                                        .Bold();
                                });


                                // ==========================================
                                // LIGNES
                                // ==========================================

                                foreach (var p in projets)
                                {
                                    table.Cell()
                                        .Text(
                                            string.IsNullOrWhiteSpace(p.Reference)
                                                ? "-"
                                                : p.Reference);


                                    table.Cell()
                                        .Text(p.TicketId ?? "-");


                                    table.Cell()
                                        .Text(p.Nom ?? "-");


                                    table.Cell()
                                        .Text(
                                            p.UniteProjet?.Nom ?? "-");


                                    table.Cell()
                                        .Text(p.Statut.ToString());


                                    table.Cell()
                                        .Text(
                                            p.OwnerIt?.Nom ?? "-");


                                    table.Cell()
                                        .Text(
                                            p.TypeProjetReference?.Nom ?? "-");
                                }
                            });


                        // ==================================================
                        // FOOTER
                        // ==================================================

                        page.Footer()
                            .AlignCenter()
                            .Text(t =>
                            {
                                t.CurrentPageNumber();

                                t.Span(" / ");

                                t.TotalPages();
                            });
                    });
                });


            // ============================================================
            // GÉNÉRATION
            // ============================================================

            var pdfBytes =
                document.GeneratePdf();


            return File(
                pdfBytes,
                "application/pdf",
                "projets.pdf"
            );
        }

        // =========================================================
        // IMPORT EXCEL
        // =========================================================

        [Authorize(Roles = "Administrateur")]
        [HttpGet]
        public IActionResult ImporterExcel()
        {
            return View();
        }


        [Authorize(Roles = "Administrateur")]
        [HttpPost]
        public async Task<IActionResult> ImporterExcel(IFormFile fichier)
        {
            if (fichier == null || fichier.Length == 0)
            {
                TempData["Erreur"] =
                    "Aucun fichier sélectionné.";

                return RedirectToAction("ImporterExcel");
            }


            var nomUtilisateur =
                User.Identity?.Name;


            var auteur =
                await _context.Utilisateurs
                    .FirstOrDefaultAsync(
                        u => u.NomADUtilisateur == nomUtilisateur);


            if (auteur == null)
            {
                TempData["Erreur"] =
                    "Utilisateur connecté introuvable.";

                return RedirectToAction("ImporterExcel");
            }


            int nbImportes = 0;
            int nbIgnores = 0;

            var raisonsIgnorees =
                new List<string>();


            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);


            try
            {
                // =========================================================
                // OUVERTURE DU FICHIER EXCEL
                // =========================================================

                using var stream =
                    new MemoryStream();

                await fichier.CopyToAsync(stream);


                using var workbook =
                    new ClosedXML.Excel.XLWorkbook(stream);


                // =========================================================
                // VÉRIFICATION DE LA FEUILLE
                // =========================================================

                if (!workbook.Worksheets.Contains("Projets"))
                {
                    TempData["Erreur"] =
                        "La feuille Excel 'Projets' est introuvable.";

                    await transaction.RollbackAsync();

                    return RedirectToAction("ImporterExcel");
                }


                var feuille =
                    workbook.Worksheet("Projets");


                var lignes =
                    feuille.RowsUsed().Skip(1);


                // =========================================================
                // PARCOURS DES LIGNES
                // =========================================================

                foreach (var ligne in lignes)
                {
                    // =====================================================
                    // COLONNES EXCEL
                    // =====================================================

                    // Colonne 1 : Société
                    var uniteTexte =
                        ligne.Cell(1)
                            .GetString()
                            .Trim();


                    // Colonne 2 : Ticket ID
                    var ticketId =
                        ligne.Cell(2)
                            .GetString()
                            .Trim();


                    // =====================================================
                    // Colonne 3 : Reference
                    // =====================================================

                    var referenceExcel =
                        ligne.Cell(3)
                            .GetString()
                            .Trim();


                    // Colonne 4 : Nom
                    var nom =
                        ligne.Cell(4)
                            .GetString()
                            .Trim();


                    // Colonne 5 : Département
                    var departementTexte =
                        ligne.Cell(5)
                            .GetString()
                            .Trim();


                    // Colonne 6 : Type
                    var typeTexte =
                        ligne.Cell(6)
                            .GetString()
                            .Trim()
                            .Replace(" ", "");


                    // Colonne 7 : Plateforme
                    var plateformeTexte =
                        ligne.Cell(7)
                            .GetString()
                            .Trim();


                    // Colonne 8 : Owner IT
                    var ownerItNom =
                        ligne.Cell(8)
                            .GetString()
                            .Trim();


                    // Colonne 9 : Power User
                    var powerUserNom =
                        ligne.Cell(9)
                            .GetString()
                            .Trim();


                    // Colonne 10 : Priorité
                    var prioriteTexte =
                        ligne.Cell(10)
                            .GetString()
                            .Trim();


                    // =====================================================
                    // COLONNE 12 : DEADLINE
                    // =====================================================

                    var deadlineCell =
                        ligne.Cell(12);


                    // =====================================================
                    // COLONNE 14 : STATUT
                    // =====================================================

                    var statutTexte =
                        ligne.Cell(14)
                            .GetString()
                            .Trim()
                            .Replace(" ", "");


                    // =====================================================
                    // COLONNE 15 : DATE DEBUT
                    // =====================================================

                    var dateDebutCell =
                        ligne.Cell(15);


                    // =====================================================
                    // COLONNE 16 : DATE FIN
                    // =====================================================

                    var dateFinCell =
                        ligne.Cell(16);


                    // =====================================================
                    // COLONNE 17 : AVANCEMENT (%)
                    // =====================================================

                    var avancementPourcentageCell =
                        ligne.Cell(17);


                    // =====================================================
                    // COLONNE 18 : AVANCEMENT TEXTE
                    // =====================================================

                    var avancementTexteCell =
                        ligne.Cell(18);


                    // =====================================================
                    // COLONNE 19 : COMMENTAIRE
                    // =====================================================

                    var commentaireExcel =
                        ligne.Cell(19)
                            .GetString()
                            .Trim();


                    // =====================================================
                    // VALIDATION DU NOM
                    // =====================================================

                    if (string.IsNullOrWhiteSpace(nom))
                    {
                        raisonsIgnorees.Add(
                            $"Ligne {ligne.RowNumber()} : nom du projet vide");

                        nbIgnores++;

                        continue;
                    }


                    // =====================================================
                    // DATE DEBUT
                    // OBLIGATOIRE
                    // =====================================================

                    DateTime? dateDebut =
                        LireDateExcel(dateDebutCell);


                    if (!dateDebut.HasValue)
                    {
                        raisonsIgnorees.Add(
                            $"Ligne {ligne.RowNumber()} : la date de début est obligatoire.");

                        nbIgnores++;

                        continue;
                    }


                    // =====================================================
                    // DATE FIN
                    // OPTIONNELLE
                    // =====================================================

                    DateTime? dateFin =
                        LireDateExcel(dateFinCell);


                    if (dateFin.HasValue &&
                        dateFin.Value.Date < dateDebut.Value.Date)
                    {
                        raisonsIgnorees.Add(
                            $"Ligne {ligne.RowNumber()} : date de fin avant date de début.");

                        nbIgnores++;

                        continue;
                    }


                    // =====================================================
                    // DEADLINE
                    // COLONNE 12
                    // OPTIONNELLE
                    // =====================================================

                    DateTime? deadline =
                        LireDateExcel(deadlineCell);


                    if (deadline.HasValue &&
                        deadline.Value.Date < dateDebut.Value.Date)
                    {
                        raisonsIgnorees.Add(
                            $"Ligne {ligne.RowNumber()} : deadline avant date de début.");

                        nbIgnores++;

                        continue;
                    }


                    // =====================================================
                    // AVANCEMENT (%)
                    // COLONNE 17
                    // =====================================================

                    int pourcentageAvancement = 0;


                    if (avancementPourcentageCell.DataType ==
                        ClosedXML.Excel.XLDataType.Number)
                    {
                        var valeur =
                            avancementPourcentageCell.GetDouble();

                        if (valeur >= 0 && valeur <= 1)
                        {
                            pourcentageAvancement =
                                (int)Math.Round(
                                    valeur * 100);
                        }
                        else
                        {
                            pourcentageAvancement =
                                (int)Math.Round(valeur);
                        }


                        pourcentageAvancement =
                            Math.Clamp(
                                pourcentageAvancement,
                                0,
                                100);
                    }
                    else
                    {
                        var textePourcentage =
                            avancementPourcentageCell
                                .GetString()
                                .Trim();


                        if (!string.IsNullOrWhiteSpace(
                            textePourcentage))
                        {
                            textePourcentage =
                                textePourcentage
                                    .Replace("%", "")
                                    .Replace(",", ".")
                                    .Trim();


                            if (double.TryParse(
                                textePourcentage,
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out var valeur))
                            {
                                if (valeur >= 0 &&
                                    valeur <= 1)
                                {
                                    pourcentageAvancement =
                                        (int)Math.Round(
                                            valeur * 100);
                                }
                                else
                                {
                                    pourcentageAvancement =
                                        (int)Math.Round(
                                            valeur);
                                }


                                pourcentageAvancement =
                                    Math.Clamp(
                                        pourcentageAvancement,
                                        0,
                                        100);
                            }
                        }
                    }


                    // =====================================================
                    // AVANCEMENT TEXTE
                    // COLONNE 18
                    // =====================================================

                    var avancementTexte =
                        avancementTexteCell
                            .GetString()
                            .Trim();


                    if (avancementTexte == "-")
                    {
                        avancementTexte =
                            string.Empty;
                    }


                    // =====================================================
                    // SOCIÉTÉ
                    // =====================================================

                    var unite =
                        string.IsNullOrWhiteSpace(uniteTexte)
                            ? null
                            : await ObtenirOuCreerUnite(
                                uniteTexte);


                    // =====================================================
                    // DÉPARTEMENT
                    // =====================================================

                    var departement =
                        string.IsNullOrWhiteSpace(departementTexte)
                            ? null
                            : await ObtenirOuCreerDepartement(
                                departementTexte);


                    // =====================================================
                    // TYPE PROJET
                    // =====================================================

                    var typeProjet =
                        string.IsNullOrWhiteSpace(typeTexte)
                            ? null
                            : await ObtenirOuCreerTypeProjet(
                                typeTexte);


                    // =====================================================
                    // PLATEFORME
                    // =====================================================

                    var plateforme =
                        string.IsNullOrWhiteSpace(plateformeTexte)
                            ? null
                            : await ObtenirOuCreerPlateforme(
                                plateformeTexte);


                    // =====================================================
                    // STATUT
                    // =====================================================

                    if (!Enum.TryParse<StatutProjet>(
                        statutTexte,
                        true,
                        out var statut))
                    {
                        statut =
                            StatutProjet.WaitingRFC;
                    }


                    // =====================================================
                    // PRIORITÉ
                    // =====================================================

                    var priorite =
                        PrioriteProjet.Medium;


                    if (Enum.TryParse<PrioriteProjet>(
                        prioriteTexte,
                        true,
                        out var prioriteParsee)
                        &&
                        Enum.IsDefined(
                            typeof(PrioriteProjet),
                            prioriteParsee))
                    {
                        priorite =
                            prioriteParsee;
                    }


                    // =====================================================
                    // UTILISATEURS
                    // =====================================================

                    var ownerIt =
                        await ObtenirOuCreerUtilisateur(
                            ownerItNom);


                    var powerUser =
                        await ObtenirOuCreerUtilisateur(
                            powerUserNom);


                    // =====================================================
                    // GÉNÉRATION DE LA RÉFÉRENCE
                    // =====================================================

                    string? referenceFinale =
                        null;


                    // -----------------------------------------------------
                    // PRIORITÉ 1 :
                    // Utiliser la Reference présente dans Excel
                    // -----------------------------------------------------

                    if (!string.IsNullOrWhiteSpace(referenceExcel) &&
                        referenceExcel != "-")
                    {
                        referenceFinale =
                            referenceExcel;
                    }
                    else
                    {
                        // -------------------------------------------------
                        // PRIORITÉ 2 :
                        // Génération automatique si Excel est vide
                        // -------------------------------------------------

                        if (unite != null)
                        {
                            var compteur =
                                await _context.ReferenceCompteurs
                                    .FirstOrDefaultAsync(
                                        r =>
                                            r.UniteProjetId ==
                                            unite.Id);


                            if (compteur == null)
                            {
                                compteur =
                                    new ReferenceCompteur
                                    {
                                        UniteProjetId =
                                            unite.Id,

                                        Prefixe =
                                            unite.Prefixe,

                                        DernierNumero =
                                            0
                                    };


                                _context.ReferenceCompteurs.Add(
                                    compteur);


                                await _context.SaveChangesAsync();
                            }


                            compteur.DernierNumero++;


                            referenceFinale =
                                $"{compteur.Prefixe}-{compteur.DernierNumero}";
                        }
                    }


                    // =====================================================
                    // CRÉATION DU PROJET
                    // =====================================================

                    var projet =
                        new Projet
                        {
                            TicketId =
                                string.IsNullOrWhiteSpace(ticketId)
                                    ? $"IMPORT-{nbImportes + 1}"
                                    : ticketId,

                            Reference =
                                referenceFinale ??
                                string.Empty,

                            Nom =
                                nom,

                            Description =
                                string.Empty,

                            // CLASSIFICATION
                            UniteProjetId =
                                unite?.Id,

                            DepartementProjetId =
                                departement?.Id,

                            TypeProjetReferenceId =
                                typeProjet?.Id,

                            PlateformeProjetId =
                                plateforme?.Id,

                            // PRIORITÉ
                            Priorite =
                                priorite,

                            // DATES
                            DateDebut =
                                dateDebut.Value,

                            DateFin =
                                dateFin,

                            Deadline =
                                deadline,

                            // AVANCEMENT
                            PourcentageAvancement =
                                pourcentageAvancement,

                            Avancement =
                                avancementTexte,

                            // RESPONSABLES
                            OwnerItId =
                                ownerIt?.Id,

                            PowerUserId =
                                powerUser?.Id,

                            // STATUT
                            Statut =
                                statut
                        };


                    _context.Projets.Add(
                        projet);


                    // =====================================================
                    // COMMENTAIRE
                    // =====================================================

                    if (!string.IsNullOrWhiteSpace(
                        commentaireExcel))
                    {
                        var commentaire =
                            new Commentaire
                            {
                                Projet =
                                    projet,

                                AuteurId =
                                    auteur.Id,

                                Contenu =
                                    commentaireExcel,

                                DatePublication =
                                    DateTime.Now
                            };


                        _context.Commentaires.Add(
                            commentaire);
                    }


                    nbImportes++;
                }


                // =========================================================
                // SAUVEGARDE
                // =========================================================

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                // =========================================================
                // MESSAGE DE SUCCÈS
                // =========================================================

                TempData["Succes"] =
                    $"{nbImportes} projet(s) importé(s) avec succès. " +
                    $"{nbIgnores} ligne(s) ignorée(s).";


                if (raisonsIgnorees.Count > 0)
                {
                    TempData["Erreur"] =
                        string.Join(
                            " | ",
                            raisonsIgnorees.Take(10));
                }


                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();


                TempData["Erreur"] =
                    "Une erreur est survenue pendant l'import Excel. " +
                    "Aucun projet n'a été importé. " +
                    $"Détail : {ex.Message}";


                return RedirectToAction("ImporterExcel");
            }
        }


        // =========================================================
        // LECTURE DATE EXCEL
        // =========================================================

        private static DateTime? LireDateExcel(
            ClosedXML.Excel.IXLCell cell)
        {
            if (cell.DataType ==
                ClosedXML.Excel.XLDataType.DateTime)
            {
                return cell.GetDateTime();
            }


            var texte =
                cell.GetString().Trim();


            if (string.IsNullOrWhiteSpace(texte) ||
                texte == "-")
            {
                return null;
            }


            if (DateTime.TryParse(
                texte,
                out var date))
            {
                return date;
            }


            return null;
        }


        // =========================================================
        // UTILISATEUR IMPORT
        // =========================================================

        private async Task<Utilisateur?> ObtenirOuCreerUtilisateur(
            string nom)
        {
            if (string.IsNullOrWhiteSpace(nom) ||
                nom == "-")
            {
                return null;
            }


            var utilisateur =
                await _context.Utilisateurs
                    .FirstOrDefaultAsync(
                        u => u.Nom == nom);


            if (utilisateur == null)
            {
                utilisateur =
                    new Utilisateur
                    {
                        NomADUtilisateur =
                            $"IMPORT\\{nom}",

                        Nom =
                            nom,

                        Email =
                            string.Empty,

                        Role =
                            RoleUtilisateur.Lecteur
                    };


                _context.Utilisateurs.Add(
                    utilisateur);


                await _context.SaveChangesAsync();
            }


            return utilisateur;
        }


        // =========================================================
        // UNITÉ / SOCIÉTÉ
        // =========================================================

        private async Task<UniteProjet> ObtenirOuCreerUnite(
            string nom)
        {
            var unite =
                await _context.UnitesProjets
                    .FirstOrDefaultAsync(u =>
                        u.Actif &&
                        u.Nom.ToLower() ==
                        nom.ToLower());


            if (unite == null)
            {
                var prefixe =
                    nom.Length >= 2
                        ? nom.Substring(0, 2).ToUpper()
                        : nom.ToUpper();


                unite =
                    new UniteProjet
                    {
                        Nom =
                            nom,

                        Prefixe =
                            prefixe,

                        Actif =
                            true
                    };


                _context.UnitesProjets.Add(
                    unite);


                await _context.SaveChangesAsync();
            }


            return unite;
        }


        // =========================================================
        // DÉPARTEMENT
        // =========================================================

        private async Task<DepartementProjet>
            ObtenirOuCreerDepartement(
                string nom)
        {
            var departement =
                await _context.DepartementsProjets
                    .FirstOrDefaultAsync(d =>
                        d.Actif &&
                        d.Nom.ToLower() ==
                        nom.ToLower());


            if (departement == null)
            {
                departement =
                    new DepartementProjet
                    {
                        Nom =
                            nom,

                        Actif =
                            true
                    };


                _context.DepartementsProjets.Add(
                    departement);


                await _context.SaveChangesAsync();
            }


            return departement;
        }


        // =========================================================
        // TYPE PROJET
        // =========================================================

        private async Task<TypeProjetReference>
            ObtenirOuCreerTypeProjet(
                string nom)
        {
            var typeProjet =
                await _context.TypesProjets
                    .FirstOrDefaultAsync(t =>
                        t.Actif &&
                        t.Nom.ToLower() ==
                        nom.ToLower());


            if (typeProjet == null)
            {
                typeProjet =
                    new TypeProjetReference
                    {
                        Nom =
                            nom,

                        Actif =
                            true
                    };


                _context.TypesProjets.Add(
                    typeProjet);


                await _context.SaveChangesAsync();
            }


            return typeProjet;
        }


        // =========================================================
        // PLATEFORME
        // =========================================================

        private async Task<PlateformeProjet>
            ObtenirOuCreerPlateforme(
                string nom)
        {
            var plateforme =
                await _context.PlateformesProjets
                    .FirstOrDefaultAsync(p =>
                        p.Actif &&
                        p.Nom.ToLower() ==
                        nom.ToLower());


            if (plateforme == null)
            {
                plateforme =
                    new PlateformeProjet
                    {
                        Nom =
                            nom,

                        Actif =
                            true
                    };


                _context.PlateformesProjets.Add(
                    plateforme);


                await _context.SaveChangesAsync();
            }


            return plateforme;
        }

        // =========================================================
        // KANBAN
        // =========================================================

        public async Task<IActionResult> Kanban()
        {
            var projets =
                await _context.Projets
                    .AsNoTracking()
                    .Include(p => p.OwnerIt)
                    .Include(p => p.UniteProjet)
                    .Include(p => p.DepartementProjet)
                    .Include(p => p.TypeProjetReference)
                    .Include(p => p.PlateformeProjet)
                    .ToListAsync();


            var statutsAffiches =
                new List<StatutProjet>
                {
                    StatutProjet.WaitingRFC,
                    StatutProjet.RFCApproved,
                    StatutProjet.Analyse,
                    StatutProjet.DevStarted,
                    StatutProjet.Testing,
                    StatutProjet.Debugging,
                    StatutProjet.Formation,
                    StatutProjet.GoLive,
                    StatutProjet.Support,
                    StatutProjet.Closed
                };


            ViewBag.StatutsAffiches =
                statutsAffiches;


            return View(projets);
        }


        // =========================================================
        // ROADMAP
        // =========================================================

        public async Task<IActionResult> Roadmap()
        {
            var projets =
                await _context.Projets
                    .Include(p => p.OwnerIt)
                    .Where(p =>
                        p.DateFin.HasValue &&
                        p.Statut != StatutProjet.Cancelled)
                    .ToListAsync();


            var maintenant =
                DateTime.Now;


            var trimestres =
                new List<(string Label, List<Projet> Projets)>();


            for (int i = 0; i < 4; i++)
            {
                var dateReference =
                    maintenant.AddMonths(i * 3);


                var trimestreNum =
                    (dateReference.Month - 1) / 3 + 1;


                var label =
                    $"T{trimestreNum} {dateReference.Year}";


                var moisDebut =
                    (trimestreNum - 1) * 3 + 1;


                var debutTrimestre =
                    new DateTime(
                        dateReference.Year,
                        moisDebut,
                        1);


                var finTrimestre =
                    debutTrimestre
                        .AddMonths(3)
                        .AddDays(-1);


                var projetsDuTrimestre =
                    projets
                        .Where(p =>
                            p.DateFin!.Value.Date >=
                                debutTrimestre.Date &&

                            p.DateFin.Value.Date <=
                                finTrimestre.Date)
                        .OrderBy(p => p.DateFin)
                        .ToList();


                trimestres.Add(
                    (label, projetsDuTrimestre));
            }


            ViewBag.Trimestres =
                trimestres;


            return View();
        }


        // =========================================================
        // GANTT
        // =========================================================

        public async Task<IActionResult> Gantt()
        {
            var projets =
                await _context.Projets
                    .Where(p =>
                        p.Statut !=
                        StatutProjet.Cancelled)
                    .ToListAsync();


            return View(projets);
        }


        // =========================================================
        // DASHBOARD
        // =========================================================

        public async Task<IActionResult> Dashboard()
        {
            var projets =
                await _context.Projets
                    .AsNoTracking()
                    .Include(p => p.UniteProjet)
                    .Include(p => p.DepartementProjet)
                    .Select(p => new
                    {
                        p.Statut,

                        UniteNom =
                            p.UniteProjet != null
                                ? p.UniteProjet.Nom
                                : null,

                        DepartementNom =
                            p.DepartementProjet != null
                                ? p.DepartementProjet.Nom
                                : null,

                        p.DateFin,
                        p.Deadline,

                        OwnerItNom =
                            p.OwnerIt != null
                                ? p.OwnerIt.Nom
                                : null
                    })
                    .ToListAsync();


            var maintenant =
                DateTime.Now;


            var stats =
                new DashboardViewModel
                {
                    TotalProjets =
                        projets.Count,

                    ProjetsActifs =
                        projets.Count(p =>
                            p.Statut !=
                                StatutProjet.Closed &&
                            p.Statut !=
                                StatutProjet.Cancelled),

                    ProjetsTermines =
                        projets.Count(p =>
                            p.Statut ==
                            StatutProjet.Closed),

                    ProjetsSuspendus =
                        projets.Count(p =>
                            p.Statut ==
                            StatutProjet.Suspendu),

                    // Retard = Deadline dépassée
                    // uniquement lorsqu'une Deadline existe.
                    ProjetsEnRetard =
                        projets.Count(p =>
                            p.Deadline.HasValue &&
                            p.Deadline.Value.Date <
                                maintenant.Date &&
                            p.Statut !=
                                StatutProjet.Closed &&
                            p.Statut !=
                                StatutProjet.Cancelled),

                    RepartitionParStatut =
                        projets
                            .GroupBy(p => p.Statut)
                            .Select(g =>
                                new StatDonnee
                                {
                                    Label =
                                        g.Key.ToString(),

                                    Valeur =
                                        g.Count()
                                })
                            .ToList(),

                    RepartitionParUnite =
                        projets
                            .Where(p =>
                                p.UniteNom != null)
                            .GroupBy(p =>
                                p.UniteNom!)
                            .Select(g =>
                                new StatDonnee
                                {
                                    Label =
                                        g.Key,

                                    Valeur =
                                        g.Count()
                                })
                            .ToList(),

                    RepartitionParDepartement =
                        projets
                            .Where(p =>
                                p.DepartementNom != null)
                            .GroupBy(p =>
                                p.DepartementNom!)
                            .Select(g =>
                                new StatDonnee
                                {
                                    Label =
                                        g.Key,

                                    Valeur =
                                        g.Count()
                                })
                            .ToList(),

                    ChargeParOwnerIt =
                        projets
                            .Where(p =>
                                p.OwnerItNom != null)
                            .GroupBy(p =>
                                p.OwnerItNom!)
                            .Select(g =>
                                new StatDonnee
                                {
                                    Label =
                                        g.Key,

                                    Valeur =
                                        g.Count()
                                })
                            .ToList()
                };


            return View(stats);
        }


        // =========================================================
        // DASHBOARD COMEX
        // =========================================================

        public async Task<IActionResult> DashboardComex()
        {
            var projets =
                await _context.Projets
                    .AsNoTracking()
                    .Include(p => p.TypeProjetReference)
                    .Include(p => p.PlateformeProjet)
                    .Select(p => new
                    {
                        p.Statut,

                        TypeNom =
                            p.TypeProjetReference != null
                                ? p.TypeProjetReference.Nom
                                : null,

                        PlateformeNom =
                            p.PlateformeProjet != null
                                ? p.PlateformeProjet.Nom
                                : null,

                        p.DateFin,
                        p.Deadline
                    })
                    .ToListAsync();


            var maintenant =
                DateTime.Now;


            int vert = 0;
            int orange = 0;
            int rouge = 0;


            foreach (var p in projets)
            {
                if (p.Statut ==
                        StatutProjet.Closed ||
                    p.Statut ==
                        StatutProjet.Cancelled)
                {
                    continue;
                }


                // Sans Deadline :
                // aucune alerte de retard.
                if (!p.Deadline.HasValue ||
                    p.Deadline.Value.Date >=
                        maintenant.Date)
                {
                    vert++;
                }
                else
                {
                    var joursRetard =
                        (maintenant.Date -
                         p.Deadline.Value.Date).Days;


                    if (joursRetard < 30)
                    {
                        orange++;
                    }
                    else
                    {
                        rouge++;
                    }
                }
            }


            var stats =
                new DashboardViewModel
                {
                    TotalProjets =
                        projets.Count,

                    ProjetsActifs =
                        projets.Count(p =>
                            p.Statut !=
                                StatutProjet.Closed &&
                            p.Statut !=
                                StatutProjet.Cancelled),

                    PortfolioVert =
                        vert,

                    PortfolioOrange =
                        orange,

                    PortfolioRouge =
                        rouge,

                    RepartitionParType =
                        projets
                            .Where(p =>
                                p.TypeNom != null)
                            .GroupBy(p =>
                                p.TypeNom!)
                            .Select(g =>
                                new StatDonnee
                                {
                                    Label =
                                        g.Key,

                                    Valeur =
                                        g.Count()
                                })
                            .ToList(),

                    RepartitionParPlateforme =
                        projets
                            .Where(p =>
                                p.PlateformeNom != null)
                            .GroupBy(p =>
                                p.PlateformeNom!)
                            .Select(g =>
                                new StatDonnee
                                {
                                    Label =
                                        g.Key,

                                    Valeur =
                                        g.Count()
                                })
                            .ToList()
                };


            return View(stats);
        }


        // =========================================================
        // DASHBOARD IT MANAGER
        // =========================================================

        public async Task<IActionResult> DashboardItManager()
        {
            var projets =
                await _context.Projets
                    .AsNoTracking()
                    .Select(p => new
                    {
                        p.Statut,
                        p.Priorite,
                        p.DateFin,
                        p.Deadline,
                        p.DateCreation,

                        OwnerItNom =
                            p.OwnerIt != null
                                ? p.OwnerIt.Nom
                                : null
                    })
                    .ToListAsync();


            var maintenant =
                DateTime.Now;

            var aujourdHui =
                maintenant.Date;

            var dans30Jours =
                aujourdHui.AddDays(30);


            var stats =
                new DashboardViewModel
                {
                    TotalProjets =
                        projets.Count,

                    ProjetsActifs =
                        projets.Count(p =>
                            p.Statut !=
                                StatutProjet.Closed &&
                            p.Statut !=
                                StatutProjet.Cancelled),

                    // Retard = Deadline dépassée
                    ProjetsEnRetard =
                        projets.Count(p =>
                            p.Deadline.HasValue &&
                            p.Deadline.Value.Date <
                                aujourdHui &&
                            p.Statut !=
                                StatutProjet.Closed &&
                            p.Statut !=
                                StatutProjet.Cancelled),

                    ProjetsCritiques =
                        projets.Count(p =>
                            p.Priorite ==
                                PrioriteProjet.High &&
                            p.Statut !=
                                StatutProjet.Closed &&
                            p.Statut !=
                                StatutProjet.Cancelled),

                    // Ici on conserve DateFin :
                    // il s'agit des fins prévues dans les
                    // 30 prochains jours.
                    DatesFinDuMois =
                        await _context.Projets
                            .AsNoTracking()
                            .Include(p => p.OwnerIt)
                            .Where(p =>
                                p.DateFin.HasValue &&
                                p.DateFin.Value.Date >=
                                    aujourdHui &&
                                p.DateFin.Value.Date <=
                                    dans30Jours &&
                                p.Statut !=
                                    StatutProjet.Closed &&
                                p.Statut !=
                                    StatutProjet.Cancelled)
                            .OrderBy(p => p.DateFin)
                            .ToListAsync(),

                    RepartitionParStatut =
                        projets
                            .GroupBy(p => p.Statut)
                            .Select(g =>
                                new StatDonnee
                                {
                                    Label =
                                        g.Key.ToString(),

                                    Valeur =
                                        g.Count()
                                })
                            .ToList(),

                    ChargeParOwnerIt =
                        projets
                            .Where(p =>
                                p.OwnerItNom != null &&
                                p.Statut !=
                                    StatutProjet.Closed &&
                                p.Statut !=
                                    StatutProjet.Cancelled)
                            .GroupBy(p =>
                                p.OwnerItNom!)
                            .Select(g =>
                                new StatDonnee
                                {
                                    Label =
                                        g.Key,

                                    Valeur =
                                        g.Count()
                                })
                            .ToList(),

                    AgingProjets =
                        new List<StatDonnee>
                        {
                            new()
                            {
                                Label =
                                    "0-30 jours",

                                Valeur =
                                    projets.Count(p =>
                                        (maintenant -
                                            p.DateCreation).Days <=
                                            30 &&

                                        p.Statut !=
                                            StatutProjet.Closed &&

                                        p.Statut !=
                                            StatutProjet.Cancelled)
                            },

                            new()
                            {
                                Label =
                                    "31-60 jours",

                                Valeur =
                                    projets.Count(p =>
                                        (maintenant -
                                            p.DateCreation).Days > 30 &&

                                        (maintenant -
                                            p.DateCreation).Days <= 60 &&

                                        p.Statut !=
                                            StatutProjet.Closed &&

                                        p.Statut !=
                                            StatutProjet.Cancelled)
                            },

                            new()
                            {
                                Label =
                                    "61-90 jours",

                                Valeur =
                                    projets.Count(p =>
                                        (maintenant -
                                            p.DateCreation).Days > 60 &&

                                        (maintenant -
                                            p.DateCreation).Days <= 90 &&

                                        p.Statut !=
                                            StatutProjet.Closed &&

                                        p.Statut !=
                                            StatutProjet.Cancelled)
                            },

                            new()
                            {
                                Label =
                                    "90+ jours",

                                Valeur =
                                    projets.Count(p =>
                                        (maintenant -
                                            p.DateCreation).Days > 90 &&

                                        p.Statut !=
                                            StatutProjet.Closed &&

                                        p.Statut !=
                                            StatutProjet.Cancelled)
                            }
                        }
                };


            return View(stats);
        }


        // =========================================================
        // DETAILS
        // =========================================================

        public async Task<IActionResult> Details(int id)
        {
            var projet =
                await _context.Projets
                    .Include(p => p.OwnerIt)
                    .Include(p => p.PowerUser)
                    .Include(p => p.UniteProjet)
                    .Include(p => p.DepartementProjet)
                    .Include(p => p.TypeProjetReference)
                    .Include(p => p.PlateformeProjet)
                    .FirstOrDefaultAsync(
                        p => p.Id == id);


            if (projet == null)
            {
                return NotFound();
            }


            var rfc =
                await _context.RFCs
                    .Include(r => r.Champion)
                    .Include(r => r.Sponsor)
                    .FirstOrDefaultAsync(
                        r => r.ProjetId == id);


            var actions =
                await _context.Actions
                    .Include(a => a.Responsable)
                    .Where(a =>
                        a.ProjetId == id)
                    .ToListAsync();


            ViewBag.RFC =
                rfc;

            ViewBag.Actions =
                actions;


            var commentaires =
                await _context.Commentaires
                    .Include(c => c.Auteur)
                    .Where(c =>
                        c.ProjetId == id)
                    .OrderByDescending(
                        c => c.DatePublication)
                    .ToListAsync();


            ViewBag.Commentaires =
                commentaires;


            var piecesJointes =
                await _context.PiecesJointes
                    .Where(pj =>
                        pj.ProjetId == id)
                    .OrderByDescending(
                        pj => pj.DateAjout)
                    .ToListAsync();


            ViewBag.PiecesJointes =
                piecesJointes;


            var historique =
                await _context.HistoriqueProjets
                    .Include(h => h.Utilisateur)
                    .Where(h =>
                        h.ProjetId == id)
                    .OrderByDescending(
                        h => h.DateAction)
                    .ToListAsync();


            ViewBag.Historique =
                historique;


            var dernierCommentaire =
                commentaires.Any()
                    ? commentaires.Max(
                        c => c.DatePublication)
                    : (DateTime?)null;


            var score =
                _scoreRisqueService.CalculerScore(
                    projet,
                    dernierCommentaire);


            ViewBag.ScoreRisque =
                score;


            ViewBag.NiveauRisque =
                _scoreRisqueService.ObtenirNiveau(
                    score);


            ViewBag.CouleurRisque =
                _scoreRisqueService.ObtenirCouleur(
                    score);


            return View(projet);
        }


        // =========================================================
        // COMPTE RENDU
        // =========================================================

        public async Task<IActionResult> GenererCompteRendu(
            int id)
        {
            var projet =
                await _context.Projets
                    .Include(p => p.OwnerIt)
                    .FirstOrDefaultAsync(
                        p => p.Id == id);


            if (projet == null)
            {
                return NotFound();
            }


            var actions =
                await _context.Actions
                    .Where(a =>
                        a.ProjetId == id)
                    .ToListAsync();


            var commentaires =
                await _context.Commentaires
                    .Include(c => c.Auteur)
                    .OrderByDescending(
                        c => c.DatePublication)
                    .Where(c =>
                        c.ProjetId == id)
                    .Take(5)
                    .ToListAsync();


            var dateDebut = projet.DateDebut.HasValue
                 ? projet.DateDebut.Value.ToString("dd/MM/yyyy")
                 : "Non définie";

            var dateFin =
                projet.DateFin.HasValue
                    ? projet.DateFin.Value
                        .ToString("dd/MM/yyyy")
                    : "non définie";


            var deadline =
                projet.Deadline.HasValue
                    ? projet.Deadline.Value
                        .ToString("dd/MM/yyyy")
                    : "non définie";


            var prompt =
                $@"Rédige un compte-rendu professionnel et concis (en français) pour le projet suivant, destiné à un rapport de suivi interne.

Nom du projet : {projet.Nom}
Statut actuel : {projet.Statut}
Avancement : {projet.PourcentageAvancement}%
Responsable IT : {projet.OwnerIt?.Nom ?? "non assigné"}

Date de début : {dateDebut}
Fin prévue : {dateFin}
Deadline : {deadline}

Actions en cours ({actions.Count}) :
{string.Join("\n", actions.Select(a => $"- {a.Description} ({a.Statut})"))}

Derniers commentaires :
{string.Join("\n", commentaires.Select(c => $"- {c.Auteur?.Nom}: {c.Contenu}"))}

Structure attendue : un paragraphe de résumé de la situation, suivi des points d'attention si nécessaire.";


            var compteRendu =
                await _geminiService.GenererCompteRendu(
                    prompt);


            ViewBag.Projet =
                projet;

            ViewBag.CompteRendu =
                compteRendu;


            return View();
        }

        // =========================================================
        // CREATE - GET
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model =
                new ProjetCreateViewModel
                {
                    UtilisateursDisponibles =
                        await _context.Utilisateurs
                            .Where(u =>
                                u.EstActif)
                            .OrderBy(u =>
                                u.Nom)
                            .ToListAsync(),

                    UnitesDisponibles =
                        await _context.UnitesProjets
                            .Where(u =>
                                u.Actif)
                            .OrderBy(u =>
                                u.Nom)
                            .ToListAsync(),

                    DepartementsDisponibles =
                        await _context.DepartementsProjets
                            .Where(d =>
                                d.Actif)
                            .OrderBy(d =>
                                d.Nom)
                            .ToListAsync(),

                    TypesDisponibles =
                        await _context.TypesProjets
                            .Where(t =>
                                t.Actif)
                            .OrderBy(t =>
                                t.Nom)
                            .ToListAsync(),

                    PlateformesDisponibles =
                        await _context.PlateformesProjets
                            .Where(p =>
                                p.Actif)
                            .OrderBy(p =>
                                p.Nom)
                            .ToListAsync()
                };

            return View(model);
        }


        // =========================================================
        // CREATE - POST
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> Create(
            ProjetCreateViewModel model)
        {
            // =====================================================
            // DATE DE DÉBUT - OBLIGATOIRE
            // =====================================================

            if (!model.DateDebut.HasValue)
            {
                ModelState.AddModelError(
                    "DateDebut",
                    "La date de début est obligatoire.");
            }


            // =====================================================
            // DATE FIN - OPTIONNELLE
            // La date de fin prévue ne peut pas être avant
            // la date de début.
            // =====================================================

            if (model.DateDebut.HasValue &&
                model.DateFin.HasValue &&
                model.DateFin.Value.Date <
                model.DateDebut.Value.Date)
            {
                ModelState.AddModelError(
                    "DateFin",
                    "La date de fin doit être postérieure ou égale à la date de début.");
            }


            // =====================================================
            // DEADLINE - OPTIONNELLE
            // La deadline ne peut pas être avant
            // la date de début.
            //
            // IMPORTANT :
            // aucune comparaison n'est faite entre Deadline
            // et DateFin.
            //
            // Donc :
            // Deadline < DateFin = AUTORISÉ
            // Deadline > DateFin = AUTORISÉ
            // Deadline = DateFin = AUTORISÉ
            // =====================================================

            if (model.DateDebut.HasValue &&
                model.Deadline.HasValue &&
                model.Deadline.Value.Date <
                model.DateDebut.Value.Date)
            {
                ModelState.AddModelError(
                    "Deadline",
                    "La deadline doit être postérieure ou égale à la date de début.");
            }


            // =====================================================
            // UNITÉ
            // =====================================================

            var unite =
                await _context.UnitesProjets
                    .FirstOrDefaultAsync(u =>
                        u.Id ==
                            model.UniteProjetId &&
                        u.Actif);

            if (unite == null)
            {
                ModelState.AddModelError(
                    "UniteProjetId",
                    "L'unité sélectionnée est invalide.");
            }


            // =====================================================
            // DÉPARTEMENT
            // =====================================================

            var departement =
                await _context.DepartementsProjets
                    .FirstOrDefaultAsync(d =>
                        d.Id ==
                            model.DepartementProjetId &&
                        d.Actif);

            if (departement == null)
            {
                ModelState.AddModelError(
                    "DepartementProjetId",
                    "Le département sélectionné est invalide.");
            }


            // =====================================================
            // TYPE
            // =====================================================

            var typeProjet =
                await _context.TypesProjets
                    .FirstOrDefaultAsync(t =>
                        t.Id ==
                            model.TypeProjetReferenceId &&
                        t.Actif);

            if (typeProjet == null)
            {
                ModelState.AddModelError(
                    "TypeProjetReferenceId",
                    "Le type de projet sélectionné est invalide.");
            }


            // =====================================================
            // PLATEFORME
            // =====================================================

            var plateforme =
                await _context.PlateformesProjets
                    .FirstOrDefaultAsync(p =>
                        p.Id ==
                            model.PlateformeProjetId &&
                        p.Actif);

            if (plateforme == null)
            {
                ModelState.AddModelError(
                    "PlateformeProjetId",
                    "La plateforme sélectionnée est invalide.");
            }


            // =====================================================
            // RETOUR FORMULAIRE SI ERREUR
            // =====================================================

            if (!ModelState.IsValid)
            {
                await ChargerDonneesFormulaireCreate(
                    model);

                return View(model);
            }


            // =====================================================
            // TRANSACTION
            // =====================================================

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                unite =
                    await _context.UnitesProjets
                        .FirstOrDefaultAsync(u =>
                            u.Id ==
                                model.UniteProjetId &&
                            u.Actif);

                if (unite == null)
                {
                    throw new InvalidOperationException(
                        "L'unité sélectionnée n'existe plus ou est inactive.");
                }


                // =================================================
                // COMPTEUR DE RÉFÉRENCE
                // =================================================

                var compteur =
                    await _context.ReferenceCompteurs
                        .FirstOrDefaultAsync(
                            r =>
                                r.UniteProjetId ==
                                unite.Id);

                if (compteur == null)
                {
                    compteur =
                        new ReferenceCompteur
                        {
                            UniteProjetId =
                                unite.Id,

                            Prefixe =
                                unite.Prefixe,

                            DernierNumero =
                                0
                        };

                    _context.ReferenceCompteurs.Add(
                        compteur);

                    await _context.SaveChangesAsync();
                }


                compteur.DernierNumero++;


                // =================================================
                // GÉNÉRATION DE LA RÉFÉRENCE
                // =================================================

                var referenceGeneree =
                    $"{compteur.Prefixe}-{compteur.DernierNumero}";


                // =================================================
                // CRÉATION DU PROJET
                // =================================================

                var projet =
                    new Projet
                    {
                        TicketId =
                            model.TicketId,

                        Reference =
                            referenceGeneree,

                        Nom =
                            model.Nom,

                        Description =
                            model.Description ??
                            string.Empty,

                        UniteProjetId =
                            model.UniteProjetId,

                        DepartementProjetId =
                            model.DepartementProjetId,

                        TypeProjetReferenceId =
                            model.TypeProjetReferenceId,

                        PlateformeProjetId =
                            model.PlateformeProjetId,

                        Priorite =
                            model.Priorite,

                        // OBLIGATOIRE
                        DateDebut =
                            model.DateDebut!.Value,

                        // OPTIONNELLE
                        DateFin =
                            model.DateFin,

                        // OPTIONNELLE
                        Deadline =
                            model.Deadline,

                        OwnerItId =
                            model.OwnerItId,

                        PowerUserId =
                            model.PowerUserId,

                        Statut =
                            StatutProjet.WaitingRFC
                    };


                _context.Projets.Add(
                    projet);


                await _context.SaveChangesAsync();


                // =================================================
                // NOTIFICATION POWER USER
                // =================================================

                if (projet.PowerUserId.HasValue)
                {
                    await _notificationService
                        .CreerNotificationAsync(
                            projet.Id,
                            projet.PowerUserId.Value,
                            "ValidationRequise",
                            $"Vous avez été assigné comme Power User sur le projet \"{projet.Nom}\". " +
                            "Merci de le valider pour permettre le démarrage du développement."
                        );
                }


                // =================================================
                // HISTORIQUE
                // =================================================

                var nomAD =
                    User.Identity?.Name;


                var auteur =
                    await _context.Utilisateurs
                        .FirstOrDefaultAsync(
                            u =>
                                u.NomADUtilisateur ==
                                nomAD);


                var detailDates =
                    $"Début : {projet.DateDebut:dd/MM/yyyy}";


                // =================================================
                // DATE DE FIN PRÉVUE
                // =================================================

                if (projet.DateFin.HasValue)
                {
                    detailDates +=
                        $" | Fin prévue : {projet.DateFin.Value:dd/MM/yyyy}";
                }
                else
                {
                    detailDates +=
                        " | Fin prévue : Non définie";
                }


                // =================================================
                // DEADLINE
                // =================================================

                if (projet.Deadline.HasValue)
                {
                    detailDates +=
                        $" | Deadline : {projet.Deadline.Value:dd/MM/yyyy}";
                }
                else
                {
                    detailDates +=
                        " | Deadline : Non définie";
                }


                _context.HistoriqueProjets.Add(
                    new HistoriqueProjet
                    {
                        ProjetId =
                            projet.Id,

                        UtilisateurId =
                            auteur?.Id ?? 0,

                        TypeAction =
                            "Création",

                        Detail =
                            $"Projet créé avec le statut {projet.Statut}, " +
                            $"la référence {projet.Reference}. " +
                            detailDates,

                        DateAction =
                            DateTime.Now
                    });


                await _context.SaveChangesAsync();


                // =================================================
                // VALIDATION TRANSACTION
                // =================================================

                await transaction.CommitAsync();


                // =================================================
                // MESSAGE DE SUCCÈS
                // =================================================

                TempData["Succes"] =
                    $"Le projet \"{projet.Nom}\" a été créé avec succès. " +
                    $"Référence : {projet.Reference}";


                return RedirectToAction(
                    "Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();


                TempData["Erreur"] =
                    "ERREUR DEBUG : " +
                    ex.Message +
                    " | INNER: " +
                    (ex.InnerException?.Message ??
                     "aucune");


                return RedirectToAction(
                    "Create");
            }
        }

        // =========================================================
        // CHARGER DONNÉES CREATE
        // =========================================================

        private async Task ChargerDonneesFormulaireCreate(
            ProjetCreateViewModel model)
        {
            model.UtilisateursDisponibles =
                await _context.Utilisateurs
                    .Where(u =>
                        u.EstActif)
                    .OrderBy(u =>
                        u.Nom)
                    .ToListAsync();


            model.UnitesDisponibles =
                await _context.UnitesProjets
                    .Where(u =>
                        u.Actif)
                    .OrderBy(u =>
                        u.Nom)
                    .ToListAsync();


            model.DepartementsDisponibles =
                await _context.DepartementsProjets
                    .Where(d =>
                        d.Actif)
                    .OrderBy(d =>
                        d.Nom)
                    .ToListAsync();


            model.TypesDisponibles =
                await _context.TypesProjets
                    .Where(t =>
                        t.Actif)
                    .OrderBy(t =>
                        t.Nom)
                    .ToListAsync();


            model.PlateformesDisponibles =
                await _context.PlateformesProjets
                    .Where(p =>
                        p.Actif)
                    .OrderBy(p =>
                        p.Nom)
                    .ToListAsync();
        }


        // =========================================================
        // AJOUTER UNITÉ
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjouterUnite(
            string nom,
            string prefixe)
        {
            nom =
                nom?.Trim() ??
                string.Empty;

            prefixe =
                prefixe?.Trim() ??
                string.Empty;


            if (string.IsNullOrWhiteSpace(nom))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Le nom de l'unité est obligatoire."
                });
            }


            if (string.IsNullOrWhiteSpace(prefixe))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Le préfixe de l'unité est obligatoire."
                });
            }


            var existe =
                await _context.UnitesProjets
                    .AnyAsync(u =>
                        u.Nom.ToLower() ==
                        nom.ToLower());


            if (existe)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "Cette unité existe déjà."
                });
            }


            var nouvelleUnite =
                new UniteProjet
                {
                    Nom =
                        nom,

                    Prefixe =
                        prefixe,

                    Actif =
                        true
                };


            _context.UnitesProjets.Add(
                nouvelleUnite);


            await _context.SaveChangesAsync();


            return Json(new
            {
                success = true,
                id = nouvelleUnite.Id,
                nom = nouvelleUnite.Nom,
                prefixe = nouvelleUnite.Prefixe
            });
        }


        // =========================================================
        // AJOUTER DÉPARTEMENT
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjouterDepartement(
            string nom)
        {
            nom =
                nom?.Trim() ??
                string.Empty;


            if (string.IsNullOrWhiteSpace(nom))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Le nom du département est obligatoire."
                });
            }


            var existe =
                await _context.DepartementsProjets
                    .AnyAsync(d =>
                        d.Nom.ToLower() ==
                        nom.ToLower());


            if (existe)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "Ce département existe déjà."
                });
            }


            var nouveauDepartement =
                new DepartementProjet
                {
                    Nom =
                        nom,

                    Actif =
                        true
                };


            _context.DepartementsProjets.Add(
                nouveauDepartement);


            await _context.SaveChangesAsync();


            return Json(new
            {
                success = true,
                id = nouveauDepartement.Id,
                nom = nouveauDepartement.Nom
            });
        }


        // =========================================================
        // AJOUTER TYPE
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjouterTypeProjet(
            string nom)
        {
            nom =
                nom?.Trim() ??
                string.Empty;


            if (string.IsNullOrWhiteSpace(nom))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Le nom du type est obligatoire."
                });
            }


            var existe =
                await _context.TypesProjets
                    .AnyAsync(t =>
                        t.Nom.ToLower() ==
                        nom.ToLower());


            if (existe)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "Ce type de projet existe déjà."
                });
            }


            var nouveauType =
                new TypeProjetReference
                {
                    Nom =
                        nom,

                    Actif =
                        true
                };


            _context.TypesProjets.Add(
                nouveauType);


            await _context.SaveChangesAsync();


            return Json(new
            {
                success = true,
                id = nouveauType.Id,
                nom = nouveauType.Nom
            });
        }


        // =========================================================
        // AJOUTER PLATEFORME
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjouterPlateforme(
            string nom)
        {
            nom =
                nom?.Trim() ??
                string.Empty;


            if (string.IsNullOrWhiteSpace(nom))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Le nom de la plateforme est obligatoire."
                });
            }


            var existe =
                await _context.PlateformesProjets
                    .AnyAsync(p =>
                        p.Nom.ToLower() ==
                        nom.ToLower());


            if (existe)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "Cette plateforme existe déjà."
                });
            }


            var nouvellePlateforme =
                new PlateformeProjet
                {
                    Nom =
                        nom,

                    Actif =
                        true
                };


            _context.PlateformesProjets.Add(
                nouvellePlateforme);


            await _context.SaveChangesAsync();


            return Json(new
            {
                success = true,
                id = nouvellePlateforme.Id,
                nom = nouvellePlateforme.Nom
            });
        }


        // =========================================================
        // AJOUTER UTILISATEUR
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjouterUtilisateur(
            string nomADUtilisateur,
            string nom,
            string email)
        {
            nomADUtilisateur =
                nomADUtilisateur?.Trim() ??
                string.Empty;

            nom =
                nom?.Trim() ??
                string.Empty;

            email =
                email?.Trim() ??
                string.Empty;


            if (string.IsNullOrWhiteSpace(
                nomADUtilisateur))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Le nom AD utilisateur est obligatoire."
                });
            }


            if (string.IsNullOrWhiteSpace(nom))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Le nom complet est obligatoire."
                });
            }


            var existe =
                await _context.Utilisateurs
                    .AnyAsync(u =>
                        u.NomADUtilisateur.ToLower() ==
                        nomADUtilisateur.ToLower());


            if (existe)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "Cet utilisateur existe déjà."
                });
            }


            var nouvelUtilisateur =
                new Utilisateur
                {
                    NomADUtilisateur =
                        nomADUtilisateur,

                    Nom =
                        nom,

                    Email =
                        email,

                    Role =
                        RoleUtilisateur.Lecteur
                };


            _context.Utilisateurs.Add(
                nouvelUtilisateur);


            await _context.SaveChangesAsync();


            return Json(new
            {
                success = true,
                id = nouvelUtilisateur.Id,
                nom = nouvelUtilisateur.Nom,
                nomADUtilisateur =
                    nouvelUtilisateur.NomADUtilisateur,
                email =
                    nouvelUtilisateur.Email
            });
        }


        // =========================================================
        // SUPPRIMER UNITÉ
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SupprimerUnite(
            int id)
        {
            var unite =
                await _context.UnitesProjets
                    .FirstOrDefaultAsync(
                        u => u.Id == id);


            if (unite == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "Unité introuvable."
                });
            }


            var utilisee =
                await _context.Projets
                    .AnyAsync(
                        p =>
                            p.UniteProjetId ==
                            id);


            if (utilisee)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "Cette unité est utilisée par un ou plusieurs projets."
                });
            }


            var compteur =
                await _context.ReferenceCompteurs
                    .FirstOrDefaultAsync(
                        r =>
                            r.UniteProjetId ==
                            id);


            if (compteur != null)
            {
                _context.ReferenceCompteurs.Remove(
                    compteur);
            }


            _context.UnitesProjets.Remove(
                unite);


            await _context.SaveChangesAsync();


            return Json(new
            {
                success = true,
                message =
                    "L'unité a été supprimée."
            });
        }


        // =========================================================
        // SUPPRIMER DÉPARTEMENT
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SupprimerDepartement(
            int id)
        {
            var departement =
                await _context.DepartementsProjets
                    .FirstOrDefaultAsync(
                        d => d.Id == id);


            if (departement == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "Département introuvable."
                });
            }


            var utilise =
                await _context.Projets
                    .AnyAsync(
                        p =>
                            p.DepartementProjetId ==
                            id);


            if (utilise)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "Ce département est utilisé par un ou plusieurs projets."
                });
            }


            _context.DepartementsProjets.Remove(
                departement);


            await _context.SaveChangesAsync();


            return Json(new
            {
                success = true,
                message =
                    "Le département a été supprimé."
            });
        }


        // =========================================================
        // SUPPRIMER TYPE
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SupprimerTypeProjet(
            int id)
        {
            var type =
                await _context.TypesProjets
                    .FirstOrDefaultAsync(
                        t => t.Id == id);


            if (type == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "Type de projet introuvable."
                });
            }


            var utilise =
                await _context.Projets
                    .AnyAsync(
                        p =>
                            p.TypeProjetReferenceId ==
                            id);


            if (utilise)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "Ce type est utilisé par un ou plusieurs projets."
                });
            }


            _context.TypesProjets.Remove(
                type);


            await _context.SaveChangesAsync();


            return Json(new
            {
                success = true,
                message =
                    "Le type de projet a été supprimé."
            });
        }


        // =========================================================
        // SUPPRIMER PLATEFORME
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SupprimerPlateforme(
            int id)
        {
            var plateforme =
                await _context.PlateformesProjets
                    .FirstOrDefaultAsync(
                        p => p.Id == id);


            if (plateforme == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "Plateforme introuvable."
                });
            }


            var utilisee =
                await _context.Projets
                    .AnyAsync(
                        p =>
                            p.PlateformeProjetId ==
                            id);


            if (utilisee)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "Cette plateforme est utilisée par un ou plusieurs projets."
                });
            }


            _context.PlateformesProjets.Remove(
                plateforme);


            await _context.SaveChangesAsync();


            return Json(new
            {
                success = true,
                message =
                    "La plateforme a été supprimée."
            });
        }


        // =========================================================
        // SUPPRIMER UTILISATEUR
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SupprimerUtilisateur(
            int id)
        {
            var utilisateur =
                await _context.Utilisateurs
                    .FirstOrDefaultAsync(
                        u => u.Id == id);


            if (utilisateur == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "Utilisateur introuvable."
                });
            }


            var utiliseCommeOwner =
                await _context.Projets
                    .AnyAsync(
                        p =>
                            p.OwnerItId ==
                            id);


            var utiliseCommePowerUser =
                await _context.Projets
                    .AnyAsync(
                        p =>
                            p.PowerUserId ==
                            id);


            if (utiliseCommeOwner ||
                utiliseCommePowerUser)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "Cet utilisateur est encore associé à un ou plusieurs projets comme Owner IT ou Power User."
                });
            }


            _context.Utilisateurs.Remove(
                utilisateur);


            await _context.SaveChangesAsync();


            return Json(new
            {
                success = true,
                message =
                    "L'utilisateur a été supprimé."
            });
        }


        // =========================================================
        // EDIT - GET
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpGet]
        public async Task<IActionResult> Edit(
            int id)
        {
            var projet =
                await _context.Projets
                    .FirstOrDefaultAsync(
                        p => p.Id == id);

            if (projet == null)
            {
                return NotFound();
            }

            var model =
                new ProjetEditViewModel
                {
                    Id =
                        projet.Id,

                    TicketId =
                        projet.TicketId,

                    Reference =
                        projet.Reference,

                    Nom =
                        projet.Nom,

                    Description =
                        projet.Description,

                    UniteProjetId =
                        projet.UniteProjetId ??
                        0,

                    DepartementProjetId =
                        projet.DepartementProjetId ??
                        0,

                    TypeProjetReferenceId =
                        projet.TypeProjetReferenceId ??
                        0,

                    PlateformeProjetId =
                        projet.PlateformeProjetId ??
                        0,

                    Statut =
                        projet.Statut,

                    Priorite =
                        projet.Priorite,

                    // =================================================
                    // DATES
                    // =================================================

                    DateDebut =
                        projet.DateDebut,

                    DateFin =
                        projet.DateFin,

                    Deadline =
                        projet.Deadline,

                    // =================================================
                    // AVANCEMENT
                    // =================================================

                    PourcentageAvancement =
                        projet.PourcentageAvancement,

                    // =================================================
                    // UTILISATEURS
                    // =================================================

                    OwnerItId =
                        projet.OwnerItId,

                    PowerUserId =
                        projet.PowerUserId,

                    // =================================================
                    // UTILISATEURS DISPONIBLES
                    // =================================================

                    UtilisateursDisponibles =
                        await _context.Utilisateurs
                            .Where(u =>
                                u.EstActif)
                            .OrderBy(u =>
                                u.Nom)
                            .ToListAsync(),

                    // =================================================
                    // UNITÉS
                    // =================================================

                    UnitesDisponibles =
                        await _context.UnitesProjets
                            .Where(u =>
                                u.Actif)
                            .OrderBy(u =>
                                u.Nom)
                            .ToListAsync(),

                    // =================================================
                    // DÉPARTEMENTS
                    // =================================================

                    DepartementsDisponibles =
                        await _context.DepartementsProjets
                            .Where(d =>
                                d.Actif)
                            .OrderBy(d =>
                                d.Nom)
                            .ToListAsync(),

                    // =================================================
                    // TYPES
                    // =================================================

                    TypesDisponibles =
                        await _context.TypesProjets
                            .Where(t =>
                                t.Actif)
                            .OrderBy(t =>
                                t.Nom)
                            .ToListAsync(),

                    // =================================================
                    // PLATEFORMES
                    // =================================================

                    PlateformesDisponibles =
                        await _context.PlateformesProjets
                            .Where(p =>
                                p.Actif)
                            .OrderBy(p =>
                                p.Nom)
                            .ToListAsync()
                };

            return View(model);
        }

        // =========================================================
        // EDIT - POST
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> Edit(
    ProjetEditViewModel model)
        {

            // =====================================================
            // DATE DEBUT - OBLIGATOIRE
            // =====================================================

            if (!model.DateDebut.HasValue)
            {
                ModelState.AddModelError(
                    "DateDebut",
                    "La date de début est obligatoire.");
            }

            if (model.DateDebut.HasValue &&
                model.DateFin.HasValue &&
                model.DateFin.Value.Date <
                model.DateDebut.Value.Date)
            {
                ModelState.AddModelError(
                    "DateFin",
                    "La date de fin doit être postérieure ou égale à la date de début.");
            }

            if (model.DateDebut.HasValue &&
                model.Deadline.HasValue &&
                model.Deadline.Value.Date <
                model.DateDebut.Value.Date)
            {
                ModelState.AddModelError(
                    "Deadline",
                    "La deadline doit être postérieure ou égale à la date de début.");
            }

            // =====================================================
            // UNITÉ
            // =====================================================

            var unite =
                await _context.UnitesProjets
                    .FirstOrDefaultAsync(u =>
                        u.Id == model.UniteProjetId &&
                        u.Actif);

            if (unite == null)
            {
                ModelState.AddModelError(
                    "UniteProjetId",
                    "L'unité sélectionnée est invalide.");
            }


            // =====================================================
            // DÉPARTEMENT
            // =====================================================

            var departement =
                await _context.DepartementsProjets
                    .FirstOrDefaultAsync(d =>
                        d.Id == model.DepartementProjetId &&
                        d.Actif);

            if (departement == null)
            {
                ModelState.AddModelError(
                    "DepartementProjetId",
                    "Le département sélectionné est invalide.");
            }


            // =====================================================
            // TYPE
            // =====================================================

            var typeProjet =
                await _context.TypesProjets
                    .FirstOrDefaultAsync(t =>
                        t.Id == model.TypeProjetReferenceId &&
                        t.Actif);

            if (typeProjet == null)
            {
                ModelState.AddModelError(
                    "TypeProjetReferenceId",
                    "Le type de projet sélectionné est invalide.");
            }


            // =====================================================
            // PLATEFORME
            // =====================================================

            var plateforme =
                await _context.PlateformesProjets
                    .FirstOrDefaultAsync(p =>
                        p.Id == model.PlateformeProjetId &&
                        p.Actif);

            if (plateforme == null)
            {
                ModelState.AddModelError(
                    "PlateformeProjetId",
                    "La plateforme sélectionnée est invalide.");
            }


            // =====================================================
            // RETOUR FORMULAIRE SI ERREUR
            // =====================================================

            if (!ModelState.IsValid)
            {
                await ChargerDonneesFormulaireEdit(model);

                return View(model);
            }


            // =====================================================
            // PROJET
            // =====================================================

            var projet =
                await _context.Projets
                    .FirstOrDefaultAsync(
                        p => p.Id == model.Id);

            if (projet == null)
            {
                return NotFound();
            }


            // =====================================================
            // ANCIENNES VALEURS
            // =====================================================

            var ancienTicketId =
                projet.TicketId;

            var ancienNom =
                projet.Nom;

            var ancienneDescription =
                projet.Description;

            var ancienneUniteProjetId =
                projet.UniteProjetId;

            var ancienDepartementProjetId =
                projet.DepartementProjetId;

            var ancienTypeProjetReferenceId =
                projet.TypeProjetReferenceId;

            var anciennePlateformeProjetId =
                projet.PlateformeProjetId;

            var ancienStatut =
                projet.Statut;

            var anciennePriorite =
                projet.Priorite;

            var ancienneDateDebut =
                projet.DateDebut;

            var ancienneDateFin =
                projet.DateFin;

            var ancienneDeadline =
                projet.Deadline;

            var ancienPourcentage =
                projet.PourcentageAvancement;

            var ancienOwnerItId =
                projet.OwnerItId;

            var ancienPowerUserId =
                projet.PowerUserId;


            // =====================================================
            // DÉTECTION DES CHANGEMENTS
            // =====================================================

            var statutChange =
                ancienStatut != model.Statut;

            var powerUserChange =
                ancienPowerUserId != model.PowerUserId;


            var nouvelAvancement =
                ObtenirAvancementAutomatique(
                    model.Statut,
                    ancienPourcentage);


            var modifications =
                new List<string>();


            // =====================================================
            // TICKET ID
            // =====================================================

            if (ancienTicketId != model.TicketId)
            {
                modifications.Add(
                    $"Ticket ID : {ancienTicketId} → {model.TicketId}");
            }


            // =====================================================
            // NOM
            // =====================================================

            if (ancienNom != model.Nom)
            {
                modifications.Add(
                    $"Nom : {ancienNom} → {model.Nom}");
            }


            // =====================================================
            // DESCRIPTION
            // =====================================================

            if (ancienneDescription !=
                (model.Description ?? string.Empty))
            {
                modifications.Add(
                    "Description modifiée");
            }


            // =====================================================
            // UNITÉ
            // =====================================================

            if (ancienneUniteProjetId !=
                model.UniteProjetId)
            {
                var ancienneUnite =
                    await _context.UnitesProjets
                        .Where(u =>
                            u.Id == ancienneUniteProjetId)
                        .Select(u =>
                            u.Nom)
                        .FirstOrDefaultAsync();

                var nouvelleUnite =
                    await _context.UnitesProjets
                        .Where(u =>
                            u.Id == model.UniteProjetId)
                        .Select(u =>
                            u.Nom)
                        .FirstOrDefaultAsync();

                modifications.Add(
                    $"Unité : {ancienneUnite ?? "Non définie"} → " +
                    $"{nouvelleUnite ?? "Non définie"}");
            }


            // =====================================================
            // DÉPARTEMENT
            // =====================================================

            if (ancienDepartementProjetId !=
                model.DepartementProjetId)
            {
                var ancienDepartement =
                    await _context.DepartementsProjets
                        .Where(d =>
                            d.Id == ancienDepartementProjetId)
                        .Select(d =>
                            d.Nom)
                        .FirstOrDefaultAsync();

                var nouveauDepartement =
                    await _context.DepartementsProjets
                        .Where(d =>
                            d.Id == model.DepartementProjetId)
                        .Select(d =>
                            d.Nom)
                        .FirstOrDefaultAsync();

                modifications.Add(
                    $"Département : {ancienDepartement ?? "Non défini"} → " +
                    $"{nouveauDepartement ?? "Non défini"}");
            }


            // =====================================================
            // TYPE
            // =====================================================

            if (ancienTypeProjetReferenceId !=
                model.TypeProjetReferenceId)
            {
                var ancienTypeNom =
                    await _context.TypesProjets
                        .Where(t =>
                            t.Id == ancienTypeProjetReferenceId)
                        .Select(t =>
                            t.Nom)
                        .FirstOrDefaultAsync();

                var nouveauTypeNom =
                    await _context.TypesProjets
                        .Where(t =>
                            t.Id == model.TypeProjetReferenceId)
                        .Select(t =>
                            t.Nom)
                        .FirstOrDefaultAsync();

                modifications.Add(
                    $"Type : {ancienTypeNom ?? "Non défini"} → " +
                    $"{nouveauTypeNom ?? "Non défini"}");
            }


            // =====================================================
            // PLATEFORME
            // =====================================================

            if (anciennePlateformeProjetId !=
                model.PlateformeProjetId)
            {
                var anciennePlateforme =
                    await _context.PlateformesProjets
                        .Where(p =>
                            p.Id == anciennePlateformeProjetId)
                        .Select(p =>
                            p.Nom)
                        .FirstOrDefaultAsync();

                var nouvellePlateforme =
                    await _context.PlateformesProjets
                        .Where(p =>
                            p.Id == model.PlateformeProjetId)
                        .Select(p =>
                            p.Nom)
                        .FirstOrDefaultAsync();

                modifications.Add(
                    $"Plateforme : {anciennePlateforme ?? "Non définie"} → " +
                    $"{nouvellePlateforme ?? "Non définie"}");
            }


            // =====================================================
            // STATUT
            // =====================================================

            if (ancienStatut != model.Statut)
            {
                modifications.Add(
                    $"Statut : {ancienStatut} → {model.Statut}");
            }


            // =====================================================
            // PRIORITÉ
            // =====================================================

            if (anciennePriorite != model.Priorite)
            {
                modifications.Add(
                    $"Priorité : {anciennePriorite} → {model.Priorite}");
            }


            // =====================================================
            // DATE DEBUT
            // =====================================================

            if (ancienneDateDebut != model.DateDebut)
            {
                modifications.Add(
                    $"Date de début : " +
                    $"{ancienneDateDebut:dd/MM/yyyy} → " +
                    $"{model.DateDebut:dd/MM/yyyy}");
            }


            // =====================================================
            // DATE FIN
            // =====================================================

            if (ancienneDateFin != model.DateFin)
            {
                var ancienneDate =
                    ancienneDateFin.HasValue
                        ? ancienneDateFin.Value
                            .ToString("dd/MM/yyyy")
                        : "Non définie";

                var nouvelleDate =
                    model.DateFin.HasValue
                        ? model.DateFin.Value
                            .ToString("dd/MM/yyyy")
                        : "Non définie";

                modifications.Add(
                    $"Fin prévue : {ancienneDate} → {nouvelleDate}");
            }


            // =====================================================
            // DEADLINE
            // =====================================================

            if (ancienneDeadline != model.Deadline)
            {
                var ancienneDate =
                    ancienneDeadline.HasValue
                        ? ancienneDeadline.Value
                            .ToString("dd/MM/yyyy")
                        : "Non définie";

                var nouvelleDate =
                    model.Deadline.HasValue
                        ? model.Deadline.Value
                            .ToString("dd/MM/yyyy")
                        : "Non définie";

                modifications.Add(
                    $"Deadline : {ancienneDate} → {nouvelleDate}");
            }


            // =====================================================
            // AVANCEMENT
            // =====================================================

            if (ancienPourcentage != nouvelAvancement)
            {
                modifications.Add(
                    $"Avancement : {ancienPourcentage}% → {nouvelAvancement}%");
            }


            // =====================================================
            // OWNER IT
            // =====================================================

            if (ancienOwnerItId != model.OwnerItId)
            {
                var ancienOwner =
                    ancienOwnerItId.HasValue
                        ? (await _context.Utilisateurs
                            .FindAsync(
                                ancienOwnerItId.Value))?.Nom
                        : "Non assigné";

                var nouvelOwner =
                    model.OwnerItId.HasValue
                        ? (await _context.Utilisateurs
                            .FindAsync(
                                model.OwnerItId.Value))?.Nom
                        : "Non assigné";

                modifications.Add(
                    $"Owner IT : {ancienOwner ?? "Non assigné"} → " +
                    $"{nouvelOwner ?? "Non assigné"}");
            }


            // =====================================================
            // POWER USER
            // =====================================================

            if (ancienPowerUserId != model.PowerUserId)
            {
                var ancienPowerUser =
                    ancienPowerUserId.HasValue
                        ? (await _context.Utilisateurs
                            .FindAsync(
                                ancienPowerUserId.Value))?.Nom
                        : "Non assigné";

                var nouveauPowerUser =
                    model.PowerUserId.HasValue
                        ? (await _context.Utilisateurs
                            .FindAsync(
                                model.PowerUserId.Value))?.Nom
                        : "Non assigné";

                modifications.Add(
                    $"Power User : {ancienPowerUser ?? "Non assigné"} → " +
                    $"{nouveauPowerUser ?? "Non assigné"}");
            }


            // =====================================================
            // MISE À JOUR
            // =====================================================

            projet.TicketId =
                model.TicketId;

            projet.Nom =
                model.Nom;

            projet.Description =
                model.Description ??
                string.Empty;

            projet.UniteProjetId =
                model.UniteProjetId;

            projet.DepartementProjetId =
                model.DepartementProjetId;

            projet.TypeProjetReferenceId =
                model.TypeProjetReferenceId;

            projet.PlateformeProjetId =
                model.PlateformeProjetId;

            projet.StatutPrecedent =
                ancienStatut;

            projet.Statut =
                model.Statut;

            projet.Priorite =
                model.Priorite;


            // =====================================================
            // DATES
            // =====================================================

            projet.DateDebut =
                model.DateDebut;

            projet.DateFin =
                model.DateFin;

            projet.Deadline =
                model.Deadline;


            // =====================================================
            // AVANCEMENT
            // =====================================================

            projet.PourcentageAvancement =
                nouvelAvancement;


            // =====================================================
            // UTILISATEURS
            // =====================================================

            projet.OwnerItId =
                model.OwnerItId;

            projet.PowerUserId =
                model.PowerUserId;


            // =====================================================
            // CHANGEMENT POWER USER
            // =====================================================

            if (powerUserChange)
            {
                projet.ValidePowerUser =
                    false;

                projet.DateValidationPowerUser =
                    null;
            }


            // =====================================================
            // HISTORIQUE
            // =====================================================

            if (modifications.Any())
            {
                var nomAD =
                    User.Identity?.Name;

                var auteur =
                    await _context.Utilisateurs
                        .FirstOrDefaultAsync(
                            u =>
                                u.NomADUtilisateur ==
                                nomAD);

                _context.HistoriqueProjets.Add(
                    new HistoriqueProjet
                    {
                        ProjetId =
                            projet.Id,

                        UtilisateurId =
                            auteur?.Id ?? 0,

                        TypeAction =
                            "Modification",

                        Detail =
                            string.Join(
                                Environment.NewLine,
                                modifications),

                        DateAction =
                            DateTime.Now
                    });
            }


            // =====================================================
            // SAUVEGARDE
            // =====================================================

            await _context.SaveChangesAsync();


            // =====================================================
            // NOTIFICATION STATUT
            // =====================================================

            if (statutChange &&
                projet.OwnerItId.HasValue)
            {
                await _notificationService
                    .CreerNotificationAsync(
                        projet.Id,
                        projet.OwnerItId.Value,
                        "ChangementStatut",
                        $"Le projet \"{projet.Nom}\" est passé de " +
                        $"{ancienStatut} à {projet.Statut}."
                    );
            }


            // =====================================================
            // NOTIFICATION POWER USER
            // =====================================================

            if (powerUserChange &&
                projet.PowerUserId.HasValue)
            {
                await _notificationService
                    .CreerNotificationAsync(
                        projet.Id,
                        projet.PowerUserId.Value,
                        "ValidationRequise",
                        $"Vous avez été assigné comme Power User sur le projet \"{projet.Nom}\". " +
                        "Merci de le valider pour permettre le démarrage du développement."
                    );
            }


            // =====================================================
            // MESSAGE DE SUCCÈS
            // =====================================================

            TempData["Succes"] =
                $"Le projet \"{projet.Nom}\" a été modifié avec succès.";


            // =====================================================
            // REDIRECTION
            // =====================================================

            return RedirectToAction(
                "Index");
        }


        // =========================================================
        // CHARGER DONNÉES EDIT
        // =========================================================

        private async Task ChargerDonneesFormulaireEdit(
            ProjetEditViewModel model)
        {
            model.UtilisateursDisponibles =
                await _context.Utilisateurs
                    .Where(u =>
                        u.EstActif)
                    .OrderBy(u =>
                        u.Nom)
                    .ToListAsync();


            model.UnitesDisponibles =
                await _context.UnitesProjets
                    .Where(u =>
                        u.Actif)
                    .OrderBy(u =>
                        u.Nom)
                    .ToListAsync();


            model.DepartementsDisponibles =
                await _context.DepartementsProjets
                    .Where(d =>
                        d.Actif)
                    .OrderBy(d =>
                        d.Nom)
                    .ToListAsync();


            model.TypesDisponibles =
                await _context.TypesProjets
                    .Where(t =>
                        t.Actif)
                    .OrderBy(t =>
                        t.Nom)
                    .ToListAsync();


            model.PlateformesDisponibles =
                await _context.PlateformesProjets
                    .Where(p =>
                        p.Actif)
                    .OrderBy(p =>
                        p.Nom)
                    .ToListAsync();
        }


        // =========================================================
        // CHANGER STATUT - GET
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpGet]
        public async Task<IActionResult> ChangerStatut(
            int id)
        {
            var projet =
                await _context.Projets
                    .Include(p =>
                        p.TypeProjetReference)
                    .FirstOrDefaultAsync(
                        p => p.Id == id);


            if (projet == null)
            {
                return NotFound();
            }


            if (projet.TypeProjetReference == null)
            {
                TempData["Erreur"] =
                    "Ce projet n'a pas de Type défini. Modifiez-le pour en assigner un avant de changer son statut.";
            }


            ViewBag.TransitionsPossibles =
                _workflowService.GetTransitionsPossibles(
                    projet.Statut,
                    projet.StatutPrecedent,
                    projet.TypeProjetReference);


            return View(projet);
        }


        // =========================================================
        // CHANGER STATUT - POST
        // =========================================================

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> ChangerStatut(
            int id,
            StatutProjet nouveauStatut)
        {
            var projet =
                await _context.Projets
                    .Include(p =>
                        p.OwnerIt)
                    .Include(p =>
                        p.TypeProjetReference)
                    .FirstOrDefaultAsync(
                        p => p.Id == id);


            if (projet == null)
            {
                return NotFound();
            }


            if (projet.TypeProjetReference == null)
            {
                TempData["Erreur"] =
                    "Ce projet n'a pas de Type défini. Modifiez-le pour en assigner un avant de changer son statut.";


                return RedirectToAction(
                    "ChangerStatut",
                    new
                    {
                        id
                    });
            }


            var transitionsAutorisees =
                _workflowService.GetTransitionsPossibles(
                    projet.Statut,
                    projet.StatutPrecedent,
                    projet.TypeProjetReference);


            if (!transitionsAutorisees.Contains(
                nouveauStatut))
            {
                TempData["Erreur"] =
                    "Transition de statut non autorisée.";


                return RedirectToAction(
                    "ChangerStatut",
                    new
                    {
                        id
                    });
            }


            if (nouveauStatut ==
                    StatutProjet.DevStarted &&
                !projet.ValidePowerUser)
            {
                TempData["Erreur"] =
                    "Le Power User doit valider le projet avant de démarrer le développement.";


                return RedirectToAction(
                    "ChangerStatut",
                    new
                    {
                        id
                    });
            }


            var ancienStatut =
                projet.Statut;


            var nomAD =
                User.Identity?.Name;


            var auteur =
                await _context.Utilisateurs
                    .FirstOrDefaultAsync(
                        u =>
                            u.NomADUtilisateur ==
                            nomAD);


            _context.HistoriqueProjets.Add(
                new HistoriqueProjet
                {
                    ProjetId =
                        projet.Id,

                    UtilisateurId =
                        auteur?.Id ?? 0,

                    TypeAction =
                        "Changement de statut",

                    Detail =
                        $"{ancienStatut} → {nouveauStatut}",

                    DateAction =
                        DateTime.Now
                });


            projet.StatutPrecedent =
                ancienStatut;


            projet.Statut =
                nouveauStatut;


            projet.PourcentageAvancement =
                ObtenirAvancementAutomatique(
                    nouveauStatut,
                    projet.PourcentageAvancement);


            await _context.SaveChangesAsync();


            if (projet.OwnerItId.HasValue)
            {
                await _notificationService
                    .CreerNotificationAsync(
                        projet.Id,
                        projet.OwnerItId.Value,
                        "ChangementStatut",
                        $"Le projet \"{projet.Nom}\" est passé de " +
                        $"{ancienStatut} à {nouveauStatut}."
                    );
            }


            TempData["Succes"] =
                $"Statut changé vers {nouveauStatut}.";


            return RedirectToAction(
                "Index");
        }


        // =========================================================
        // VALIDATION POWER USER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValiderPowerUser(
            int id)
        {
            var projet =
                await _context.Projets
                    .Include(p =>
                        p.PowerUser)
                    .FirstOrDefaultAsync(
                        p => p.Id == id);


            if (projet == null)
            {
                return NotFound();
            }


            var nomAD =
                User.Identity?.Name;


            if (projet.PowerUser == null ||
                projet.PowerUser.NomADUtilisateur !=
                    nomAD)
            {
                TempData["Erreur"] =
                    "Seul le Power User assigné à ce projet peut le valider.";


                return RedirectToAction(
                    "Details",
                    new
                    {
                        id
                    });
            }


            projet.ValidePowerUser =
                true;


            projet.DateValidationPowerUser =
                DateTime.Now;


            var auteur =
                await _context.Utilisateurs
                    .FirstOrDefaultAsync(
                        u =>
                            u.NomADUtilisateur ==
                            nomAD);


            _context.HistoriqueProjets.Add(
                new HistoriqueProjet
                {
                    ProjetId =
                        projet.Id,

                    UtilisateurId =
                        auteur?.Id ?? 0,

                    TypeAction =
                        "Validation Power User",

                    Detail =
                        $"Le projet a été validé par le Power User {projet.PowerUser.Nom}.",

                    DateAction =
                        DateTime.Now
                });


            await _context.SaveChangesAsync();


            TempData["Succes"] =
                "Le projet a été validé avec succès.";


            return RedirectToAction(
                "Details",
                new
                {
                    id
                });
        }


        // =========================================================
        // DELETE - GET
        // =========================================================

        [Authorize(Roles = "Administrateur")]
        [HttpGet]
        public async Task<IActionResult> Delete(
            int id)
        {
            var projet =
                await _context.Projets
                    .Include(p =>
                        p.OwnerIt)
                    .Include(p =>
                        p.PowerUser)
                    .Include(p =>
                        p.UniteProjet)
                    .Include(p =>
                        p.DepartementProjet)
                    .Include(p =>
                        p.TypeProjetReference)
                    .Include(p =>
                        p.PlateformeProjet)
                    .FirstOrDefaultAsync(
                        p => p.Id == id);


            if (projet == null)
            {
                return NotFound();
            }


            return View(projet);
        }


        // =========================================================
        // DELETE - POST
        // =========================================================

        [Authorize(Roles = "Administrateur")]
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var projet =
                await _context.Projets
                    .FirstOrDefaultAsync(
                        p => p.Id == id);


            if (projet != null)
            {
                var notifications =
                    await _context.Notifications
                        .Where(n =>
                            n.ProjetId == id)
                        .ToListAsync();


                _context.Notifications.RemoveRange(
                    notifications);


                _context.Projets.Remove(
                    projet);


                await _context.SaveChangesAsync();
            }


            TempData["Succes"] =
                "Le projet a été supprimé.";


            return RedirectToAction(
                "Index");
        }
    }
}