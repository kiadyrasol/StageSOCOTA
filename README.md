# StageSOCOTA

================================================================================
          COMPTE-RENDU D'AVANCEMENT - PROJET GESTIONPROJETSOCOTA
================================================================================

================================================================================
#Jour 1 apres avoir recu le RFC (12/08/2026)
================================================================================

1. FINALISATION DU MODULE 1 (CRUD PROJETS)
--------------------------------------------------------------------------------
- Fonctionnalités fondamentales de gestion des projets finalisées et testées.
- Opérations CRUD complètes : Création, consultation, modification et suppression 
  via 'ProjetController.cs'.
- Correction appliquée : Champ 'Description' rendu optionnel.

2. AUTHENTIFICATION ACTIVE DIRECTORY / SSO
--------------------------------------------------------------------------------
- Intégration de l'authentification Windows native (Microsoft.AspNetCore.Authentication.Negotiate).
- Connexion transparente via le compte de domaine (testé avec SOCOTA\kiady.info).
- Suppression de l'ancien système de login local.

3. SYNCHRONISATION AUTOMATIQUE DES UTILISATEURS
--------------------------------------------------------------------------------
- Mise en place du middleware 'SyncUtilisateurMiddleware'.
- Création automatique du profil en BDD lors de la première connexion d'un utilisateur,
  avec attribution du rôle par défaut "Lecteur".

4. NETTOYAGE DU CODE & ENVIRONNEMENT
--------------------------------------------------------------------------------
- Suppression des fichiers obsolètes (AccountController.cs, LoginViewModel.cs, Views/Account/).
- Installation de l'extension SQL Server dans VS Code pour la vérification directe des données.

5. ARCHIVAGE GIT & TAG DE VERSION
--------------------------------------------------------------------------------
- Creation du commit selon les normes Conventional Commits.
- Marquage de la version v1.1.0 (version mineure validant le Module 1 et la sécurité AD).

================================================================================
#Jour 2 (13/08/2026)
================================================================================

1. SÉCURISATION PAR RÔLES (RBAC)
--------------------------------------------------------------------------------
- Restriction des actions Create/Edit aux rôles Administrateur et ChefDeProjet,
  et Delete au rôle Administrateur uniquement, via '[Authorize(Roles = "...")]'.
- Correction d'un bug d'authentification Windows : le rôle applicatif (table
  Utilisateurs) n'était pas reconnu par '[Authorize]' car Windows utilise par
  défaut le type de claim 'groupsid' plutôt que 'ClaimTypes.Role'.
- Mise à jour de 'SyncUtilisateurMiddleware' : reconstruction explicite de
  l'identité ('ClaimsIdentity') en forçant 'ClaimTypes.Role' comme type de rôle
  de référence, pour que le rôle métier soit bien pris en compte par [Authorize].
- Testé dans les deux sens : accès refusé (403) en rôle Lecteur, accès autorisé
  en rôle Administrateur.

================================================================================
#Jour 2 (suite) - Module 2 Workflow (13/08/2026)
================================================================================

1. MODULE 2 - WORKFLOW PROJET
--------------------------------------------------------------------------------
- Création de 'WorkflowService.cs' : logique métier centralisant les transitions
  de statut autorisées, avec 2 branches distinctes (In-house / Outsourced) selon
  le type de projet.
