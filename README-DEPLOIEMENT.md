markdown
# README Technique — Déploiement GestionProjetSocota

## 1. Prérequis

- **SDK .NET** (version 10 ou compatible) — https://dotnet.microsoft.com/download
- **SQL Server** (LocalDB pour développement, ou une instance SQL Server complète pour la production)
- **IIS** avec le module ASP.NET Core Hosting Bundle installé (pour un déploiement Windows Server)
- Un compte de domaine Windows Active Directory (Socota) pour l'authentification

## 2. Récupérer le projet

Copier l'ensemble du code source du projet sur la machine de destination
(dépôt de code source interne, ou archive fournie séparément).


## 3. Configuration — appsettings.json

**Important** :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=VOTRE_SERVEUR;Database=GestionProjetSocotaDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Gemini": {
    "ApiKey": "VOTRE_CLE_API_GEMINI"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- Remplacer VOTRE_SERVEUR par le nom de l'instance SQL Server (ex: `(localdb)\mssqllocaldb` en local, ou le nom du serveur de production)
- Remplacer VOTRE_CLE_API_GEMINI par une clé obtenue sur https://aistudio.google.com/apikey (nécessaire pour la fonctionnalité "Générer un compte-rendu IA" — l'application fonctionne sans, mais cette fonctionnalité précise sera indisponible)

## 4. Créer la base de données

Dans le dossier du projet :

dotnet tool install --global dotnet-ef
dotnet ef database update


Ça applique automatiquement toutes les migrations et crée les tables nécessaires.

## 5. Lancer en local (développement)

dotnet run


L'application démarre sur http://localhost:5009 (ou le port indiqué dans le terminal).

## 6. Déploiement en production (IIS)

1. Publier le projet :

dotnet publish -c Release -o ./publish

2. Copier le contenu du dossier `publish` vers le serveur IIS
3. Dans IIS, créer un site pointant vers ce dossier, avec le pool d'applications configuré en "Aucun code managé"
4. Copier manuellement `appsettings.json` (configuré avec les vraies valeurs de production) dans le dossier publié — il n'est pas inclus automatiquement
5. Vérifier que le compte du pool d'applications IIS a les droits d'accès à la base SQL Server et que l'authentification Windows (AD/SSO) est bien activée au niveau du site IIS

## 7. Dépendances externes non configurées (à activer plus tard)

| Fonctionnalité | Ce qui manque |
|---|---|
| Module 9 — Notifications email | Paramètres SMTP Socota (serveur, port, compte d'envoi) |
| Stockage réseau des pièces jointes | Accès à \\fileserver\IT\Digitalisation\ ou SharePoint |
| Power BI Embedded | Licence et espace de travail Power BI Socota |

## 8. Structure du projet

GestionProjetSocota/
├── Controllers/ — logique de traitement des requêtes
├── Models/ — entités de données
├── ViewModels/ — objets liés aux formulaires
├── Views/ — pages Razor (.cshtml)
├── Services/ — logique métier (WorkflowService, GeminiService, ScoreRisqueService)
├── Middlewares/ — synchronisation AD
├── Data/ — Entity Framework (ApplicationDbContext)
├── Migrations/ — historique des changements de structure de base de données
└── wwwroot/ — fichiers statiques, pièces jointes uploadées