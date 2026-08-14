using GestionProjetSocota.Models;

namespace GestionProjetSocota.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalProjets { get; set; }
        public int ProjetsActifs { get; set; }
        public int ProjetsTermines { get; set; }
        public int ProjetsSuspendus { get; set; }
        public int ProjetsEnRetard { get; set; }
        public List<StatDonnee> RepartitionParStatut { get; set; } = new();
        public List<StatDonnee> RepartitionParUnite { get; set; } = new();
        public List<StatDonnee> RepartitionParDepartement { get; set; } = new();
        public List<StatDonnee> ChargeParOwnerIt { get; set; } = new();
        public int PortfolioVert { get; set; }
        public int PortfolioOrange { get; set; }
        public int PortfolioRouge { get; set; }
        public List<StatDonnee> RepartitionParType { get; set; } = new();
        public List<StatDonnee> RepartitionParPlateforme { get; set; } = new();
        public int ProjetsCritiques { get; set; }
        public List<Projet> DeadlinesDuMois { get; set; } = new();
        public List<StatDonnee> AgingProjets { get; set; } = new();
    }

    public class StatDonnee
    {
        public string Label { get; set; } = string.Empty;
        public int Valeur { get; set; }
    }
}