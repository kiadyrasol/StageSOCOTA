# StageSOCOTA

================================================================================
          COMPTE-RENDU D'AVANCEMENT - PROJET GESTIONPROJETSOCOTA
================================================================================

================================================================================
#Jour 1 apres avoir recu le RFC (12/08/2026)
================================================================================

1. FINALISATION DU MODULE 1 (CRUD PROJETS)
--------------------------------------------------------------------------------
- CRUD complet via ProjetController.cs. Champ Description rendu optionnel.

2. AUTHENTIFICATION ACTIVE DIRECTORY / SSO
--------------------------------------------------------------------------------
- Microsoft.AspNetCore.Authentication.Negotiate, testé avec SOCOTA\kiady.info.
- Suppression du login local.

3. SYNCHRONISATION AUTOMATIQUE DES UTILISATEURS
--------------------------------------------------------------------------------
- SyncUtilisateurMiddleware : création auto en BDD, rôle "Lecteur" par défaut.

4. NETTOYAGE & ENVIRONNEMENT
--------------------------------------------------------------------------------
- Suppression AccountController.cs, LoginViewModel.cs, Views/Account/.
- Extension SQL Server dans VS Code.

5. ARCHIVAGE GIT
--------------------------------------------------------------------------------
- v1.1.0 : Module 1 + sécurité AD.

================================================================================
#Jour 2 (13/08/2026)
================================================================================

1. SÉCURISATION PAR RÔLES (RBAC)
--------------------------------------------------------------------------------
- [Authorize(Roles = "...")] sur Create/Edit (Admin+ChefDeProjet) et Delete (Admin).
- Correction RoleClaimType (Windows utilise groupsid, pas ClaimTypes.Role) via
  reconstruction de l'identité dans SyncUtilisateurMiddleware.
- Testé dans les 2 sens (Lecteur bloqué, Administrateur autorisé). v1.2.0

2. MODULE 2 - WORKFLOW
--------------------------------------------------------------------------------
- WorkflowService.cs : transitions par branche In-house / Outsourced.
- ChangerStatut avec vérification serveur, historisation via StatutPrecedent.
- Statuts spéciaux Suspendu/Cancelled + réactivation explicite. v1.3.0

3. MODULE 3 - GESTION RFC
--------------------------------------------------------------------------------
- RFCController : Create, Valider (déclenche WaitingRFC->RFCApproved),
  AnnulerValidation (protégée si le projet a avancé).
- ProjetController.Details créé comme page centrale. v1.4.0

4. MODULE 4 - KANBAN / MODULE 5 - DASHBOARD EXÉCUTIF
--------------------------------------------------------------------------------
- Kanban en colonnes par statut. Dashboard avec KPIs + ChartJS. v1.5.0

================================================================================
#Jour 3 (14/08/2026)
================================================================================

1. MODULE 7 - ACTIONS / MODULE 8 - COMMENTAIRES / MODULE 10 - PIÈCES JOINTES
--------------------------------------------------------------------------------
- Actions : Create, ChangerStatut, Delete.
- Commentaires : auteur = identité AD auto, restreint Admin/ChefDeProjet/PowerUser.
- Pièces jointes : upload 20 Mo max, 6 types, stockage local (réseau/SharePoint
  Socota en attente d'accès).

2. DASHBOARDS COMEX / IT MANAGER - REFONTE
--------------------------------------------------------------------------------
- COMEX : Portfolio Health (Vert/Orange/Rouge), In House/Outsourced, plateforme.
- IT Manager : charge équipe, projets critiques, deadlines 30j, aging.
- Priorite en enum, ajout DateCreation, fix bug ChartJS (canvas hauteur 0).

3. RECHERCHE MULTICRITÈRES + EXPORT
--------------------------------------------------------------------------------
- Filtres combinables, sélections persistantes, export Excel (ClosedXML) et
  PDF (QuestPDF).

4. CORRECTIONS SÉCURITÉ
--------------------------------------------------------------------------------
- RFC ouvert à PowerUser. Commentaires restreints (Lecteur exclu). v1.6.0

================================================================================
#Jour 4 (14-15/08/2026)
================================================================================

1. ASSISTANT IA (API GEMINI)
--------------------------------------------------------------------------------
- GeminiService.cs, génération de compte-rendu par projet. Repositionné en MVP.

2. REFONTE DESIGN - ADMINLTE
--------------------------------------------------------------------------------
- AdminLTE 3.2 + Bootstrap 5 + Font Awesome. Nouveau _Layout.cshtml.
- Conversion de toutes les Vues au style Bootstrap/AdminLTE. v1.7.0

3. MODE SOMBRE
--------------------------------------------------------------------------------
- Bascule clair/sombre via bouton navbar, persistant (localStorage).

4. SÉLECTEUR DE LANGUE (STRUCTURE)
--------------------------------------------------------------------------------
- CultureController + cookie de langue FR/EN en place.
- Traduction du contenu texte des Vues non encore faite (à prévoir).

5. CORRECTIONS RESPONSIVE
--------------------------------------------------------------------------------
- Tableaux enveloppés dans table-responsive (Index, Recherche, IT Manager).
- Filtres de recherche adaptés en grille mobile (col-6 col-md-2).

================================================================================
                    ÉTAT D'AVANCEMENT DES MODULES (CAHIER DES CHARGES)
================================================================================
- Module 1  - Gestion des projets ................ FAIT
- Module 2  - Workflow ............................ FAIT
- Module 3  - Gestion RFC ......................... FAIT
- Module 4  - Kanban .............................. FAIT
- Module 5  - Dashboard Exécutif .................. FAIT
- Module 5b - Dashboard COMEX ..................... FAIT
- Module 6  - Dashboard IT Manager ................ FAIT
- Module 7  - Actions ............................. FAIT
- Module 8  - Commentaires ........................ FAIT
- Module 9  - Notifications (email) ............... EN ATTENTE (accès SMTP Socota requis)
- Module 10 - Pièces jointes ...................... FAIT (stockage local, réseau/SharePoint à prévoir)
- Module 11 - Sécurité / Rôles .................... FAIT
- Recherche multicritères + Export ................ FAIT
- Assistant IA (API Gemini) ........................ FAIT
- Design AdminLTE + Mode sombre .................... FAIT
- Sélecteur de langue (structure technique) ........ FAIT (traduction du contenu à faire)
- Responsive ........................................ EN COURS (corrigé sur pages principales)

================================================================================
                          PROCHAINES ÉTAPES PRÉVUES
================================================================================
1. Tests de robustesse (cas extrêmes, accès non autorisés, messages de confirmation).
2. Documentation (rapport de stage, guide utilisateur, README technique).
3. Module 9 dès accès SMTP.
4. Bonus restants (Gantt, roadmap, score de risque). Power BI Embedded en attente.
5. Traduction complète FR/EN du contenu si le temps le permet.