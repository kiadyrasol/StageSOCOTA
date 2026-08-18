namespace GestionProjetSocota.Models
{
    public class HistoriqueProjet
    {
        public int Id { get; set; }

        public int ProjetId { get; set; }
        public Projet? Projet { get; set; }

        public int UtilisateurId { get; set; }
        public Utilisateur? Utilisateur { get; set; }

        public string TypeAction { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public DateTime DateAction { get; set; } = DateTime.Now;
    }
}