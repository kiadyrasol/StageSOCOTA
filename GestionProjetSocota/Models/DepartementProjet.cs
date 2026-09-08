using System.ComponentModel.DataAnnotations;

namespace GestionProjetSocota.Models
{
    public class DepartementProjet
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        public bool Actif { get; set; } = true;
    }
}