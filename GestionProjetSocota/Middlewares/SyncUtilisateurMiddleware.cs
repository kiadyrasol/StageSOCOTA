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
            var nomAD = context.User?.Identity?.Name;

            if (!string.IsNullOrEmpty(nomAD))
            {
                var utilisateur = await dbContext.Utilisateurs
                    .FirstOrDefaultAsync(u => u.NomADUtilisateur == nomAD);

                if (utilisateur == null)
                {
                    var nomAffiche = nomAD.Contains('\\') ? nomAD.Split('\\')[1] : nomAD;

                    utilisateur = new Utilisateur
                    {
                        NomADUtilisateur = nomAD,
                        Nom = nomAffiche,
                        Email = string.Empty,
                        Role = RoleUtilisateur.Lecteur
                    };

                    dbContext.Utilisateurs.Add(utilisateur);
                    await dbContext.SaveChangesAsync();
                }

                var identity = context.User!.Identity as ClaimsIdentity;

                if (identity != null)
                {
                    var claims = new List<Claim>(identity.Claims)
                    {
                        new Claim(ClaimTypes.Role, utilisateur.Role.ToString())
                    };

                    var nouvelleIdentity = new ClaimsIdentity(
                        claims,
                        identity.AuthenticationType,
                        identity.NameClaimType,
                        ClaimTypes.Role);

                    context.User = new ClaimsPrincipal(nouvelleIdentity);
                }
            }

            await _next(context);
        }
    }
}