using System.ComponentModel.DataAnnotations;

namespace GestionProjetSocota.ViewModels
{
    public class CommentaireCreateViewModel
    {
        public int ProjetId { get; set; }

        [Required(ErrorMessage = "Le commentaire ne peut pas être vide")]
        public string Contenu { get; set; } = string.Empty;
    }
}