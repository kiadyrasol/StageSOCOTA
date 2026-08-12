namespace GestionProjetSocota.Models
{
    public enum RoleUtilisateur
    {
        Administrateur,
        ChefDeProjet,
        PowerUser,
        Lecteur
    }

    public class Utilisateur
    {
        public int Id { get; set; }
        public string NomADUtilisateur { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public RoleUtilisateur Role { get; set; } = RoleUtilisateur.Lecteur;
    }
}