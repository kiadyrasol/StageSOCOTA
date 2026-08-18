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

        public ProjetController(ApplicationDbContext context, WorkflowService workflowService, GeminiService geminiService, ScoreRisqueService scoreRisqueService)
        {
            _context = context;
            _workflowService = workflowService;
            _geminiService = geminiService;
            _scoreRisqueService = scoreRisqueService;
        }


        // Index
        public async Task<IActionResult> Index()
        {
            var projets = await _context.Projets
                .Include(p => p.OwnerIt)
                .Include(p => p.PowerUser)
                .ToListAsync();

            var scores = new Dictionary<int, (int score, string couleur)>();
            foreach (var p in projets)
            {
                var dernierCommentaire = await _context.Commentaires
                    .Where(c => c.ProjetId == p.Id)
                    .OrderByDescending(c => c.DatePublication)
                    .Select(c => (DateTime?)c.DatePublication)
                    .FirstOrDefaultAsync();

                var score = _scoreRisqueService.CalculerScore(p, dernierCommentaire);
                scores[p.Id] = (score, _scoreRisqueService.ObtenirCouleur(score));
            }

            ViewBag.Scores = scores;

            return View(projets);
        }


        // Recherche
        public async Task<IActionResult> Recherche(Unite? unite, Departement? departement, StatutProjet? statut, TypeProjet? type, int? ownerItId)
        {
            var query = _context.Projets
                .Include(p => p.OwnerIt)
                .Include(p => p.PowerUser)
                .AsQueryable();

            if (unite.HasValue)
                query = query.Where(p => p.Unite == unite.Value);

            if (departement.HasValue)
                query = query.Where(p => p.Departement == departement.Value);

            if (statut.HasValue)
                query = query.Where(p => p.Statut == statut.Value);

            if (type.HasValue)
                query = query.Where(p => p.Type == type.Value);

            if (ownerItId.HasValue)
                query = query.Where(p => p.OwnerItId == ownerItId.Value);

            var projets = await query.ToListAsync();

            ViewBag.Utilisateurs = await _context.Utilisateurs.ToListAsync();
            ViewBag.FiltresActifs = new { unite, departement, statut, type, ownerItId };

            return View(projets);
        }


        // Export du fichier excel
        public async Task<IActionResult> ExporterExcel(Unite? unite, Departement? departement, StatutProjet? statut, TypeProjet? type, int? ownerItId)
        {
            var query = _context.Projets.Include(p => p.OwnerIt).AsQueryable();

            if (unite.HasValue) query = query.Where(p => p.Unite == unite.Value);
            if (departement.HasValue) query = query.Where(p => p.Departement == departement.Value);
            if (statut.HasValue) query = query.Where(p => p.Statut == statut.Value);
            if (type.HasValue) query = query.Where(p => p.Type == type.Value);
            if (ownerItId.HasValue) query = query.Where(p => p.OwnerItId == ownerItId.Value);

            var projets = await query.ToListAsync();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var feuille = workbook.Worksheets.Add("Projets");

            feuille.Cell(1, 1).Value = "Ticket ID";
            feuille.Cell(1, 2).Value = "Nom";
            feuille.Cell(1, 3).Value = "Unité";
            feuille.Cell(1, 4).Value = "Département";
            feuille.Cell(1, 5).Value = "Statut";
            feuille.Cell(1, 6).Value = "Owner IT";
            feuille.Row(1).Style.Font.Bold = true;

            for (int i = 0; i < projets.Count; i++)
            {
                var p = projets[i];
                feuille.Cell(i + 2, 1).Value = p.TicketId;
                feuille.Cell(i + 2, 2).Value = p.Nom;
                feuille.Cell(i + 2, 3).Value = p.Unite.ToString();
                feuille.Cell(i + 2, 4).Value = p.Departement.ToString();
                feuille.Cell(i + 2, 5).Value = p.Statut.ToString();
                feuille.Cell(i + 2, 6).Value = p.OwnerIt?.Nom ?? "-";
            }

            feuille.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "projets.xlsx");
        }


        // Pdf
        public async Task<IActionResult> ExporterPdf(Unite? unite, Departement? departement, StatutProjet? statut, TypeProjet? type, int? ownerItId)
        {
            var query = _context.Projets.Include(p => p.OwnerIt).AsQueryable();

            if (unite.HasValue) query = query.Where(p => p.Unite == unite.Value);
            if (departement.HasValue) query = query.Where(p => p.Departement == departement.Value);
            if (statut.HasValue) query = query.Where(p => p.Statut == statut.Value);
            if (type.HasValue) query = query.Where(p => p.Type == type.Value);
            if (ownerItId.HasValue) query = query.Where(p => p.OwnerItId == ownerItId.Value);

            var projets = await query.ToListAsync();

            var document = QuestPDF.Fluent.Document.Create(conteneur =>
            {
                conteneur.Page(page =>
                {
                    page.Margin(30);

                    page.Header().Text("Liste des projets - GestionProjetSocota")
                        .FontSize(16).Bold().FontColor("#155D15");

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
                            table.Cell().Text(p.TicketId);
                            table.Cell().Text(p.Nom);
                            table.Cell().Text(p.Unite.ToString());
                            table.Cell().Text(p.Statut.ToString());
                            table.Cell().Text(p.OwnerIt?.Nom ?? "-");
                        }
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();

            return File(pdfBytes, "application/pdf", "projets.pdf");
        }


        // Kanban
        public async Task<IActionResult> Kanban()
        {
            var projets = await _context.Projets
                .Include(p => p.OwnerIt)
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

            ViewBag.StatutsAffiches = statutsAffiches;

            return View(projets);
        }


        // Roadmap
        public async Task<IActionResult> Roadmap()
        {
            var projets = await _context.Projets
                .Include(p => p.OwnerIt)
                .Where(p => p.Deadline.HasValue && p.Statut != StatutProjet.Cancelled)
                .ToListAsync();

            var maintenant = DateTime.Now;
            var trimestres = new List<(string Label, List<Projet> Projets)>();

            for (int i = 0; i < 4; i++)
            {
                var dateReference = maintenant.AddMonths(i * 3);
                var trimestreNum = (dateReference.Month - 1) / 3 + 1;
                var label = $"T{trimestreNum} {dateReference.Year}";

                var moisDebut = (trimestreNum - 1) * 3 + 1;
                var debutTrimestre = new DateTime(dateReference.Year, moisDebut, 1);
                var finTrimestre = debutTrimestre.AddMonths(3).AddDays(-1);

                var projetsDuTrimestre = projets
                    .Where(p => p.Deadline!.Value.Date >= debutTrimestre && p.Deadline.Value.Date <= finTrimestre)
                    .OrderBy(p => p.Deadline)
                    .ToList();

                trimestres.Add((label, projetsDuTrimestre));
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
                .Include(p => p.OwnerIt)
                .ToListAsync();

            var stats = new DashboardViewModel
            {
                TotalProjets = projets.Count,
                ProjetsActifs = projets.Count(p => p.Statut != StatutProjet.Closed && p.Statut != StatutProjet.Cancelled),
                ProjetsTermines = projets.Count(p => p.Statut == StatutProjet.Closed),
                ProjetsSuspendus = projets.Count(p => p.Statut == StatutProjet.Suspendu),
                ProjetsEnRetard = projets.Count(p => p.Deadline.HasValue && p.Deadline < DateTime.Now && p.Statut != StatutProjet.Closed),

                RepartitionParStatut = projets
                    .GroupBy(p => p.Statut)
                    .Select(g => new StatDonnee { Label = g.Key.ToString(), Valeur = g.Count() })
                    .ToList(),

                RepartitionParUnite = projets
                    .GroupBy(p => p.Unite)
                    .Select(g => new StatDonnee { Label = g.Key.ToString(), Valeur = g.Count() })
                    .ToList(),

                RepartitionParDepartement = projets
                    .GroupBy(p => p.Departement)
                    .Select(g => new StatDonnee { Label = g.Key.ToString(), Valeur = g.Count() })
                    .ToList(),

                ChargeParOwnerIt = projets
                    .Where(p => p.OwnerIt != null)
                    .GroupBy(p => p.OwnerIt!.Nom)
                    .Select(g => new StatDonnee { Label = g.Key, Valeur = g.Count() })
                    .ToList()
            };

            return View(stats);
        }


        // Dashboard COMEX
        public async Task<IActionResult> DashboardComex()
        {
            var projets = await _context.Projets.ToListAsync();

            int vert = 0, orange = 0, rouge = 0;

            foreach (var p in projets.Where(p => p.Statut != StatutProjet.Closed && p.Statut != StatutProjet.Cancelled))
            {
                if (!p.Deadline.HasValue || p.Deadline >= DateTime.Now)
                {
                    vert++;
                }
                else
                {
                    var joursRetard = (DateTime.Now - p.Deadline.Value).Days;
                    if (joursRetard < 30) orange++;
                    else rouge++;
                }
            }

            var stats = new DashboardViewModel
            {
                TotalProjets = projets.Count,
                ProjetsActifs = projets.Count(p => p.Statut != StatutProjet.Closed && p.Statut != StatutProjet.Cancelled),

                PortfolioVert = vert,
                PortfolioOrange = orange,
                PortfolioRouge = rouge,

                RepartitionParType = projets
                    .GroupBy(p => p.Type)
                    .Select(g => new StatDonnee { Label = g.Key.ToString(), Valeur = g.Count() })
                    .ToList(),

                RepartitionParPlateforme = projets
                    .GroupBy(p => p.Plateforme)
                    .Select(g => new StatDonnee { Label = g.Key.ToString(), Valeur = g.Count() })
                    .ToList()
            };

            return View(stats);
        }


        // Dashboard IT Manager
        public async Task<IActionResult> DashboardItManager()
        {
            var projets = await _context.Projets
                .Include(p => p.OwnerIt)
                .ToListAsync();

            var maintenant = DateTime.Now;

            var stats = new DashboardViewModel
            {
                TotalProjets = projets.Count,
                ProjetsActifs = projets.Count(p => p.Statut != StatutProjet.Closed && p.Statut != StatutProjet.Cancelled),
                ProjetsEnRetard = projets.Count(p => p.Deadline.HasValue && p.Deadline < maintenant && p.Statut != StatutProjet.Closed),

                ProjetsCritiques = projets.Count(p => p.Priorite == PrioriteProjet.High && p.Statut != StatutProjet.Closed),

                DeadlinesDuMois = projets
                    .Where(p => p.Deadline.HasValue
                        && p.Deadline.Value.Date >= maintenant.Date
                        && p.Deadline.Value.Date <= maintenant.Date.AddDays(30)
                        && p.Statut != StatutProjet.Closed)
                    .OrderBy(p => p.Deadline)
                    .ToList(),

                RepartitionParStatut = projets
                    .GroupBy(p => p.Statut)
                    .Select(g => new StatDonnee { Label = g.Key.ToString(), Valeur = g.Count() })
                    .ToList(),

                ChargeParOwnerIt = projets
                    .Where(p => p.OwnerIt != null && p.Statut != StatutProjet.Closed)
                    .GroupBy(p => p.OwnerIt!.Nom)
                    .Select(g => new StatDonnee { Label = g.Key, Valeur = g.Count() })
                    .ToList(),

                AgingProjets = new List<StatDonnee>
                {
                    new() { Label = "0-30 jours", Valeur = projets.Count(p => (maintenant - p.DateCreation).Days <= 30 && p.Statut != StatutProjet.Closed) },
                    new() { Label = "31-60 jours", Valeur = projets.Count(p => (maintenant - p.DateCreation).Days > 30 && (maintenant - p.DateCreation).Days <= 60 && p.Statut != StatutProjet.Closed) },
                    new() { Label = "61-90 jours", Valeur = projets.Count(p => (maintenant - p.DateCreation).Days > 60 && (maintenant - p.DateCreation).Days <= 90 && p.Statut != StatutProjet.Closed) },
                    new() { Label = "90+ jours", Valeur = projets.Count(p => (maintenant - p.DateCreation).Days > 90 && p.Statut != StatutProjet.Closed) }
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

            var dernierCommentaire = commentaires.Any() ? commentaires.Max(c => c.DatePublication) : (DateTime?)null;
            var score = _scoreRisqueService.CalculerScore(projet, dernierCommentaire);

            ViewBag.ScoreRisque = score;
            ViewBag.NiveauRisque = _scoreRisqueService.ObtenirNiveau(score);
            ViewBag.CouleurRisque = _scoreRisqueService.ObtenirCouleur(score);

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

            var prompt = $@"Rédige un compte-rendu professionnel et concis (en français) pour le projet suivant, destiné à un rapport de suivi interne.

Nom du projet : {projet.Nom}
Statut actuel : {projet.Statut}
Avancement : {projet.PourcentageAvancement}%
Responsable IT : {projet.OwnerIt?.Nom ?? "non assigné"}
Deadline : {(projet.Deadline.HasValue ? projet.Deadline.Value.ToString("dd/MM/yyyy") : "non définie")}

Actions en cours ({actions.Count}) :
{string.Join("\n", actions.Select(a => $"- {a.Description} ({a.Statut})"))}

Derniers commentaires :
{string.Join("\n", commentaires.Select(c => $"- {c.Auteur?.Nom}: {c.Contenu}"))}

Structure attendue : un paragraphe de résumé de la situation, suivi des points d'attention si nécessaire.";

            var compteRendu = await _geminiService.GenererCompteRendu(prompt);

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

            var nomAD = User.Identity?.Name;
            var auteur = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.NomADUtilisateur == nomAD);

            _context.HistoriqueProjets.Add(new HistoriqueProjet
            {
                ProjetId = projet.Id,
                UtilisateurId = auteur?.Id ?? 0,
                TypeAction = "Création",
                Detail = $"Projet créé avec le statut {projet.Statut}"
            });
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"Le projet \"{projet.Nom}\" a été créé avec succès.";

            return RedirectToAction("Index");
        }


        // Edit
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

            if (projet.Statut != model.Statut)
            {
                var nomAD = User.Identity?.Name;
                var auteur = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.NomADUtilisateur == nomAD);

                _context.HistoriqueProjets.Add(new HistoriqueProjet
                {
                    ProjetId = projet.Id,
                    UtilisateurId = auteur?.Id ?? 0,
                    TypeAction = "Modification",
                    Detail = $"Statut : {projet.Statut} → {model.Statut}"
                });
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
            projet.PourcentageAvancement = (model.Statut == StatutProjet.WaitingRFC) ? 0 : model.PourcentageAvancement;
            projet.OwnerItId = model.OwnerItId;
            projet.PowerUserId = model.PowerUserId;

            await _context.SaveChangesAsync();
            TempData["Succes"] = "Les modifications ont été enregistrées.";

            return RedirectToAction("Index");
        }


        // Changer statut
        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpGet]
        public async Task<IActionResult> ChangerStatut(int id)
        {
            var projet = await _context.Projets.FindAsync(id);
            if (projet == null)
            {
                return NotFound();
            }

            ViewBag.TransitionsPossibles = _workflowService.GetTransitionsPossibles(projet.Statut, projet.StatutPrecedent, projet.Type);

            return View(projet);
        }

        [Authorize(Roles = "Administrateur,ChefDeProjet")]
        [HttpPost]
        public async Task<IActionResult> ChangerStatut(int id, StatutProjet nouveauStatut)
        {
            var projet = await _context.Projets.FindAsync(id);
            if (projet == null)
            {
                return NotFound();
            }

            var transitionsAutorisees = _workflowService.GetTransitionsPossibles(projet.Statut, projet.StatutPrecedent, projet.Type);

            if (!transitionsAutorisees.Contains(nouveauStatut))
            {
                TempData["Erreur"] = "Transition de statut non autorisée.";
                return RedirectToAction("ChangerStatut", new { id });
            }

            var nomAD = User.Identity?.Name;
            var auteur = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.NomADUtilisateur == nomAD);

            _context.HistoriqueProjets.Add(new HistoriqueProjet
            {
                ProjetId = projet.Id,
                UtilisateurId = auteur?.Id ?? 0,
                TypeAction = "Changement de statut",
                Detail = $"{projet.Statut} → {nouveauStatut}"
            });

            projet.StatutPrecedent = projet.Statut;
            projet.Statut = nouveauStatut;
            await _context.SaveChangesAsync();
            TempData["Succes"] = $"Statut changé vers {nouveauStatut}.";

            return RedirectToAction("Index");
        }


        // Delete
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

            TempData["Succes"] = "Le projet a été supprimé.";
            return RedirectToAction("Index");
        }
    }
}