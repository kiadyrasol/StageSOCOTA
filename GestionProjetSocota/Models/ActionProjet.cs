namespace GestionProjetSocota.Models
{
    public enum StatutAction
    {
        Open,
        Ongoing,
        Closed
    }

    public class ActionProjet
    {
        public int Id { get; set; }

        public int ProjetId { get; set; }
        public Projet? Projet { get; set; }

        public string Description { get; set; } = string.Empty;

        public int ResponsableId { get; set; }
        public Utilisateur? Responsable { get; set; }

        public DateTime DateEcheance { get; set; }
        public StatutAction Statut { get; set; } = StatutAction.Open;
    }
}