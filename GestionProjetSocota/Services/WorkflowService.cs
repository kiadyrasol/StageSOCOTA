using GestionProjetSocota.Models;


namespace GestionProjetSocota.Services
{
    public class WorkflowService
    {

        // InHouse
        private static readonly Dictionary<StatutProjet, List<StatutProjet>> TransitionsInHouse = new()
        {
            { StatutProjet.WaitingRFC, new() { StatutProjet.RFCApproved } },
            { StatutProjet.RFCApproved, new() { StatutProjet.Analyse } },
            { StatutProjet.Analyse, new() { StatutProjet.DevStarted } },
            { StatutProjet.DevStarted, new() { StatutProjet.Testing } },
            { StatutProjet.Testing, new() { StatutProjet.Debugging, StatutProjet.Formation } },
            { StatutProjet.Debugging, new() { StatutProjet.Testing, StatutProjet.Formation } },
            { StatutProjet.Formation, new() { StatutProjet.GoLive } },
            { StatutProjet.GoLive, new() { StatutProjet.Support } },
            { StatutProjet.Support, new() { StatutProjet.Closed } },
        };


        // Outsourced
        private static readonly Dictionary<StatutProjet, List<StatutProjet>> TransitionsOutsourced = new()
        {
            { StatutProjet.WaitingRFC, new() { StatutProjet.RFCApproved } },
            { StatutProjet.RFCApproved, new() { StatutProjet.Prospection } },
            { StatutProjet.Prospection, new() { StatutProjet.Achat } },
            { StatutProjet.Achat, new() { StatutProjet.Installation } },
            { StatutProjet.Installation, new() { StatutProjet.Configuration } },
            { StatutProjet.Configuration, new() { StatutProjet.DataUpload } },
            { StatutProjet.DataUpload, new() { StatutProjet.Securisation } },
            { StatutProjet.Securisation, new() { StatutProjet.Formation } },
            { StatutProjet.Formation, new() { StatutProjet.GoLive } },
            { StatutProjet.GoLive, new() { StatutProjet.ApresVente } },
            { StatutProjet.ApresVente, new() { StatutProjet.Closed } },
        };


        // Transitions
        public List<StatutProjet> GetTransitionsPossibles(StatutProjet statutActuel, StatutProjet statutPrecedent, TypeProjet type)
        {
            if (statutActuel == StatutProjet.Suspendu || statutActuel == StatutProjet.Cancelled)
            {
                return new List<StatutProjet> { statutPrecedent };
            }

            var table = type == TypeProjet.InHouse ? TransitionsInHouse : TransitionsOutsourced;

            var transitions = table.TryGetValue(statutActuel, out var liste)
                ? new List<StatutProjet>(liste)
                : new List<StatutProjet>();

            if (statutActuel != StatutProjet.Closed)
            {
                transitions.Add(StatutProjet.Suspendu);
                transitions.Add(StatutProjet.Cancelled);
            }

            return transitions;
        }
    }
}