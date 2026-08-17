using System.ComponentModel.DataAnnotations;
using GestionProjetSocota.Models;

namespace GestionProjetSocota.ViewModels
{
    public class RFCCreateViewModel
    {
        public int ProjetId { get; set; }

        [Required(ErrorMessage = "Le Business Case est obligatoire")]
        [MaxLength(2000, ErrorMessage = "Ne peut pas dépasser 2000 caractères")]

        public string BusinessCase { get; set; } = string.Empty;

        public decimal? RoiEstime { get; set; }
        [MaxLength(1000, ErrorMessage = "Ne peut pas dépasser 1000 caractères")]

        public string? GainsAttendus { get; set; }

        [Required(ErrorMessage = "Le Champion est obligatoire")]
        public int ChampionId { get; set; }

        [Required(ErrorMessage = "Le Sponsor est obligatoire")]
        public int SponsorId { get; set; }

        public string? Priorite { get; set; }

        public List<Utilisateur> UtilisateursDisponibles { get; set; } = new();
    }
}