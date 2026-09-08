using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionProjetSocota.Data;
using GestionProjetSocota.Models;
using GestionProjetSocota.ViewModels;
using Microsoft.AspNetCore.Authorization;
using GestionProjetSocota.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

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

        private static int ObtenirAvancementAutomatique(StatutProjet statut, int avancementActuel)
        {
            if (statut == StatutProjet.Suspendu || statut == StatutProjet.Cancelled)
            {
                return avancementActuel;
            }

            return AvancementParStatut.TryGetValue(statut, out var valeur)
                ? valeur
                : avancementActuel;
        }

        // Index
        public async Task<IActionResult> Index()
        {
            var projets = await _context.Projets
                .Include(p => p.OwnerIt)
                .Include(p => p.PowerUser)
                .Include(p => p.UniteProjet)
                .Include(p => p.DepartementProjet)
                .Include(p => p.TypeProjetReference)
                .Include(p => p.PlateformeProjet)
                .ToListAsync();

            var derniersCommentaires = await _context.Commentaires
                .GroupBy(c => c.ProjetId)
                .Select(g => new
                {
                    ProjetId = g.Key,
                    Date = g.Max(c => c.DatePublication)
                })
                .ToDictionaryAsync(
                    x => x.ProjetId,
                    x => (DateTime?)x.Date);

            var scores = new Dictionary<int, (int score, string couleur)>();

            foreach (var p in projets)
            {
                derniersCommentaires.TryGetValue(
                    p.Id,
                    out var dernierCommentaire);

                var score =
                    _scoreRisqueService.CalculerScore(
                        p,
                        dernierCommentaire);

                scores[p.Id] = (
                    score,
                    _scoreRisqueService.ObtenirCouleur(score));
            }

            ViewBag.Scores = scores;

            return View(projets);
        }

        // Recherche
        public async Task<IActionResult> Recherche(
            int? unite,
            int? departement,
            StatutProjet? statut,
            int? type,
            int? ownerItId)
        {
            var query = _context.Projets
                .Include(p => p.OwnerIt)
                .Include(p => p.PowerUser)
                .Include(p => p.UniteProjet)
                .Include(p => p.DepartementProjet)
                .Include(p => p.TypeProjetReference)
                .Include(p => p.PlateformeProjet)
                .AsQueryable();

            // Filtre Unité
            if (unite.HasValue)
            {
                query = query.Where(
                    p => p.UniteProjetId == unite.Value);
            }

            // Filtre Département
            if (departement.HasValue)
            {
                query = query.Where(
                    p => p.DepartementProjetId == departement.Value);
            }

            // Filtre Statut
            if (statut.HasValue)
            {
                query = query.Where(
                    p => p.Statut == statut.Value);
            }

            // Filtre Type
            if (type.HasValue)
            {
                query = query.Where(
                    p => p.TypeProjetReferenceId == type.Value);
            }

            // Filtre Owner IT
            if (ownerItId.HasValue)
            {
                query = query.Where(
                    p => p.OwnerItId == ownerItId.Value);
            }

            var projets = await query
                .OrderBy(p => p.Nom)
                .ToListAsync();

            // Données utilisées par Recherche.cshtml
            ViewBag.Utilisateurs =
                await _context.Utilisateurs
                    .OrderBy(u => u.Nom)
                    .ToListAsync();

            ViewBag.Unites =
                await _context.UnitesProjets
                    .Where(u => u.Actif)
                    .OrderBy(u => u.Nom)
                    .ToListAsync();

            ViewBag.Departements =
                await _context.DepartementsProjets
                    .Where(d => d.Actif)
                    .OrderBy(d => d.Nom)
                    .ToListAsync();

            ViewBag.Types =
                await _context.TypesProjets
                    .Where(t => t.Actif)
                    .OrderBy(t => t.Nom)
                    .ToListAsync();

            ViewBag.FiltresActifs = new
            {
                unite,
                departement,
                statut,
                type,
                ownerItId
            };

            return View(projets);
        }

        // Export du fichier Excel
        public async Task<IActionResult> ExporterExcel(
            int? unite,
            int? departement,
            StatutProjet? statut,
            int? type,
            int? ownerItId)
        {
            var query = _context.Projets
                .Include(p => p.OwnerIt)
                .Include(p => p.UniteProjet)
                .Include(p => p.DepartementProjet)
                .Include(p => p.TypeProjetReference)
                .Include(p => p.PlateformeProjet)
                .AsQueryable();

            if (unite.HasValue)
            {
                query = query.Where(
                    p => p.UniteProjetId == unite.Value);
            }

            if (departement.HasValue)
            {
                query = query.Where(
                    p => p.DepartementProjetId == departement.Value);
            }

            if (statut.HasValue)
            {
                query = query.Where(
                    p => p.Statut == statut.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(
                    p => p.TypeProjetReferenceId == type.Value);
            }

            if (ownerItId.HasValue)
            {
                query = query.Where(
                    p => p.OwnerItId == ownerItId.Value);
            }

            var projets = await query
                .OrderBy(p => p.Nom)
                .ToListAsync();

            using var workbook =
                new ClosedXML.Excel.XLWorkbook();

            var feuille =
                workbook.Worksheets.Add("Projets");

            feuille.Cell(1, 1).Value = "Ticket ID";
            feuille.Cell(1, 2).Value = "Référence";
            feuille.Cell(1, 3).Value = "Nom";
            feuille.Cell(1, 4).Value = "Unité";
            feuille.Cell(1, 5).Value = "Département";
            feuille.Cell(1, 6).Value = "Type";
            feuille.Cell(1, 7).Value = "Plateforme";
            feuille.Cell(1, 8).Value = "Statut";
            feuille.Cell(1, 9).Value = "Owner IT";

            feuille.Row(1).Style.Font.Bold = true;

            for (int i = 0; i < projets.Count; i++)
            {
                var p = projets[i];

                feuille.Cell(i + 2, 1).Value =
                    p.TicketId;

                feuille.Cell(i + 2, 2).Value =
                    p.Reference;

                feuille.Cell(i + 2, 3).Value =
                    p.Nom;

                feuille.Cell(i + 2, 4).Value =
                    p.UniteProjet?.Nom ?? "-";

                feuille.Cell(i + 2, 5).Value =
                    p.DepartementProjet?.Nom ?? "-";

                feuille.Cell(i + 2, 6).Value =
                    p.TypeProjetReference?.Nom ?? "-";

                feuille.Cell(i + 2, 7).Value =
                    p.PlateformeProjet?.Nom ?? "-";

                feuille.Cell(i + 2, 8).Value =
                    p.Statut.ToString();

                feuille.Cell(i + 2, 9).Value =
                    p.OwnerIt?.Nom ?? "-";
            }

            feuille.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "projets.xlsx");
        }

        // Pdf
        public async Task<IActionResult> ExporterPdf(
            int? unite,
            int? departement,
            StatutProjet? statut,
            int? type,
            int? ownerItId)
        {
            var query = _context.Projets
                .Include(p => p.OwnerIt)
                .Include(p => p.UniteProjet)
                .Include(p => p.DepartementProjet)
                .Include(p => p.TypeProjetReference)
                .AsQueryable();

            if (unite.HasValue)
            {
                query = query.Where(
                    p => p.UniteProjetId == unite.Value);
            }

            if (departement.HasValue)
            {
                query = query.Where(
                    p => p.DepartementProjetId == departement.Value);
            }

            if (statut.HasValue)
            {
                query = query.Where(
                    p => p.Statut == statut.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(
                    p => p.TypeProjetReferenceId == type.Value);
            }

            if (ownerItId.HasValue)
            {
                query = query.Where(
                    p => p.OwnerItId == ownerItId.Value);
            }

            var projets = await query
                .OrderBy(p => p.Nom)
                .ToListAsync();

            var document =
                QuestPDF.Fluent.Document.Create(conteneur =>
                {
                    conteneur.Page(page =>
                    {
                        page.Margin(30);

                        page.Header()
                            .Text("Liste des projets - GestionProjetSocota")
                            .FontSize(16)
                            .Bold()
                            .FontColor("#155D15");

                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(colonnes =>
                            {
                                colonnes.RelativeColumn();
                                colonnes.RelativeColumn(2);
                                colonnes.RelativeColumn();
                                colonnes.RelativeColumn();
                                colonnes.RelativeColumn();
                            });

                            table.Header(entete =>
                            {
                                entete.Cell().Text("Ticket ID").Bold();
                                entete.Cell().Text("Nom").Bold();
                                entete.Cell().Text("Unité").Bold();
                                entete.Cell().Text("Statut").Bold();
                                entete.Cell().Text("Owner IT").Bold();
                            });

                            foreach (var p in projets)
                            {
                                table.Cell().Text(
                                    p.TicketId);

                                table.Cell().Text(
                                    p.Nom);

                                table.Cell().Text(
                                    p.UniteProjet?.Nom ?? "-");

                                table.Cell().Text(
                                    p.Statut.ToString());

                                table.Cell().Text(
                                    p.OwnerIt?.Nom ?? "-");
                            }
                        });

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

            var pdfBytes =
                document.GeneratePdf();

            return File(
                pdfBytes,
                "application/pdf",
                "projets.pdf");
        }

        // Importer le fichier Excel
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
                TempData["Erreur"] = "Aucun fichier sélectionné.";
                return RedirectToAction("ImporterExcel");
            }

            var nomUtilisateur = User.Identity?.Name;

            var auteur = await _context.Utilisateurs
                .FirstOrDefaultAsync(
                    u => u.NomADUtilisateur == nomUtilisateur);

            if (auteur == null)
            {
                TempData["Erreur"] = "Utilisateur connecté introuvable.";
                return RedirectToAction("ImporterExcel");
            }

            int nbImportes = 0;
            int nbIgnores = 0;

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                using var stream = new MemoryStream();

                await fichier.CopyToAsync(stream);

                using var workbook =
                    new ClosedXML.Excel.XLWorkbook(stream);

                var feuille = workbook.Worksheet("Projets");

                var lignes = feuille.RowsUsed().Skip(1);

                foreach (var ligne in lignes)
                {
                    var uniteTexte =
                        ligne.Cell(1).GetString().Trim();

                    var ticketId =
                        ligne.Cell(2).GetString().Trim();

                    var nom =
                        ligne.Cell(4).GetString().Trim();

                    var departementTexte =
                        ligne.Cell(5).GetString().Trim();

                    var typeTexte =
                        ligne.Cell(6).GetString().Trim();

                    var plateformeTexte =
                        ligne.Cell(7).GetString().Trim();

                    var ownerItNom =
                        ligne.Cell(8).GetString().Trim();

                    var powerUserNom =
                        ligne.Cell(9).GetString().Trim();

                    var prioriteTexte =
                        ligne.Cell(10).GetString().Trim();

                    var statutTexte =
                        ligne.Cell(14)
                            .GetString()
                            .Trim()
                            .Replace(" ", "");

                    var dateDebutCell =
                        ligne.Cell(15);

                    var dateFinCell =
                        ligne.Cell(16);

                    var avancementCell =
                        ligne.Cell(17);

                    var commentaireExcel =
                        ligne.Cell(19).GetString().Trim();


                    // ==============================
                    // NOM
                    // ==============================

                    if (string.IsNullOrWhiteSpace(nom))
                    {
                        nbIgnores++;
                        continue;
                    }


                    // ==============================
                    // DATES
                    // ==============================

                    DateTime? dateDebut = null;

                    if (dateDebutCell.DataType ==
                        ClosedXML.Excel.XLDataType.DateTime)
                    {
                        dateDebut =
                            dateDebutCell.GetDateTime();
                    }
                    else if (!string.IsNullOrWhiteSpace(
                        dateDebutCell.GetString()))
                    {
                        if (DateTime.TryParse(
                            dateDebutCell.GetString(),
                            out var dateDebutParsee))
                        {
                            dateDebut = dateDebutParsee;
                        }
                    }


                    DateTime? dateFin = null;

                    if (dateFinCell.DataType ==
                        ClosedXML.Excel.XLDataType.DateTime)
                    {
                        dateFin =
                            dateFinCell.GetDateTime();
                    }
                    else if (!string.IsNullOrWhiteSpace(
                        dateFinCell.GetString()))
                    {
                        if (DateTime.TryParse(
                            dateFinCell.GetString(),
                            out var dateFinParsee))
                        {
                            dateFin = dateFinParsee;
                        }
                    }


                    if (dateDebut.HasValue &&
                        dateFin.HasValue &&
                        dateFin.Value < dateDebut.Value)
                    {
                        nbIgnores++;
                        continue;
                    }


                    // ==============================
                    // AVANCEMENT
                    // ==============================

                    int pourcentageAvancement = 0;

                    if (avancementCell.DataType ==
                        ClosedXML.Excel.XLDataType.Number)
                    {
                        var valeur =
                            avancementCell.GetDouble();

                        pourcentageAvancement =
                            (int)Math.Round(valeur * 100);

                        if (pourcentageAvancement > 100)
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


                    // ==============================
                    // CLASSIFICATIONS DYNAMIQUES
                    // ==============================

                    var unite = await _context.UnitesProjets
                        .FirstOrDefaultAsync(u =>
                            u.Actif &&
                            u.Nom.ToLower() ==
                            uniteTexte.ToLower());

                    var departement =
                        await _context.DepartementsProjets
                            .FirstOrDefaultAsync(d =>
                                d.Actif &&
                                d.Nom.ToLower() ==
                                departementTexte.ToLower());

                    var typeProjet =
                        await _context.TypesProjets
                            .FirstOrDefaultAsync(t =>
                                t.Actif &&
                                t.Nom.ToLower() ==
                                typeTexte.ToLower());

                    var plateforme =
                        await _context.PlateformesProjets
                            .FirstOrDefaultAsync(p =>
                                p.Actif &&
                                p.Nom.ToLower() ==
                                plateformeTexte.ToLower());


                    if (unite == null ||
                        departement == null ||
                        typeProjet == null ||
                        plateforme == null)
                    {
                        nbIgnores++;
                        continue;
                    }


                    // ==============================
                    // STATUT
                    // ==============================

                    if (!Enum.TryParse<StatutProjet>(
                        statutTexte,
                        true,
                        out var statut))
                    {
                        statut =
                            StatutProjet.WaitingRFC;
                    }


                    // ==============================
                    // PRIORITÉ
                    // ==============================

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


                    // ==============================
                    // UTILISATEURS
                    // ==============================

                    var ownerIt =
                        await ObtenirOuCreerUtilisateur(
                            ownerItNom);

                    var powerUser =
                        await ObtenirOuCreerUtilisateur(
                            powerUserNom);


                    // ==============================
                    // COMPTEUR
                    // ==============================

                    var compteur =
                        await _context.ReferenceCompteurs
                            .FirstOrDefaultAsync(
                                r => r.UniteProjetId == unite.Id);

                    if (compteur == null)
                    {
                        compteur = new ReferenceCompteur
                        {
                            UniteProjetId = unite.Id,
                            Prefixe = unite.Prefixe,
                            DernierNumero = 0
                        };

                        _context.ReferenceCompteurs.Add(
                            compteur);

                        await _context.SaveChangesAsync();
                    }

                    compteur.DernierNumero++;

                    var referenceGeneree =
                        $"{compteur.Prefixe}-{compteur.DernierNumero}";


                    // ==============================
                    // PROJET
                    // ==============================

                    var projet = new Projet
                    {
                        TicketId =
                            string.IsNullOrWhiteSpace(ticketId)
                                ? $"IMPORT-{nbImportes + 1}"
                                : ticketId,

                        Reference =
                            referenceGeneree,

                        Nom =
                            nom,

                        Description =
                            string.Empty,

                        UniteProjetId =
                            unite.Id,

                        DepartementProjetId =
                            departement.Id,

                        TypeProjetReferenceId =
                            typeProjet.Id,

                        PlateformeProjetId =
                            plateforme.Id,

                        Priorite =
                            priorite,

                        DateDebut =
                            dateDebut,

                        DateFin =
                            dateFin,

                        PourcentageAvancement =
                            pourcentageAvancement,

                        OwnerItId =
                            ownerIt?.Id,

                        PowerUserId =
                            powerUser?.Id,

                        Statut =
                            statut
                    };

                    _context.Projets.Add(projet);


                    // ==============================
                    // COMMENTAIRE
                    // ==============================

                    if (!string.IsNullOrWhiteSpace(
                        commentaireExcel))
                    {
                        var commentaire =
                            new Commentaire
                            {
                                Projet = projet,

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


                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                TempData["Succes"] =
                    $"{nbImportes} projet(s) importé(s) avec succès. " +
                    $"{nbIgnores} ligne(s) ignorée(s) " +
                    "(données incomplètes, classifications inexistantes ou dates invalides).";

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                TempData["Erreur"] =
                    "Une erreur est survenue pendant l'import Excel. " +
                    "Aucun projet n'a été importé.";

                return RedirectToAction("ImporterExcel");
            }
        }
        private async Task<Utilisateur?> ObtenirOuCreerUtilisateur(
      string nom)
        {
            if (string.IsNullOrWhiteSpace(nom) || nom == "-")
            {
                return null;
            }

            var utilisateur =
                await _context.Utilisateurs
                    .FirstOrDefaultAsync(
                        u => u.Nom == nom);

            if (utilisateur == null)
            {
                utilisateur = new Utilisateur
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

        // Kanban
        public async Task<IActionResult> Kanban()
        {
            var projets = await _context.Projets
                .AsNoTracking()
                .Include(p => p.OwnerIt)
                .Include(p => p.UniteProjet)
                .Include(p => p.DepartementProjet)
                .Include(p => p.TypeProjetReference)
                .Include(p => p.PlateformeProjet)
                .ToListAsync();

            var statutsAffiches = new List<StatutProjet>
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

        // Roadmap
        public async Task<IActionResult> Roadmap()
        {
            var projets = await _context.Projets
                .Include(p => p.OwnerIt)
                .Where(p =>
                    p.DateFin.HasValue &&
                    p.Statut != StatutProjet.Cancelled)
                .ToListAsync();

            var maintenant = DateTime.Now;

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

                var projetsDuTrimestre = projets
                    .Where(p =>
                        p.DateFin!.Value.Date >= debutTrimestre &&
                        p.DateFin.Value.Date <= finTrimestre)
                    .OrderBy(p => p.DateFin)
                    .ToList();

                trimestres.Add(
                    (label, projetsDuTrimestre));
            }

            ViewBag.Trimestres = trimestres;

            return View();
        }

        // Gantt
        public async Task<IActionResult> Gantt()
        {
            var projets = await _context.Projets
                .Where(p => p.Statut != StatutProjet.Cancelled)
                .ToListAsync();

            return View(projets);
        }


        // Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var projets = await _context.Projets
                .AsNoTracking()
                .Include(p => p.UniteProjet)
                .Include(p => p.DepartementProjet)
                .Select(p => new
                {
                    p.Statut,
                    UniteNom = p.UniteProjet != null
                        ? p.UniteProjet.Nom
                        : null,
                    DepartementNom = p.DepartementProjet != null
                        ? p.DepartementProjet.Nom
                        : null,
                    p.DateFin,
                    OwnerItNom = p.OwnerIt != null
                        ? p.OwnerIt.Nom
                        : null
                })
                .ToListAsync();

            var maintenant = DateTime.Now;

            var stats = new DashboardViewModel
            {
                TotalProjets = projets.Count,

                ProjetsActifs = projets.Count(p =>
                    p.Statut != StatutProjet.Closed &&
                    p.Statut != StatutProjet.Cancelled),

                ProjetsTermines = projets.Count(p =>
                    p.Statut == StatutProjet.Closed),

                ProjetsSuspendus = projets.Count(p =>
                    p.Statut == StatutProjet.Suspendu),

                ProjetsEnRetard = projets.Count(p =>
                    p.DateFin.HasValue &&
                    p.DateFin.Value < maintenant &&
                    p.Statut != StatutProjet.Closed &&
                    p.Statut != StatutProjet.Cancelled),

                RepartitionParStatut = projets
                    .GroupBy(p => p.Statut)
                    .Select(g => new StatDonnee
                    {
                        Label = g.Key.ToString(),
                        Valeur = g.Count()
                    })
                    .ToList(),

                RepartitionParUnite = projets
                    .Where(p => p.UniteNom != null)
                    .GroupBy(p => p.UniteNom!)
                    .Select(g => new StatDonnee
                    {
                        Label = g.Key,
                        Valeur = g.Count()
                    })
                    .ToList(),

                RepartitionParDepartement = projets
                    .Where(p => p.DepartementNom != null)
                    .GroupBy(p => p.DepartementNom!)
                    .Select(g => new StatDonnee
                    {
                        Label = g.Key,
                        Valeur = g.Count()
                    })
                    .ToList(),

                ChargeParOwnerIt = projets
                    .Where(p => p.OwnerItNom != null)
                    .GroupBy(p => p.OwnerItNom!)
                    .Select(g => new StatDonnee
                    {
                        Label = g.Key,
                        Valeur = g.Count()
                    })
                    .ToList()
            };

            return View(stats);
        }

        // Dashboard COMEX
        public async Task<IActionResult> DashboardComex()
        {
            var projets = await _context.Projets
                .AsNoTracking()
                .Include(p => p.TypeProjetReference)
                .Include(p => p.PlateformeProjet)
                .Select(p => new
                {
                    p.Statut,

                    TypeNom = p.TypeProjetReference != null
                        ? p.TypeProjetReference.Nom
                        : null,

                    PlateformeNom = p.PlateformeProjet != null
                        ? p.PlateformeProjet.Nom
                        : null,

                    p.DateFin
                })
                .ToListAsync();

            var maintenant = DateTime.Now;

            int vert = 0;
            int orange = 0;
            int rouge = 0;

            foreach (var p in projets)
            {
                if (p.Statut == StatutProjet.Closed ||
                    p.Statut == StatutProjet.Cancelled)
                {
                    continue;
                }

                if (!p.DateFin.HasValue ||
                    p.DateFin.Value >= maintenant)
                {
                    vert++;
                }
                else
                {
                    var joursRetard =
                        (maintenant - p.DateFin.Value).Days;

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

            var stats = new DashboardViewModel
            {
                TotalProjets = projets.Count,

                ProjetsActifs = projets.Count(p =>
                    p.Statut != StatutProjet.Closed &&
                    p.Statut != StatutProjet.Cancelled),

                PortfolioVert = vert,

                PortfolioOrange = orange,

                PortfolioRouge = rouge,

                RepartitionParType = projets
                    .Where(p => p.TypeNom != null)
                    .GroupBy(p => p.TypeNom!)
                    .Select(g => new StatDonnee
                    {
                        Label = g.Key,
                        Valeur = g.Count()
                    })
                    .ToList(),

                RepartitionParPlateforme = projets
                    .Where(p => p.PlateformeNom != null)
                    .GroupBy(p => p.PlateformeNom!)
                    .Select(g => new StatDonnee
                    {
                        Label = g.Key,
                        Valeur = g.Count()
                    })
                    .ToList()
            };

            return View(stats);
        }

        // Dashboard IT Manager
        public async Task<IActionResult> DashboardItManager()
        {
            var projets = await _context.Projets
                .AsNoTracking()
                .Select(p => new
                {
                    p.Statut,
                    p.Priorite,
                    p.DateFin,
                    p.DateCreation,
                    OwnerItNom = p.OwnerIt != null
                        ? p.OwnerIt.Nom
                        : null
                })
                .ToListAsync();

            var maintenant = DateTime.Now;
            var aujourdHui = maintenant.Date;
            var dans30Jours = aujourdHui.AddDays(30);

            var stats = new DashboardViewModel
            {
                TotalProjets = projets.Count,

                ProjetsActifs = projets.Count(p =>
                    p.Statut != StatutProjet.Closed &&
                    p.Statut != StatutProjet.Cancelled),

                ProjetsEnRetard = projets.Count(p =>
                    p.DateFin.HasValue &&
                    p.DateFin.Value < maintenant &&
                    p.Statut != StatutProjet.Closed &&
                    p.Statut != StatutProjet.Cancelled),

                ProjetsCritiques = projets.Count(p =>
                    p.Priorite == PrioriteProjet.High &&
                    p.Statut != StatutProjet.Closed &&
                    p.Statut != StatutProjet.Cancelled),

                DatesFinDuMois = await _context.Projets
                    .AsNoTracking()
                    .Include(p => p.OwnerIt)
                    .Where(p =>
                        p.DateFin.HasValue &&
                        p.DateFin.Value.Date >= aujourdHui &&
                        p.DateFin.Value.Date <= dans30Jours &&
                        p.Statut != StatutProjet.Closed &&
                        p.Statut != StatutProjet.Cancelled)
                    .OrderBy(p => p.DateFin)
                    .ToListAsync(),

                RepartitionParStatut = projets
                    .GroupBy(p => p.Statut)
                    .Select(g => new StatDonnee
                    {
                        Label = g.Key.ToString(),
                        Valeur = g.Count()
                    })
                    .ToList(),

                ChargeParOwnerIt = projets
                    .Where(p =>
                        p.OwnerItNom != null &&
                        p.Statut != StatutProjet.Closed &&
                        p.Statut != StatutProjet.Cancelled)
                    .GroupBy(p => p.OwnerItNom!)
                    .Select(g => new StatDonnee
                    {
                        Label = g.Key,
                        Valeur = g.Count()
                    })
                    .ToList(),

                AgingProjets = new List<StatDonnee>
        {
            new()
            {
                Label = "0-30 jours",
                Valeur = projets.Count(p =>
                    (maintenant - p.DateCreation).Days <= 30 &&
                    p.Statut != StatutProjet.Closed &&
                    p.Statut != StatutProjet.Cancelled)
            },

            new()
            {
                Label = "31-60 jours",
                Valeur = projets.Count(p =>
                    (maintenant - p.DateCreation).Days > 30 &&
                    (maintenant - p.DateCreation).Days <= 60 &&
                    p.Statut != StatutProjet.Closed &&
                    p.Statut != StatutProjet.Cancelled)
            },

            new()
            {
                Label = "61-90 jours",
                Valeur = projets.Count(p =>
                    (maintenant - p.DateCreation).Days > 60 &&
                    (maintenant - p.DateCreation).Days <= 90 &&
                    p.Statut != StatutProjet.Closed &&
                    p.Statut != StatutProjet.Cancelled)
            },

            new()
            {
                Label = "90+ jours",
                Valeur = projets.Count(p =>
                    (maintenant - p.DateCreation).Days > 90 &&
                    p.Statut != StatutProjet.Closed &&
                    p.Statut != StatutProjet.Cancelled)
            }
        }
            };

            return View(stats);
        }

        // Details
        public async Task<IActionResult> Details(int id)
        {
            var projet = await _context.Projets
                .Include(p => p.OwnerIt)
                .Include(p => p.PowerUser)
                .Include(p => p.UniteProjet)
                .Include(p => p.DepartementProjet)
                .Include(p => p.TypeProjetReference)
                .Include(p => p.PlateformeProjet)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (projet == null)
            {
                return NotFound();
            }

            var rfc = await _context.RFCs
                .Include(r => r.Champion)
                .Include(r => r.Sponsor)
                .FirstOrDefaultAsync(r => r.ProjetId == id);

            var actions = await _context.Actions
                .Include(a => a.Responsable)
                .Where(a => a.ProjetId == id)
                .ToListAsync();

            ViewBag.RFC = rfc;
            ViewBag.Actions = actions;

            var commentaires = await _context.Commentaires
                .Include(c => c.Auteur)
                .Where(c => c.ProjetId == id)
                .OrderByDescending(c => c.DatePublication)
                .ToListAsync();

            ViewBag.Commentaires = commentaires;

            var piecesJointes = await _context.PiecesJointes
                .Where(pj => pj.ProjetId == id)
                .OrderByDescending(pj => pj.DateAjout)
                .ToListAsync();

            ViewBag.PiecesJointes = piecesJointes;

            var historique = await _context.HistoriqueProjets
                .Include(h => h.Utilisateur)
                .Where(h => h.ProjetId == id)
                .OrderByDescending(h => h.DateAction)
                .ToListAsync();

            ViewBag.Historique = historique;

            var dernierCommentaire =
                commentaires.Any()
                    ? commentaires.Max(c => c.DatePublication)
                    : (DateTime?)null;

            var score =
                _scoreRisqueService.CalculerScore(
                    projet,
                    dernierCommentaire);

            ViewBag.ScoreRisque = score;

            ViewBag.NiveauRisque =
                _scoreRisqueService.ObtenirNiveau(score);

            ViewBag.CouleurRisque =
                _scoreRisqueService.ObtenirCouleur(score);

            return View(projet);
        }

        // Compte rendu
        public async Task<IActionResult> GenererCompteRendu(int id)
        {
            var projet = await _context.Projets
                .Include(p => p.OwnerIt)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (projet == null)
            {
                return NotFound();
            }

            var actions = await _context.Actions
                .Where(a => a.ProjetId == id)
                .ToListAsync();

            var commentaires = await _context.Commentaires
                .Include(c => c.Auteur)
                .OrderByDescending(c => c.DatePublication)
                .Where(c => c.ProjetId == id)
                .Take(5)
                .ToListAsync();

            var dateDebut = projet.DateDebut.HasValue
                ? projet.DateDebut.Value.ToString("dd/MM/yyyy")
                : "non définie";

            var dateFin = projet.DateFin.HasValue
                ? projet.DateFin.Value.ToString("dd/MM/yyyy")
                : "non définie";

            var prompt = $@"Rédige un compte-rendu professionnel et concis (en français) pour le projet suivant, destiné à un rapport de suivi interne.

Nom du projet : {projet.Nom}
Statut actuel : {projet.Statut}
Avancement : {projet.PourcentageAvancement}%
Responsable IT : {projet.OwnerIt?.Nom ?? "non assigné"}
Date de début : {dateDebut}
Date de fin : {dateFin}

Actions en cours ({actions.Count}) :
{string.Join("\n", actions.Select(a => $"- {a.Description} ({a.Statut})"))}

Derniers commentaires :
{string.Join("\n", commentaires.Select(c => $"- {c.Auteur?.Nom}: {c.Contenu}"))}

Structure attendue : un paragraphe de résumé de la situation, suivi des points d'attention si nécessaire.";

            var compteRendu =
                await _geminiService.GenererCompteRendu(prompt);

            ViewBag.Projet = projet;
            ViewBag.CompteRendu = compteRendu;

            return View();
        }

        // Create
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ProjetCreateViewModel
            {
                UtilisateursDisponibles =
                    await _context.Utilisateurs
                     .Where(u => u.EstActif)
                        .OrderBy(u => u.Nom)
                        .ToListAsync(),

                UnitesDisponibles =
                    await _context.UnitesProjets
                        .Where(u => u.Actif)
                        .OrderBy(u => u.Nom)
                        .ToListAsync(),

                DepartementsDisponibles =
                    await _context.DepartementsProjets
                        .Where(d => d.Actif)
                        .OrderBy(d => d.Nom)
                        .ToListAsync(),

                TypesDisponibles =
                    await _context.TypesProjets
                        .Where(t => t.Actif)
                        .OrderBy(t => t.Nom)
                        .ToListAsync(),

                PlateformesDisponibles =
                    await _context.PlateformesProjets
                        .Where(p => p.Actif)
                        .OrderBy(p => p.Nom)
                        .ToListAsync()
            };

            return View(model);
        }

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> Create(ProjetCreateViewModel model)
        {
            // Vérification des dates
            if (model.DateFin < model.DateDebut)
            {
                ModelState.AddModelError(
                    "DateFin",
                    "La date de fin doit être postérieure ou égale à la date de début.");
            }

            // Vérification de l'unité
            var unite = await _context.UnitesProjets
                .FirstOrDefaultAsync(u =>
                    u.Id == model.UniteProjetId &&
                    u.Actif);

            if (unite == null)
            {
                ModelState.AddModelError(
                    "UniteProjetId",
                    "L'unité sélectionnée est invalide.");
            }

            // Vérification du département
            var departement = await _context.DepartementsProjets
                .FirstOrDefaultAsync(d =>
                    d.Id == model.DepartementProjetId &&
                    d.Actif);

            if (departement == null)
            {
                ModelState.AddModelError(
                    "DepartementProjetId",
                    "Le département sélectionné est invalide.");
            }

            // Vérification du type
            var typeProjet = await _context.TypesProjets
                .FirstOrDefaultAsync(t =>
                    t.Id == model.TypeProjetReferenceId &&
                    t.Actif);

            if (typeProjet == null)
            {
                ModelState.AddModelError(
                    "TypeProjetReferenceId",
                    "Le type de projet sélectionné est invalide.");
            }

            // Vérification de la plateforme
            var plateforme = await _context.PlateformesProjets
                .FirstOrDefaultAsync(p =>
                    p.Id == model.PlateformeProjetId &&
                    p.Actif);

            if (plateforme == null)
            {
                ModelState.AddModelError(
                    "PlateformeProjetId",
                    "La plateforme sélectionnée est invalide.");
            }

            if (!ModelState.IsValid)
            {
                model.UtilisateursDisponibles =
                    await _context.Utilisateurs
                     .Where(u => u.EstActif)
                    .ToListAsync();


                model.UnitesDisponibles =
                    await _context.UnitesProjets
                        .Where(u => u.Actif)
                        .OrderBy(u => u.Nom)
                        .ToListAsync();

                model.DepartementsDisponibles =
                    await _context.DepartementsProjets
                        .Where(d => d.Actif)
                        .OrderBy(d => d.Nom)
                        .ToListAsync();

                model.TypesDisponibles =
                    await _context.TypesProjets
                        .Where(t => t.Actif)
                        .OrderBy(t => t.Nom)
                        .ToListAsync();

                model.PlateformesDisponibles =
                    await _context.PlateformesProjets
                        .Where(p => p.Actif)
                        .OrderBy(p => p.Nom)
                        .ToListAsync();

                return View(model);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                // Recharger l'unité dans la transaction
                unite = await _context.UnitesProjets
                    .FirstOrDefaultAsync(u =>
                        u.Id == model.UniteProjetId &&
                        u.Actif);

                if (unite == null)
                {
                    throw new InvalidOperationException(
                        "L'unité sélectionnée n'existe plus ou est inactive.");
                }

                // Recherche du compteur de l'unité
                var compteur = await _context.ReferenceCompteurs
                    .FirstOrDefaultAsync(
                        r => r.UniteProjetId == unite.Id);

                // Création du compteur si nécessaire
                if (compteur == null)
                {
                    compteur = new ReferenceCompteur
                    {
                        UniteProjetId = unite.Id,
                        Prefixe = unite.Prefixe,
                        DernierNumero = 0
                    };

                    _context.ReferenceCompteurs.Add(compteur);

                    await _context.SaveChangesAsync();
                }

                // Incrémentation
                compteur.DernierNumero++;

                var referenceGeneree =
                    $"{compteur.Prefixe}-{compteur.DernierNumero}";

                // Création du projet
                var projet = new Projet
                {
                    TicketId = model.TicketId,

                    Reference = referenceGeneree,

                    Nom = model.Nom,

                    Description = model.Description ?? string.Empty,

                    UniteProjetId = model.UniteProjetId,

                    DepartementProjetId = model.DepartementProjetId,

                    TypeProjetReferenceId = model.TypeProjetReferenceId,

                    PlateformeProjetId = model.PlateformeProjetId,

                    Priorite = model.Priorite,

                    DateDebut = model.DateDebut,

                    DateFin = model.DateFin,

                    OwnerItId = model.OwnerItId,

                    PowerUserId = model.PowerUserId,

                    Statut = StatutProjet.WaitingRFC
                };

                _context.Projets.Add(projet);

                await _context.SaveChangesAsync();

                _context.Projets.Add(projet);

                await _context.SaveChangesAsync();

                if (projet.PowerUserId.HasValue)
                {
                    await _notificationService.CreerNotificationAsync(
                        projet.Id,
                        projet.PowerUserId.Value,
                        "ValidationRequise",
                        $"Vous avez été assigné comme Power User sur le projet \"{projet.Nom}\". " +
                        "Merci de le valider pour permettre le démarrage du développement."
                    );
                }

                // Utilisateur ayant créé le projet

                // Utilisateur ayant créé le projet
                var nomAD = User.Identity?.Name;

                var auteur = await _context.Utilisateurs
                    .FirstOrDefaultAsync(
                        u => u.NomADUtilisateur == nomAD);

                _context.HistoriqueProjets.Add(
                    new HistoriqueProjet
                    {
                        ProjetId = projet.Id,
                        UtilisateurId = auteur?.Id ?? 0,
                        TypeAction = "Création",
                        Detail =
                            $"Projet créé avec le statut {projet.Statut}, " +
                            $"la référence {projet.Reference}, " +
                            $"du {projet.DateDebut:dd/MM/yyyy} " +
                            $"au {projet.DateFin:dd/MM/yyyy}",
                        DateAction = DateTime.Now
                    });

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["Succes"] =
                    $"Le projet \"{projet.Nom}\" a été créé avec succès. " +
                    $"Référence : {projet.Reference}";

                return RedirectToAction("Index");
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Erreur"] =
                    "Une erreur est survenue lors de la création du projet.";

                return RedirectToAction("Create");
            }
        }


        //Classification
        //ajouter unité
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjouterUnite(
            string nom,
            string prefixe)
        {
            nom = nom?.Trim() ?? string.Empty;
            prefixe = prefixe?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nom))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Le nom de l'unité est obligatoire."
                });
            }

            if (string.IsNullOrWhiteSpace(prefixe))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Le préfixe de l'unité est obligatoire."
                });
            }

            var existe = await _context.UnitesProjets
                .AnyAsync(u =>
                    u.Nom.ToLower() == nom.ToLower());

            if (existe)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Cette unité existe déjà."
                });
            }

            var nouvelleUnite = new UniteProjet
            {
                Nom = nom,
                Prefixe = prefixe,
                Actif = true
            };

            _context.UnitesProjets.Add(nouvelleUnite);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                id = nouvelleUnite.Id,
                nom = nouvelleUnite.Nom,
                prefixe = nouvelleUnite.Prefixe
            });
        }

        //ajouter département
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjouterDepartement(
            string nom)
        {
            nom = nom?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nom))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Le nom du département est obligatoire."
                });
            }

            var existe = await _context.DepartementsProjets
                .AnyAsync(d =>
                    d.Nom.ToLower() == nom.ToLower());

            if (existe)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Ce département existe déjà."
                });
            }

            var nouveauDepartement = new DepartementProjet
            {
                Nom = nom,
                Actif = true
            };

            _context.DepartementsProjets.Add(nouveauDepartement);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                id = nouveauDepartement.Id,
                nom = nouveauDepartement.Nom
            });
        }

        //ajouter projet
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjouterTypeProjet(
            string nom)
        {
            nom = nom?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nom))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Le nom du type est obligatoire."
                });
            }

            var existe = await _context.TypesProjets
                .AnyAsync(t =>
                    t.Nom.ToLower() == nom.ToLower());

            if (existe)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Ce type de projet existe déjà."
                });
            }

            var nouveauType = new TypeProjetReference
            {
                Nom = nom,
                Actif = true
            };

            _context.TypesProjets.Add(nouveauType);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                id = nouveauType.Id,
                nom = nouveauType.Nom
            });
        }


        //Ajouter plateforme
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjouterPlateforme(
            string nom)
        {
            nom = nom?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nom))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Le nom de la plateforme est obligatoire."
                });
            }

            var existe = await _context.PlateformesProjets
                .AnyAsync(p =>
                    p.Nom.ToLower() == nom.ToLower());

            if (existe)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Cette plateforme existe déjà."
                });
            }

            var nouvellePlateforme = new PlateformeProjet
            {
                Nom = nom,
                Actif = true
            };

            _context.PlateformesProjets.Add(nouvellePlateforme);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                id = nouvellePlateforme.Id,
                nom = nouvellePlateforme.Nom
            });
        }

        //ajouter utilisateur 
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjouterUtilisateur(
    string nomADUtilisateur,
    string nom,
    string email)
        {
            nomADUtilisateur = nomADUtilisateur?.Trim() ?? string.Empty;
            nom = nom?.Trim() ?? string.Empty;
            email = email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nomADUtilisateur))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Le nom AD utilisateur est obligatoire."
                });
            }

            if (string.IsNullOrWhiteSpace(nom))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Le nom complet est obligatoire."
                });
            }

            var existe = await _context.Utilisateurs
                .AnyAsync(u =>
                    u.NomADUtilisateur.ToLower() == nomADUtilisateur.ToLower());

            if (existe)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Cet utilisateur existe déjà."
                });
            }

            var nouvelUtilisateur = new Utilisateur
            {
                NomADUtilisateur = nomADUtilisateur,
                Nom = nom,
                Email = email,
                Role = RoleUtilisateur.Lecteur
            };

            _context.Utilisateurs.Add(nouvelUtilisateur);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                id = nouvelUtilisateur.Id,
                nom = nouvelUtilisateur.Nom,
                nomADUtilisateur = nouvelUtilisateur.NomADUtilisateur,
                email = nouvelUtilisateur.Email
            });
        }

        //suppression unité
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SupprimerUnite(int id)
        {
            var unite = await _context.UnitesProjets
                .FirstOrDefaultAsync(u => u.Id == id);

            if (unite == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Unité introuvable."
                });
            }

            var utilisee = await _context.Projets
                .AnyAsync(p => p.UniteProjetId == id);

            if (utilisee)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Cette unité est utilisée par un ou plusieurs projets."
                });
            }

            var compteur = await _context.ReferenceCompteurs
                .FirstOrDefaultAsync(r => r.UniteProjetId == id);

            if (compteur != null)
            {
                _context.ReferenceCompteurs.Remove(compteur);
            }

            _context.UnitesProjets.Remove(unite);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "L'unité a été supprimée."
            });
        }

        //suppression département 
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SupprimerDepartement(int id)
        {
            var departement = await _context.DepartementsProjets
                .FirstOrDefaultAsync(d => d.Id == id);

            if (departement == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Département introuvable."
                });
            }

            var utilise = await _context.Projets
                .AnyAsync(p => p.DepartementProjetId == id);

            if (utilise)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Ce département est utilisé par un ou plusieurs projets."
                });
            }

            _context.DepartementsProjets.Remove(departement);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Le département a été supprimé."
            });
        }

        //Suppression type
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SupprimerTypeProjet(int id)
        {
            var type = await _context.TypesProjets
                .FirstOrDefaultAsync(t => t.Id == id);

            if (type == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Type de projet introuvable."
                });
            }

            var utilise = await _context.Projets
                .AnyAsync(p => p.TypeProjetReferenceId == id);

            if (utilise)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Ce type est utilisé par un ou plusieurs projets."
                });
            }

            _context.TypesProjets.Remove(type);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Le type de projet a été supprimé."
            });
        }

        //suppression plateforme
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SupprimerPlateforme(int id)
        {
            var plateforme = await _context.PlateformesProjets
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plateforme == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Plateforme introuvable."
                });
            }

            var utilisee = await _context.Projets
                .AnyAsync(p => p.PlateformeProjetId == id);

            if (utilisee)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Cette plateforme est utilisée par un ou plusieurs projets."
                });
            }

            _context.PlateformesProjets.Remove(plateforme);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "La plateforme a été supprimée."
            });
        }

        //Suppression utilisateur
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SupprimerUtilisateur(int id)
        {
            var utilisateur = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.Id == id);

            if (utilisateur == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Utilisateur introuvable."
                });
            }

            var utiliseCommeOwner = await _context.Projets
                .AnyAsync(p => p.OwnerItId == id);

            var utiliseCommePowerUser = await _context.Projets
                .AnyAsync(p => p.PowerUserId == id);

            if (utiliseCommeOwner || utiliseCommePowerUser)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Cet utilisateur est encore associé à un ou plusieurs projets comme Owner IT ou Power User."
                });
            }

            _context.Utilisateurs.Remove(utilisateur);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "L'utilisateur a été supprimé."
            });
        }

        // Edit
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var projet = await _context.Projets
                .FirstOrDefaultAsync(p => p.Id == id);

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

                UniteProjetId = projet.UniteProjetId,
                DepartementProjetId = projet.DepartementProjetId,
                TypeProjetReferenceId = projet.TypeProjetReferenceId,
                PlateformeProjetId = projet.PlateformeProjetId,

                Statut = projet.Statut,
                Priorite = projet.Priorite,

                DateDebut = projet.DateDebut ?? DateTime.Today,
                DateFin = projet.DateFin ?? DateTime.Today,

                PourcentageAvancement = projet.PourcentageAvancement,
                OwnerItId = projet.OwnerItId,
                PowerUserId = projet.PowerUserId,

                UtilisateursDisponibles =
                    await _context.Utilisateurs
                     .Where(u => u.EstActif)
                    .ToListAsync(),

                UnitesDisponibles =
                    await _context.UnitesProjets
                        .Where(u => u.Actif)
                        .OrderBy(u => u.Nom)
                        .ToListAsync(),

                DepartementsDisponibles =
                    await _context.DepartementsProjets
                        .Where(d => d.Actif)
                        .OrderBy(d => d.Nom)
                        .ToListAsync(),

                TypesDisponibles =
                    await _context.TypesProjets
                        .Where(t => t.Actif)
                        .OrderBy(t => t.Nom)
                        .ToListAsync(),

                PlateformesDisponibles =
                    await _context.PlateformesProjets
                        .Where(p => p.Actif)
                        .OrderBy(p => p.Nom)
                        .ToListAsync()
            };

            return View(model);
        }


        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> Edit(ProjetEditViewModel model)
        {
            // Vérification des dates
            if (model.DateFin < model.DateDebut)
            {
                ModelState.AddModelError(
                    "DateFin",
                    "La date de fin doit être postérieure ou égale à la date de début.");
            }

            // Vérification de l'unité
            var unite = await _context.UnitesProjets
                .FirstOrDefaultAsync(u =>
                    u.Id == model.UniteProjetId &&
                    u.Actif);

            if (unite == null)
            {
                ModelState.AddModelError(
                    "UniteProjetId",
                    "L'unité sélectionnée est invalide.");
            }

            // Vérification du département
            var departement = await _context.DepartementsProjets
                .FirstOrDefaultAsync(d =>
                    d.Id == model.DepartementProjetId &&
                    d.Actif);

            if (departement == null)
            {
                ModelState.AddModelError(
                    "DepartementProjetId",
                    "Le département sélectionné est invalide.");
            }

            // Vérification du type
            var typeProjet = await _context.TypesProjets
                .FirstOrDefaultAsync(t =>
                    t.Id == model.TypeProjetReferenceId &&
                    t.Actif);

            if (typeProjet == null)
            {
                ModelState.AddModelError(
                    "TypeProjetReferenceId",
                    "Le type de projet sélectionné est invalide.");
            }

            // Vérification de la plateforme
            var plateforme = await _context.PlateformesProjets
                .FirstOrDefaultAsync(p =>
                    p.Id == model.PlateformeProjetId &&
                    p.Actif);

            if (plateforme == null)
            {
                ModelState.AddModelError(
                    "PlateformeProjetId",
                    "La plateforme sélectionnée est invalide.");
            }

            if (!ModelState.IsValid)
            {
                model.UtilisateursDisponibles =
                    await _context.Utilisateurs
                     .Where(u => u.EstActif)
                    .ToListAsync();

                model.UnitesDisponibles =
                    await _context.UnitesProjets
                        .Where(u => u.Actif)
                        .OrderBy(u => u.Nom)
                        .ToListAsync();

                model.DepartementsDisponibles =
                    await _context.DepartementsProjets
                        .Where(d => d.Actif)
                        .OrderBy(d => d.Nom)
                        .ToListAsync();

                model.TypesDisponibles =
                    await _context.TypesProjets
                        .Where(t => t.Actif)
                        .OrderBy(t => t.Nom)
                        .ToListAsync();

                model.PlateformesDisponibles =
                    await _context.PlateformesProjets
                        .Where(p => p.Actif)
                        .OrderBy(p => p.Nom)
                        .ToListAsync();

                return View(model);
            }

            var projet = await _context.Projets
                .FirstOrDefaultAsync(p => p.Id == model.Id);

            if (projet == null)
            {
                return NotFound();
            }

            // Anciennes valeurs
            var ancienTicketId = projet.TicketId;
            var ancienNom = projet.Nom;
            var ancienneDescription = projet.Description;

            var ancienneUniteProjetId = projet.UniteProjetId;
            var ancienDepartementProjetId = projet.DepartementProjetId;
            var ancienTypeProjetReferenceId = projet.TypeProjetReferenceId;
            var anciennePlateformeProjetId = projet.PlateformeProjetId;

            var ancienStatut = projet.Statut;
            var anciennePriorite = projet.Priorite;

            var ancienneDateDebut = projet.DateDebut;
            var ancienneDateFin = projet.DateFin;

            var ancienPourcentage = projet.PourcentageAvancement;
            var ancienOwnerItId = projet.OwnerItId;
            var ancienPowerUserId = projet.PowerUserId;

            var statutChange = ancienStatut != model.Statut;
            var powerUserChange = ancienPowerUserId != model.PowerUserId;

            var nouvelAvancement =
                ObtenirAvancementAutomatique(model.Statut, ancienPourcentage);

            var modifications = new List<string>();


            // Ticket ID
            if (ancienTicketId != model.TicketId)
            {
                modifications.Add(
                    $"Ticket ID : {ancienTicketId} → {model.TicketId}");
            }


            // Nom
            if (ancienNom != model.Nom)
            {
                modifications.Add(
                    $"Nom : {ancienNom} → {model.Nom}");
            }


            // Description
            if (ancienneDescription != (model.Description ?? string.Empty))
            {
                modifications.Add("Description modifiée");
            }


            // Unité
            if (ancienneUniteProjetId != model.UniteProjetId)
            {
                var ancienneUnite =
                    await _context.UnitesProjets
                        .Where(u => u.Id == ancienneUniteProjetId)
                        .Select(u => u.Nom)
                        .FirstOrDefaultAsync();

                var nouvelleUnite =
                    await _context.UnitesProjets
                        .Where(u => u.Id == model.UniteProjetId)
                        .Select(u => u.Nom)
                        .FirstOrDefaultAsync();

                modifications.Add(
                    $"Unité : {ancienneUnite ?? "Non définie"} → " +
                    $"{nouvelleUnite ?? "Non définie"}");
            }


            // Département
            if (ancienDepartementProjetId != model.DepartementProjetId)
            {
                var ancienDepartement =
                    await _context.DepartementsProjets
                        .Where(d => d.Id == ancienDepartementProjetId)
                        .Select(d => d.Nom)
                        .FirstOrDefaultAsync();

                var nouveauDepartement =
                    await _context.DepartementsProjets
                        .Where(d => d.Id == model.DepartementProjetId)
                        .Select(d => d.Nom)
                        .FirstOrDefaultAsync();

                modifications.Add(
                    $"Département : {ancienDepartement ?? "Non défini"} → " +
                    $"{nouveauDepartement ?? "Non défini"}");
            }


            // Type
            if (ancienTypeProjetReferenceId != model.TypeProjetReferenceId)
            {
                var ancienTypeNom =
                    await _context.TypesProjets
                        .Where(t => t.Id == ancienTypeProjetReferenceId)
                        .Select(t => t.Nom)
                        .FirstOrDefaultAsync();

                var nouveauTypeNom =
                    await _context.TypesProjets
                        .Where(t => t.Id == model.TypeProjetReferenceId)
                        .Select(t => t.Nom)
                        .FirstOrDefaultAsync();

                modifications.Add(
                    $"Type : {ancienTypeNom ?? "Non défini"} → " +
                    $"{nouveauTypeNom ?? "Non défini"}");
            }


            // Plateforme
            if (anciennePlateformeProjetId != model.PlateformeProjetId)
            {
                var anciennePlateforme =
                    await _context.PlateformesProjets
                        .Where(p => p.Id == anciennePlateformeProjetId)
                        .Select(p => p.Nom)
                        .FirstOrDefaultAsync();

                var nouvellePlateforme =
                    await _context.PlateformesProjets
                        .Where(p => p.Id == model.PlateformeProjetId)
                        .Select(p => p.Nom)
                        .FirstOrDefaultAsync();

                modifications.Add(
                    $"Plateforme : {anciennePlateforme ?? "Non définie"} → " +
                    $"{nouvellePlateforme ?? "Non définie"}");
            }


            // Statut
            if (ancienStatut != model.Statut)
            {
                modifications.Add(
                    $"Statut : {ancienStatut} → {model.Statut}");
            }


            // Priorité
            if (anciennePriorite != model.Priorite)
            {
                modifications.Add(
                    $"Priorité : {anciennePriorite} → {model.Priorite}");
            }


            // Date de début
            if (ancienneDateDebut != model.DateDebut)
            {
                var ancienneDate =
                    ancienneDateDebut?.ToString("dd/MM/yyyy")
                    ?? "Non définie";

                var nouvelleDate =
                    model.DateDebut.ToString("dd/MM/yyyy");

                modifications.Add(
                    $"Date de début : {ancienneDate} → {nouvelleDate}");
            }


            // Date de fin
            if (ancienneDateFin != model.DateFin)
            {
                var ancienneDate =
                    ancienneDateFin?.ToString("dd/MM/yyyy")
                    ?? "Non définie";

                var nouvelleDate =
                    model.DateFin.ToString("dd/MM/yyyy");

                modifications.Add(
                    $"Date de fin : {ancienneDate} → {nouvelleDate}");
            }


            // Avancement
            if (ancienPourcentage != nouvelAvancement)
            {
                modifications.Add(
                    $"Avancement : {ancienPourcentage}% → {nouvelAvancement}%");
            }


            // Owner IT
            if (ancienOwnerItId != model.OwnerItId)
            {
                var ancienOwner = ancienOwnerItId.HasValue
                    ? (await _context.Utilisateurs
                        .FindAsync(ancienOwnerItId.Value))?.Nom
                    : "Non assigné";

                var nouvelOwner = model.OwnerItId.HasValue
                    ? (await _context.Utilisateurs
                        .FindAsync(model.OwnerItId.Value))?.Nom
                    : "Non assigné";

                modifications.Add(
                    $"Owner IT : {ancienOwner ?? "Non assigné"} → " +
                    $"{nouvelOwner ?? "Non assigné"}");
            }


            // Power User
            if (ancienPowerUserId != model.PowerUserId)
            {
                var ancienPowerUser = ancienPowerUserId.HasValue
                    ? (await _context.Utilisateurs
                        .FindAsync(ancienPowerUserId.Value))?.Nom
                    : "Non assigné";

                var nouveauPowerUser = model.PowerUserId.HasValue
                    ? (await _context.Utilisateurs
                        .FindAsync(model.PowerUserId.Value))?.Nom
                    : "Non assigné";

                modifications.Add(
                    $"Power User : {ancienPowerUser ?? "Non assigné"} → " +
                    $"{nouveauPowerUser ?? "Non assigné"}");
            }


            // Mise à jour du projet
            projet.TicketId = model.TicketId;

            projet.Nom = model.Nom;

            projet.Description =
                model.Description ?? string.Empty;

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

            projet.DateDebut =
                model.DateDebut;

            projet.DateFin =
                model.DateFin;

            projet.PourcentageAvancement =
                nouvelAvancement;

            projet.OwnerItId =
                model.OwnerItId;

            projet.PowerUserId =
                model.PowerUserId;

            if (powerUserChange)
            {
                projet.ValidePowerUser = false;
                projet.DateValidationPowerUser = null;
            }


            // Historique
            if (modifications.Any())
            {
                var nomAD = User.Identity?.Name;

                var auteur = await _context.Utilisateurs
                    .FirstOrDefaultAsync(
                        u => u.NomADUtilisateur == nomAD);

                _context.HistoriqueProjets.Add(
                    new HistoriqueProjet
                    {
                        ProjetId = projet.Id,
                        UtilisateurId = auteur?.Id ?? 0,
                        TypeAction = "Modification",

                        Detail = string.Join(
                            Environment.NewLine,
                            modifications),

                        DateAction = DateTime.Now
                    });
            }


            await _context.SaveChangesAsync();


            // Notification changement de statut
            if (statutChange &&
                projet.OwnerItId.HasValue)
            {
                await _notificationService.CreerNotificationAsync(
                    projet.Id,
                    projet.OwnerItId.Value,
                    "ChangementStatut",
                    $"Le projet \"{projet.Nom}\" est passé de " +
                    $"{ancienStatut} à {projet.Statut}."
                );
            }

            // Notification nouveau Power User
            if (powerUserChange &&
                projet.PowerUserId.HasValue)
            {
                await _notificationService.CreerNotificationAsync(
                    projet.Id,
                    projet.PowerUserId.Value,
                    "ValidationRequise",
                    $"Vous avez été assigné comme Power User sur le projet \"{projet.Nom}\". " +
                    "Merci de le valider pour permettre le démarrage du développement."
                );
            }

            TempData["Succes"] =
                "Les modifications ont été enregistrées.";

            return RedirectToAction("Index");
        }

        // Changer statut

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpGet]
        public async Task<IActionResult> ChangerStatut(int id)
        {
            var projet = await _context.Projets
                .Include(p => p.TypeProjetReference)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (projet == null)
            {
                return NotFound();
            }

            ViewBag.TransitionsPossibles =
                _workflowService.GetTransitionsPossibles(
                    projet.Statut,
                    projet.StatutPrecedent,
                    projet.TypeProjetReference);

            return View(projet);
        }


        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> ChangerStatut(
            int id,
            StatutProjet nouveauStatut)
        {
            var projet = await _context.Projets
                .Include(p => p.OwnerIt)
                .Include(p => p.TypeProjetReference)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (projet == null)
            {
                return NotFound();
            }

            var transitionsAutorisees =
                _workflowService.GetTransitionsPossibles(
                    projet.Statut,
                    projet.StatutPrecedent,
                    projet.TypeProjetReference);

            if (!transitionsAutorisees.Contains(nouveauStatut))
            {
                TempData["Erreur"] =
                    "Transition de statut non autorisée.";

                return RedirectToAction(
                    "ChangerStatut",
                    new { id });
            }

            if (nouveauStatut == StatutProjet.DevStarted &&
    !projet.ValidePowerUser)
            {
                TempData["Erreur"] =
                    "Le Power User doit valider le projet avant de démarrer le développement.";

                return RedirectToAction(
                    "ChangerStatut",
                    new { id });
            }

            var ancienStatut = projet.Statut;

            var nomAD = User.Identity?.Name;

            var auteur = await _context.Utilisateurs
                .FirstOrDefaultAsync(
                    u => u.NomADUtilisateur == nomAD);

            _context.HistoriqueProjets.Add(
                new HistoriqueProjet
                {
                    ProjetId = projet.Id,
                    UtilisateurId = auteur?.Id ?? 0,
                    TypeAction = "Changement de statut",
                    Detail = $"{ancienStatut} → {nouveauStatut}",
                    DateAction = DateTime.Now
                });

            projet.StatutPrecedent = ancienStatut;
            projet.Statut = nouveauStatut;

            projet.PourcentageAvancement =
               ObtenirAvancementAutomatique(nouveauStatut, projet.PourcentageAvancement);

            await _context.SaveChangesAsync();

            if (projet.OwnerItId.HasValue)
            {
                await _notificationService.CreerNotificationAsync(
                    projet.Id,
                    projet.OwnerItId.Value,
                    "ChangementStatut",
                    $"Le projet \"{projet.Nom}\" est passé de " +
                    $"{ancienStatut} à {nouveauStatut}."
                );
            }

            TempData["Succes"] =
                $"Statut changé vers {nouveauStatut}.";

            return RedirectToAction("Index");
        }

        // Validation Power User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValiderPowerUser(int id)
        {
            var projet = await _context.Projets
                .Include(p => p.PowerUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (projet == null)
            {
                return NotFound();
            }

            var nomAD = User.Identity?.Name;

            if (projet.PowerUser == null ||
                projet.PowerUser.NomADUtilisateur != nomAD)
            {
                TempData["Erreur"] =
                    "Seul le Power User assigné à ce projet peut le valider.";

                return RedirectToAction("Details", new { id });
            }

            projet.ValidePowerUser = true;
            projet.DateValidationPowerUser = DateTime.Now;

            var auteur = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.NomADUtilisateur == nomAD);

            _context.HistoriqueProjets.Add(new HistoriqueProjet
            {
                ProjetId = projet.Id,
                UtilisateurId = auteur?.Id ?? 0,
                TypeAction = "Validation Power User",
                Detail = $"Le projet a été validé par le Power User {projet.PowerUser.Nom}.",
                DateAction = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Succes"] =
                "Le projet a été validé avec succès.";

            return RedirectToAction("Details", new { id });
        }

        // Delete
        [Authorize(Roles = "Administrateur")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var projet = await _context.Projets
                .Include(p => p.OwnerIt)
                .Include(p => p.PowerUser)
                .Include(p => p.UniteProjet)
                .Include(p => p.DepartementProjet)
                .Include(p => p.TypeProjetReference)
                .Include(p => p.PlateformeProjet)
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
            var projet = await _context.Projets
                .FirstOrDefaultAsync(p => p.Id == id);

            if (projet != null)
            {
                _context.Projets.Remove(projet);

                await _context.SaveChangesAsync();
            }

            TempData["Succes"] =
                "Le projet a été supprimé.";

            return RedirectToAction("Index");
        }
    }
}