- Ajout de l'action 'ChangerStatut' (GET/POST) dans 'ProjetController.cs',
  avec vérification côté serveur des transitions autorisées (sécurité : la
  validation ne repose jamais uniquement sur l'interface).
- Statuts spéciaux 'Suspendu' et 'Cancelled' accessibles depuis tout statut actif.
- Historisation automatique via 'StatutPrecedent' à chaque changement.
- Testé : divergence confirmée entre branches In-house et Outsourced après
  'RFCApproved' (Analyse vs Prospection).

2. AMÉLIORATION - RÉACTIVATION EXPLICITE
--------------------------------------------------------------------------------
- Ajout d'un bouton "Réactiver le projet" dédié pour les statuts Suspendu/Cancelled,
  pré-rempli avec le StatutPrecedent (au lieu d'un menu déroulant générique à une
  seule option).

3. MODULE 3 - GESTION RFC
--------------------------------------------------------------------------------
- Création de RFCController.cs : Create (lié à un projet), Valider,
  AnnulerValidation.
- Ajout de ProjetController.Details : page centrale affichant les infos d'un
  projet + son RFC associé (base pour les futurs modules Actions/Commentaires/
  Pièces jointes).
- Lien Workflow ↔ RFC : la validation d'un RFC fait automatiquement passer le
  projet de WaitingRFC à RFCApproved.
- Annulation de validation possible uniquement si le projet n'a pas encore
  avancé au-delà de RFCApproved (évite les incohérences de rollback).
- Décision de périmètre : le projet vise désormais l'intégralité du cahier des
  charges (modules secondaires et bonus inclus), pas seulement le MVP.

4. MODULE 4 - KANBAN
--------------------------------------------------------------------------------
- Vue en colonnes par statut (branche In-house comme référence d'affichage),
  cartes cliquables menant vers Details.

5. MODULE 5 - DASHBOARD EXÉCUTIF
--------------------------------------------------------------------------------
- KPIs : total, actifs, terminés, suspendus, en retard.
- Graphiques ChartJS : répartition par statut (barres), par unité (anneau).
- DashboardViewModel avec regroupement via GroupBy.

================================================================================
#Jour 3 - Modules 7,8,10, Dashboards COMEX/IT Manager, Recherche, Export (14/08/2026)
================================================================================

1. MODULE 7 - ACTIONS
--------------------------------------------------------------------------------
- ActionProjetController : Create, ChangerStatut, Delete.
- Intégration dans Projet/Details avec boutons rapides (En cours/Clôturer/Supprimer).

2. MODULE 8 - COMMENTAIRES
--------------------------------------------------------------------------------
- CommentaireController : Create (auteur = identité AD connectée automatiquement),
  Delete (Administrateur uniquement).
- Restreint aux rôles Administrateur, ChefDeProjet, PowerUser (conforme section 11).

3. MODULE 10 - PIÈCES JOINTES
--------------------------------------------------------------------------------
- PieceJointeController : upload avec limite 20 Mo, 6 types (RFC, MOM, Analyse,
  Cahier des charges, Capture écran, Plan de tests).
- Stockage local (wwwroot/uploads) en attendant un accès réseau/SharePoint Socota.

4. DASHBOARDS COMEX ET IT MANAGER - REFONTE COMPLÈTE
--------------------------------------------------------------------------------
- COMEX : Portfolio Health (Vert/Orange/Rouge selon retard), répartition
  In House/Outsourced, répartition par plateforme.
- IT Manager : charge équipe, projets critiques (Priorité=High), deadlines des
  30 prochains jours, aging des projets (tranches 0-30/31-60/61-90/90+).
- Correction : Priorite transformé de string en enum (Low/Medium/High).
- Correction : ajout de Projet.DateCreation (nécessaire pour l'aging).
- Correction : bug d'affichage ChartJS (canvas sans hauteur fixe → invisible).

5. RECHERCHE MULTICRITÈRES ET EXPORT
--------------------------------------------------------------------------------
- Filtres combinables : unité, département, statut, type, responsable IT.
- Sélections persistantes dans les filtres après recherche (via Query String).
- Export Excel (ClosedXML) et PDF (QuestPDF), respectant les filtres actifs.

6. SÉCURITÉ - CORRECTIONS DE CONFORMITÉ
--------------------------------------------------------------------------------
- RFC (Valider/AnnulerValidation) ouvert au rôle PowerUser.
- Commentaires restreints à Administrateur/ChefDeProjet/PowerUser (Lecteur exclu).

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
- Assistant IA (API Gemini) ........................ REPOSITIONNÉ EN PRIORITÉ MVP, à faire

================================================================================
                          PROCHAINES ÉTAPES PRÉVUES
================================================================================
1. Assistant IA (API Gemini) - priorité MVP.
2. Module 9 (Notifications email) - dès obtention des paramètres SMTP Socota.
3. Finition design AdminLTE + correction responsive (bug identifié sur iPhone 14 Pro Max).
4. Tests de robustesse (cas extrêmes, fichiers volumineux, textes longs, accès non autorisés).
5. Documentation (rapport de stage, guide utilisateur PDF, README technique de déploiement).
6. Bonus restants : Gantt interactif, roadmap trimestrielle, score de risque automatique.
   Power BI Embedded en attente d'un accès licence Socota.