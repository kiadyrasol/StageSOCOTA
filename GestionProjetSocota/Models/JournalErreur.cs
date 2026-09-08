using System.ComponentModel.DataAnnotations;

namespace GestionProjetSocota.Models
{
    public class JournalErreur
    {
        public int Id { get; set; }

        public DateTime DateErreur { get; set; } = DateTime.Now;

        [Required]
        public string Message { get; set; } = string.Empty;

        public string? StackTrace { get; set; }

        public string? TypeException { get; set; }

        public string? CheminRequete { get; set; }

        public string? MethodeHttp { get; set; }

        public string? Utilisateur { get; set; }
    }
}