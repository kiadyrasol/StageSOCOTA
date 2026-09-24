using GestionProjetSocota.Data;
using GestionProjetSocota.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionProjetSocota.Middlewares
{
    public class SyncUtilisateurMiddleware
    {
        private readonly RequestDelegate _next;

        public SyncUtilisateurMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            // --- DÉMO : FORCER L'UTILISATEUR ADMIN SIMULÉ ---
            var nomAD = "SOCOTA\\AdminDemo";

            var utilisateur = await dbContext.Utilisateurs
                .FirstOrDefaultAsync(u => u.NomADUtilisateur == nomAD);

            if (utilisateur == null)
            {
                utilisateur = new Utilisateur
                {
                    NomADUtilisateur = nomAD,
                    Nom = "Admin Demo",
                    Email = "admin@socota.com",
                    Role = RoleUtilisateur.Administrateur // Forcé sur Admin
                };

                dbContext.Utilisateurs.Add(utilisateur);
                await dbContext.SaveChangesAsync();
            }

            // Génération des claims Admin pour l'ensemble des visiteurs
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, nomAD),
                new Claim(ClaimTypes.Role, RoleUtilisateur.Administrateur.ToString()),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "Administrateur")
            };

            var identity = new ClaimsIdentity(
                claims,
                "DemoAuth",
                ClaimTypes.Name,
                ClaimTypes.Role);

            context.User = new ClaimsPrincipal(identity);
            // ------------------------------------------------

            await _next(context);
        }
    }
}