using System.ComponentModel.DataAnnotations;
using GestionProjetSocota.Models;
using Microsoft.AspNetCore.Http;

namespace GestionProjetSocota.ViewModels
{
    public class PieceJointeCreateViewModel
    {
        public int ProjetId { get; set; }

        [Required(ErrorMessage = "Le fichier est obligatoire")]
        public IFormFile Fichier { get; set; } = null!;

        [Required]
        public TypePieceJointe Type { get; set; }
    }
}