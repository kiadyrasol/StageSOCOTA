using Microsoft.AspNetCore.Mvc;
using GestionProjetSocota.ViewModels;

namespace GestionProjetSocota.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Email == "kiady@gmail.com" && model.MotDePasse == "Banana123")
            {
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Email ou mot de passe incorrect");
            return View(model);
        }
    }
}