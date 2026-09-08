namespace GestionProjetSocota.Models
{
    public class ReferenceCompteur
    {
        public int Id { get; set; }

        public int UniteProjetId { get; set; }

        public UniteProjet UniteProjet { get; set; } = null!;

        public string Prefixe { get; set; } = string.Empty;

        public long DernierNumero { get; set; }
    }
}