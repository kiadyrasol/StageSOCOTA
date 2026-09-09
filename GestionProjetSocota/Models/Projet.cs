namespace GestionProjetSocota.Models
{
    public enum PrioriteProjet
    {
        Low,
        Medium,
        High
    }

    public enum StatutProjet
    {
        WaitingRFC,
        RFCApproved,
        Analyse,
        DevStarted,
        Testing,
        Debugging,
        Formation,
        GoLive,
        Support,
        Closed,
        Prospection,
        Achat,
        Installation,
        Configuration,
        DataUpload,
        Securisation,
        ApresVente,
        Suspendu,
        Cancelled
    }

    public class Projet
    {
        public int Id { get; set; }

        public string TicketId { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;

        public string Nom { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;


        // =========================================================
        // CLASSIFICATION 
        // =========================================================

        public int? UniteProjetId { get; set; }

        public UniteProjet? UniteProjet { get; set; }


        public int? DepartementProjetId { get; set; }

        public DepartementProjet? DepartementProjet { get; set; }


        public int? TypeProjetReferenceId { get; set; }

        public TypeProjetReference? TypeProjetReference { get; set; }


        public int? PlateformeProjetId { get; set; }
        
        public PlateformeProjet? PlateformeProjet { get; set; }


        // =========================================================
        // STATUT
        // =========================================================

        public StatutProjet StatutPrecedent { get; set; }

        public StatutProjet Statut { get; set; } =
            StatutProjet.WaitingRFC;


        // =========================================================
        // RESPONSABLES
        // =========================================================

        public int? OwnerItId { get; set; }

        public Utilisateur? OwnerIt { get; set; }


        public int? PowerUserId { get; set; }

        public Utilisateur? PowerUser { get; set; }


        // =========================================================
        // PRIORITÉ
        // =========================================================

        public PrioriteProjet Priorite { get; set; } =
            PrioriteProjet.Medium;


        public string DevVolume { get; set; } = string.Empty;

        public DateTime? DateDebut { get; set; }

        public DateTime? DateFin { get; set; }

        public int PourcentageAvancement { get; set; } = 0;

        public bool ValidePowerUser { get; set; } = false;

        public DateTime? DateValidationPowerUser { get; set; }

        public string Commentaire { get; set; } = string.Empty;

        public DateTime DateCreation { get; set; } = DateTime.Now;

    }
}