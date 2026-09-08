using System.ComponentModel.DataAnnotations;
using GestionProjetSocota.Models;

namespace GestionProjetSocota.ViewModels
{
    public class ProjetEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le Ticket ID est obligatoire")]
        [MaxLength(50, ErrorMessage = "Le Ticket ID ne peut pas dépasser 50 caractères")]
        public string TicketId { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom du projet est obligatoire")]
        [MaxLength(200, ErrorMessage = "Le nom ne peut pas dépasser 200 caractères")]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "La description ne peut pas dépasser 2000 caractères")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "L'unité est obligatoire")]
        public int UniteProjetId { get; set; }

        [Required(ErrorMessage = "Le département est obligatoire")]
        public int DepartementProjetId { get; set; }

        [Required(ErrorMessage = "Le type de projet est obligatoire")]
        public int TypeProjetReferenceId { get; set; }

        [Required(ErrorMessage = "La plateforme est obligatoire")]
        public int PlateformeProjetId { get; set; }

        [Required]
        public StatutProjet Statut { get; set; }

        public PrioriteProjet Priorite { get; set; }

        [Required(ErrorMessage = "La date de début est obligatoire")]
        public DateTime DateDebut { get; set; }

        [Required(ErrorMessage = "La date de fin est obligatoire")]
        public DateTime DateFin { get; set; }

        public int PourcentageAvancement { get; set; }

        public int? OwnerItId { get; set; }

        public int? PowerUserId { get; set; }

        public List<Utilisateur> UtilisateursDisponibles { get; set; } = new();

        public List<UniteProjet> UnitesDisponibles { get; set; } = new();

        public List<DepartementProjet> DepartementsDisponibles { get; set; } = new();

        public List<TypeProjetReference> TypesDisponibles { get; set; } = new();

        public List<PlateformeProjet> PlateformesDisponibles { get; set; } = new();
    }
}