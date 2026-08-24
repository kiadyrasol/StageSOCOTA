namespace GestionProjetSocota.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public int ProjetId { get; set; }

        public Projet Projet { get; set; } = null!;

        public string Type { get; set; } = string.Empty;

        public DateTime DateEnvoi { get; set; } = DateTime.Now;

        public string Destinataire { get; set; } = string.Empty;
    }
}