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
    }

    public class StatDonnee
    {
        public string Label { get; set; } = string.Empty;
        public int Valeur { get; set; }
    }
}