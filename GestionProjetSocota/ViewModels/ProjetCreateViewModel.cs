using GestionProjetSocota.Models;
using System.ComponentModel.DataAnnotations;

namespace GestionProjetSocota.ViewModels
{
    public class ProjetCreateViewModel
    {
        [Required]
        [Display(Name = "Ticket ID")]
        public string TicketId { get; set; } = string.Empty;

        [Required]
        public string Nom { get; set; } = string.Empty;

        public string? Description { get; set; }
        // =========================
        // CLASSIFICATION
        // =========================

        [Required]
        [Display(Name = "Unité")]
        public int UniteProjetId { get; set; }

        [Required]
        [Display(Name = "Département")]
        public int DepartementProjetId { get; set; }

        [Required]
        [Display(Name = "Type de projet")]
        public int TypeProjetReferenceId { get; set; }

        [Required]
        [Display(Name = "Plateforme")]
        public int PlateformeProjetId { get; set; }

        // =========================
        // RESPONSABLES
        // =========================

        [Display(Name = "Owner IT")]
        public int? OwnerItId { get; set; }

        [Display(Name = "Power User")]
        public int? PowerUserId { get; set; }

        // =========================
        // PROJET
        // =========================

        [Required]
        public PrioriteProjet Priorite { get; set; } = PrioriteProjet.Medium;

        public string DevVolume { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Date de début")]
        [DataType(DataType.Date)]
        public DateTime? DateDebut { get; set; }

        [Required]
        [Display(Name = "Date de fin")]
        [DataType(DataType.Date)]
        public DateTime? DateFin { get; set; }

        public int PourcentageAvancement { get; set; } = 0;

        public string Commentaire { get; set; } = string.Empty;

        // =========================
        // LISTES POUR LES DROPDOWNS
        // =========================

        public List<UniteProjet> UnitesDisponibles { get; set; } = new();

        public List<DepartementProjet> DepartementsDisponibles { get; set; } = new();

        public List<TypeProjetReference> TypesDisponibles { get; set; } = new();

        public List<PlateformeProjet> PlateformesDisponibles { get; set; } = new();

        public List<Utilisateur> UtilisateursDisponibles { get; set; } = new();

        // =========================
        // AJOUT RAPIDE
        // =========================

        public string? NouvelleUnite { get; set; }

        public string? NouvelleUnitePrefixe { get; set; }

        public string? NouveauDepartement { get; set; }

        public string? NouveauTypeProjet { get; set; }

        public string? NouvellePlateforme { get; set; }

        // =========================
        // NOUVEAU OWNER IT
        // =========================

        public string? NouvelOwnerItNomADUtilisateur { get; set; }

        public string? NouvelOwnerItNom { get; set; }

        public string? NouvelOwnerItEmail { get; set; }

        // =========================
        // NOUVEAU POWER USER
        // =========================

        public string? NouveauPowerUserNomADUtilisateur { get; set; }

        public string? NouveauPowerUserNom { get; set; }

        public string? NouveauPowerUserEmail { get; set; }
    }
}