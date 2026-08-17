using System.ComponentModel.DataAnnotations;
using GestionProjetSocota.Models;

namespace GestionProjetSocota.ViewModels
{
    public class ActionCreateViewModel
    {
        public int ProjetId { get; set; }

        [Required(ErrorMessage = "La description est obligatoire")]
        [MaxLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]

        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le responsable est obligatoire")]
        public int ResponsableId { get; set; }

        [Required(ErrorMessage = "La date d'échéance est obligatoire")]
        public DateTime DateEcheance { get; set; }

        public List<Utilisateur> UtilisateursDisponibles { get; set; } = new();
    }
}