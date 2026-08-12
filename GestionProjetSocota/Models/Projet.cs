namespace GestionProjetSocota.Models
{
    public enum Unite
    {
        CTN, SGL, CRE
    }

    public enum Departement
    {
        CAL, IND, LOG, IT, PRO, MPF, QUA, SUST, CTE, SALES, PLN
    }

    public enum TypeProjet
    {
        InHouse,
        Outsourced
    }

    public enum Plateforme
    {
        WEB, GPAO, PBI, SUN, Oracle, CRP, Mobile, SEAM, FREvolve
    }

    public enum StatutProjet
    {
        WaitingRFC, RFCApproved, Analyse, DevStarted, Testing, Debugging, Formation, GoLive, Support, Closed,
        Prospection, Achat, Installation, Configuration, DataUpload, Securisation, ApresVente,
        Suspendu, Cancelled
    }

    public class Projet
    {
        public int Id { get; set; }
        public string TicketId { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public Unite Unite { get; set; }
        public Departement Departement { get; set; }
        public TypeProjet Type { get; set; }
        public Plateforme Plateforme { get; set; }

        public StatutProjet StatutPrecedent { get; set; }
        public StatutProjet Statut { get; set; } = StatutProjet.WaitingRFC;

        public int? OwnerItId { get; set; }
        public Utilisateur? OwnerIt { get; set; }

        public int? PowerUserId { get; set; }
        public Utilisateur? PowerUser { get; set; }

        public string Priorite { get; set; } = string.Empty;
        public string DevVolume { get; set; } = string.Empty;

        public DateTime? Deadline { get; set; }
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }

        public int PourcentageAvancement { get; set; } = 0;
        public string Commentaire { get; set; } = string.Empty;
    }
}
