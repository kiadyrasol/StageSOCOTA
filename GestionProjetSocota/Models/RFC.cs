using Microsoft.EntityFrameworkCore;

namespace GestionProjetSocota.Models
{
    public class RFC
    {
        public int Id { get; set; }

        public int ProjetId { get; set; }
        public Projet? Projet { get; set; }

        public string BusinessCase { get; set; } = string.Empty;
        [Precision(18, 2)]
        public decimal? RoiEstime { get; set; }
        public string GainsAttendus { get; set; } = string.Empty;

        public int ChampionId { get; set; }
        public Utilisateur? Champion { get; set; }

        public int SponsorId { get; set; }
        public Utilisateur? Sponsor { get; set; }

        public string Priorite { get; set; } = string.Empty;
        public bool EstValide { get; set; } = false;
        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}