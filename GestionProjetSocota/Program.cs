using Microsoft.EntityFrameworkCore;
using GestionProjetSocota.Data;
using Microsoft.AspNetCore.Authentication.Negotiate;
using GestionProjetSocota.Services;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddAuthentication(
    NegotiateDefaults.AuthenticationScheme
)
.AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<WorkflowService>();

builder.Services.AddScoped<ScoreRisqueService>();

builder.Services.AddHttpClient<GeminiService>();

builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseMiddleware<GestionProjetSocota.Middlewares.ErrorLoggingMiddleware>();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseMiddleware<GestionProjetSocota.Middlewares.SyncUtilisateurMiddleware>();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
)
.WithStaticAssets();

app.Run();