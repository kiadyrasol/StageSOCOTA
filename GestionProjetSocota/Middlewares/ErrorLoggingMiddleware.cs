using GestionProjetSocota.Data;
using GestionProjetSocota.Models;

namespace GestionProjetSocota.Middlewares
{
    public class ErrorLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                try
                {
                    var journal = new JournalErreur
                    {
                        DateErreur = DateTime.Now,
                        Message = ex.Message,
                        StackTrace = ex.StackTrace,
                        TypeException = ex.GetType().FullName,
                        CheminRequete = context.Request.Path,
                        MethodeHttp = context.Request.Method,
                        Utilisateur = context.User?.Identity?.Name
                    };

                    dbContext.JournauxErreurs.Add(journal);
                    await dbContext.SaveChangesAsync();
                }
                catch
                {
                    
                }

                throw;
            }
        }
    }
}