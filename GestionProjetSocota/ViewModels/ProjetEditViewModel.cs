using System.ComponentModel.DataAnnotations;
using GestionProjetSocota.Models;

namespace GestionProjetSocota.ViewModels
{
    public class ProjetEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le Ticket ID est obligatoire")]
        public string TicketId { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom du projet est obligatoire")]
        public string Nom { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public Unite Unite { get; set; }

        [Required]
        public Departement Departement { get; set; }

        [Required]
        public TypeProjet Type { get; set; }

        [Required]
        public Plateforme Plateforme { get; set; }

        [Required]
        public StatutProjet Statut { get; set; }

       public PrioriteProjet Priorite { get; set; }
        public DateTime? Deadline { get; set; }
        public int PourcentageAvancement { get; set; }

        public int? OwnerItId { get; set; }
        public int? PowerUserId { get; set; }

        public List<Utilisateur> UtilisateursDisponibles { get; set; } = new();
    }
}