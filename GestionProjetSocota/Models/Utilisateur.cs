namespace GestionProjetSocota.Models
{
   public enum RoleUtilisateur
{
    Administrateur, // 0
    ChefDeProjet,    // 1
    PowerUser,       // 2
    Lecteur          // 3
}

    public class Utilisateur
    {
        public int Id { get; set; }

        public string NomADUtilisateur { get; set; } = string.Empty;

        public string Nom { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public RoleUtilisateur Role { get; set; } = RoleUtilisateur.Lecteur;
        
        public bool EstActif { get; set; } = true;
    }
}