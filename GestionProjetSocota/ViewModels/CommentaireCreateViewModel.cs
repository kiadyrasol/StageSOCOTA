using System.ComponentModel.DataAnnotations;

namespace GestionProjetSocota.ViewModels
{
    public class CommentaireCreateViewModel
    {
        public int ProjetId { get; set; }

        [Required(ErrorMessage = "Le commentaire ne peut pas être vide")]
        [MaxLength(1000, ErrorMessage = "Ne peut pas dépasser 1000 caractères")]

        public string Contenu { get; set; } = string.Empty;
    }
}