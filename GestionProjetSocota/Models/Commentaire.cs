namespace GestionProjetSocota.Models
{
    public class Commentaire
    {
        public int Id { get; set; }

        public int ProjetId { get; set; }
        public Projet? Projet { get; set; }

        public int AuteurId { get; set; }
        public Utilisateur? Auteur { get; set; }

        public string Contenu { get; set; } = string.Empty;
        public DateTime DatePublication { get; set; } = DateTime.Now;
    }
}