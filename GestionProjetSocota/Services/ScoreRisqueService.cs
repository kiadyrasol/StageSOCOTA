using GestionProjetSocota.Models;

namespace GestionProjetSocota.Services
{
    public class ScoreRisqueService
    {
        public int CalculerScore(
            Projet projet,
            DateTime? dernierCommentaire)
        {
            int score = 0;

            // Projet terminé ou annulé = aucun risque
            if (projet.Statut == StatutProjet.Closed ||
                projet.Statut == StatutProjet.Cancelled)
            {
                return 0;
            }

            // Risque lié à la date de fin
            if (projet.DateFin.HasValue)
            {
                var joursRestants =
                    (projet.DateFin.Value.Date - DateTime.Now.Date).Days;

                // Projet en retard
                if (joursRestants < 0)
                {
                    score += 40;
                }
                // Fin dans les 7 prochains jours
                else if (joursRestants <= 7)
                {
                    score += 20;
                }
            }

            // Priorité élevée
            if (projet.Priorite == PrioriteProjet.High)
            {
                score += 20;
            }

            // Projet suspendu
            if (projet.Statut == StatutProjet.Suspendu)
            {
                score += 15;
            }

            // Aucun Owner IT
            if (projet.OwnerItId == null)
            {
                score += 10;
            }

            // Aucun commentaire récent
            if (!dernierCommentaire.HasValue ||
                (DateTime.Now - dernierCommentaire.Value).Days > 30)
            {
                score += 15;
            }

            return Math.Min(score, 100);
        }

        public string ObtenirNiveau(int score)
        {
            if (score >= 61)
            {
                return "Élevé";
            }

            if (score >= 31)
            {
                return "Moyen";
            }

            return "Faible";
        }

        public string ObtenirCouleur(int score)
        {
            if (score >= 61)
            {
                return "danger";
            }

            if (score >= 31)
            {
                return "warning";
            }

            return "success";
        }
    }
}