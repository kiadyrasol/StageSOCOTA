using System.ComponentModel.DataAnnotations;

namespace GestionProjetSocota.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire")]
        [DataType(DataType.Password)]
        public string MotDePasse { get; set; } = string.Empty;
    }
}