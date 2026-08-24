using Microsoft.EntityFrameworkCore;
using GestionProjetSocota.Data;
using Microsoft.AspNetCore.Authentication.Negotiate;
using GestionProjetSocota.Services;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);


// ============================================================
// DATABASE
// ============================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));


// ============================================================
// AUTHENTIFICATION WINDOWS
// ============================================================

builder.Services.AddAuthentication(
    NegotiateDefaults.AuthenticationScheme
)
.AddNegotiate();


// ============================================================
// AUTORISATION
// ============================================================

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});


// ============================================================
// MVC
// ============================================================

builder.Services.AddControllersWithViews();


// ============================================================
// LOCALISATION
// ============================================================

builder.Services.AddLocalization();

var languesSupportees = new[]
{
    "fr",
    "en"
};

var optionsLocalisation = new RequestLocalizationOptions()
    .SetDefaultCulture("fr")
    .AddSupportedCultures(languesSupportees)
    .AddSupportedUICultures(languesSupportees);

optionsLocalisation.RequestCultureProviders.Insert(
    0,
    new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider()
);


// ============================================================
// SERVICES DU PROJET
// ============================================================

builder.Services.AddScoped<WorkflowService>();

builder.Services.AddScoped<ScoreRisqueService>();

builder.Services.AddHttpClient<GeminiService>();


// ============================================================
// NOTIFICATIONS / EMAIL
// ============================================================

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<EmailSettings>(
    provider =>
        provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<EmailSettings>>().Value);

builder.Services.AddScoped<NotificationService>();

// Charger EmailSettings depuis appsettings.json
var emailSettings = builder.Configuration
    .GetSection("EmailSettings")
    .Get<EmailSettings>();

if (emailSettings == null)
{
    throw new InvalidOperationException(
        "La configuration EmailSettings est introuvable dans appsettings.json."
    );
}

// Enregistrer EmailSettings dans l'injection de dépendances
builder.Services.AddSingleton(emailSettings);

// Enregistrer NotificationService
builder.Services.AddScoped<NotificationService>();


// ============================================================
// BUILD APPLICATION
// ============================================================

var app = builder.Build();


// ============================================================
// PIPELINE HTTP
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();


// ============================================================
// SYNCHRONISATION UTILISATEUR WINDOWS
// ============================================================

app.UseMiddleware<GestionProjetSocota.Middlewares.SyncUtilisateurMiddleware>();

app.UseAuthorization();


// ============================================================
// ROUTES
// ============================================================

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
)
.WithStaticAssets();


// ============================================================
// DEMARRAGE
// ============================================================

app.Run();