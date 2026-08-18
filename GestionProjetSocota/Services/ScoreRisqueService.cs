using GestionProjetSocota.Models;

namespace GestionProjetSocota.Services
{
    public class ScoreRisqueService
    {
        public int CalculerScore(Projet projet, DateTime? dernierCommentaire)
        {
            int score = 0;

            if (projet.Statut == StatutProjet.Closed || projet.Statut == StatutProjet.Cancelled)
            {
                return 0;
            }

            if (projet.Deadline.HasValue)
            {
                var joursRestants = (projet.Deadline.Value.Date - DateTime.Now.Date).Days;
                if (joursRestants < 0)
                {
                    score += 40;
                }
                else if (joursRestants <= 7)
                {
                    score += 20;
                }
            }

            if (projet.Priorite == PrioriteProjet.High)
            {
                score += 20;
            }

            if (projet.Statut == StatutProjet.Suspendu)
            {
                score += 15;
            }

            if (projet.OwnerItId == null)
            {
                score += 10;
            }

            if (!dernierCommentaire.HasValue || (DateTime.Now - dernierCommentaire.Value).Days > 30)
            {
                score += 15;
            }

            return Math.Min(score, 100);
        }

        public string ObtenirNiveau(int score)
        {
            if (score >= 61) return "Élevé";
            if (score >= 31) return "Moyen";
            return "Faible";
        }

        public string ObtenirCouleur(int score)
        {
            if (score >= 61) return "danger";
            if (score >= 31) return "warning";
            return "success";
        }
    }
}