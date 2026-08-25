using System;

namespace GestionProjetSocota.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int? ProjetId { get; set; }
        public Projet? Projet { get; set; }
        public int UtilisateurId { get; set; }
        public Utilisateur? Utilisateur { get; set; }

        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        
        public DateTime DateCreation { get; set; } = DateTime.Now;
        public bool EstLue { get; set; } = false;
    }
}
