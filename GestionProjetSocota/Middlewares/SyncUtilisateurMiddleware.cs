using GestionProjetSocota.Data;
using GestionProjetSocota.Models;
using Microsoft.EntityFrameworkCore;

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
                var utilisateurExiste = await dbContext.Utilisateurs
                    .AnyAsync(u => u.NomADUtilisateur == nomAD);

                if (!utilisateurExiste)
                {
                    var nomAffiche = nomAD.Contains('\\') ? nomAD.Split('\\')[1] : nomAD;

                    var nouvelUtilisateur = new Utilisateur
                    {
                        NomADUtilisateur = nomAD,
                        Nom = nomAffiche,
                        Email = string.Empty,
                        Role = RoleUtilisateur.Lecteur
                    };

                    dbContext.Utilisateurs.Add(nouvelUtilisateur);
                    await dbContext.SaveChangesAsync();
                }
            }

            await _next(context);
        }
    }
}