namespace GestionProjetSocota.Models
{
    public enum TypePieceJointe
    {
        RFC, MOM, Analyse, CahierDesCharges, CaptureEcran, PlanDeTests
    }

    public class PieceJointe
    {
        public int Id { get; set; }

        public int ProjetId { get; set; }
        public Projet? Projet { get; set; }

        public string NomFichier { get; set; } = string.Empty;
        public string CheminStockage { get; set; } = string.Empty;
        public TypePieceJointe Type { get; set; }
        public DateTime DateAjout { get; set; } = DateTime.Now;
    }
}