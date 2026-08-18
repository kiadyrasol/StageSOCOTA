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
  en rôle Administrateur. Version v1.2.0

2. MODULE 2 - WORKFLOW PROJET
--------------------------------------------------------------------------------
- Création de 'WorkflowService.cs' : logique métier centralisant les transitions
  de statut autorisées, avec 2 branches distinctes (In-house / Outsourced) selon
  le type de projet.
- Ajout de l'action 'ChangerStatut' (GET/POST) dans 'ProjetController.cs',
  avec vérification côté serveur des transitions autorisées.
- Statuts spéciaux 'Suspendu' et 'Cancelled' accessibles depuis tout statut actif.
- Historisation automatique via 'StatutPrecedent' à chaque changement.
- Testé : divergence confirmée entre branches In-house et Outsourced après
  'RFCApproved' (Analyse vs Prospection). Version v1.3.0

3. AMÉLIORATION - RÉACTIVATION EXPLICITE
--------------------------------------------------------------------------------
- Ajout d'un bouton "Réactiver le projet" dédié pour les statuts Suspendu/Cancelled,
  pré-rempli avec le StatutPrecedent.

4. MODULE 3 - GESTION RFC
--------------------------------------------------------------------------------
- Création de RFCController.cs : Create (lié à un projet), Valider,
  AnnulerValidation.
- Ajout de ProjetController.Details : page centrale affichant les infos d'un
  projet + son RFC associé.
- Lien Workflow ↔ RFC : la validation d'un RFC fait automatiquement passer le
  projet de WaitingRFC à RFCApproved.
- Annulation de validation possible uniquement si le projet n'a pas encore
  avancé au-delà de RFCApproved. Version v1.4.0
- Décision de périmètre : le projet vise désormais l'intégralité du cahier des
  charges (modules secondaires et bonus inclus), pas seulement le MVP.

5. MODULE 4 - KANBAN / MODULE 5 - DASHBOARD EXÉCUTIF
--------------------------------------------------------------------------------
- Kanban : vue en colonnes par statut (branche In-house comme référence
  d'affichage), cartes cliquables menant vers Details.
- Dashboard : KPIs (total, actifs, terminés, suspendus, en retard), graphiques
  ChartJS (répartition par statut, par unité). Version v1.5.0

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
  Version v1.6.0

================================================================================
#Jour 4 - Assistant IA + Design + Finitions (14-15/08/2026)
================================================================================

1. ASSISTANT IA (API GEMINI)
--------------------------------------------------------------------------------
- GeminiService.cs : appel HTTP à l'API Gemini (modèle gemini-1.5-flash) pour
  générer un compte-rendu automatique par projet (statut, actions, commentaires).
- Action ProjetController.GenererCompteRendu + Vue dédiée.
- Assistant IA repositionné en priorité MVP (initialement en bonus).
- Incident sécurité : clé API exposée dans un commit, bloquée par GitHub Push
  Protection. Clé révoquée et régénérée. Mise en place d'un .gitignore excluant
  appsettings.json du suivi Git (protection définitive).

2. REFONTE DESIGN - ADMINLTE
--------------------------------------------------------------------------------
- Intégration du thème AdminLTE 3.2 + Bootstrap 5 + Font Awesome via CDN.
- Nouveau _Layout.cshtml : sidebar rétractable, navbar, couleur Socota (#228B22).
- Conversion complète de toutes les Vues (styles en ligne → classes Bootstrap/
  AdminLTE) : Index, Create, Edit, Delete, Details, ChangerStatut, Kanban,
  Dashboard, DashboardComex, DashboardItManager, Recherche, GenererCompteRendu,
  RFC/Create, ActionProjet/Create. Version v1.7.0

3. MODE SOMBRE
--------------------------------------------------------------------------------
- Bascule clair/sombre via bouton navbar, persistant (localStorage).

4. SÉLECTEUR DE LANGUE (STRUCTURE TECHNIQUE)
--------------------------------------------------------------------------------
- CultureController + cookie de langue FR/EN en place.
- Traduction complète du contenu texte des Vues non réalisée (décision assumée,
  abandon du chantier de traduction pour prioriser les fonctionnalités).

5. CORRECTIONS RESPONSIVE
--------------------------------------------------------------------------------
- Tableaux enveloppés dans table-responsive (Index, Recherche, IT Manager).
- Page Recherche : media query CSS précise à 650px (empilement des filtres en
  dessous de ce seuil uniquement, sans impact sur l'affichage desktop).
  Version v1.8.0 (mode sombre, langue, responsive)

6. TESTS DE ROBUSTESSE
--------------------------------------------------------------------------------
- MaxLength sur les champs texte des ViewModels (Projet, RFC, Action, Commentaire).
- Vérification limite fichier 20 Mo (bloqué proprement, message clair).
- Vérification sécurité par rôle en accès direct URL (403 confirmé sur toutes
  les routes sensibles testées, même en tapant l'URL directement).
- Système de messages flash (TempData Succes) sur toutes les actions de
  création/modification/suppression (Projet, RFC, Actions, Commentaires,
  Pièces jointes). Version v1.11.0

================================================================================
#Jour 5 - Corrections et Bonus (15-16/08/2026)
================================================================================

1. CORRECTIONS
--------------------------------------------------------------------------------
- Avancement (%) forcé à 0 tant que le projet est en statut WaitingRFC
  (empêchait une incohérence : progression affichée avant validation du RFC).
- Vérification Aging des projets : comportement normal, lié à la remise à
  DateCreation=maintenant lors d'une correction de données antérieure
  (les projets vieilliront naturellement dans les tranches suivantes avec le temps).
- Vérification temps de chargement : 60-250ms observés (F12 → Network),
  largement sous le seuil de 3s du cahier des charges. Critère MVP validé.

2. BONUS - SCORE DE RISQUE AUTOMATIQUE
--------------------------------------------------------------------------------
- ScoreRisqueService.cs : calcul multi-critères pondéré (deadline dépassée +40,
  deadline proche <7j +20, priorité High +20, statut Suspendu +15, absence
  d'Owner IT +10, inactivité 30j+ +15), plafonné à 100.
- 3 niveaux : Faible (0-30), Moyen (31-60), Élevé (61-100).
- Affiché sur Details (score + niveau + barre de progression) et sur Index
  (badge coloré par ligne de projet).

3. BONUS - ROADMAP TRIMESTRIELLE
--------------------------------------------------------------------------------
- Vue Roadmap : 4 trimestres glissants calculés à partir d'aujourd'hui (pas
  l'année civile), projets regroupés selon leur date de deadline.

4. BONUS - GANTT INTERACTIF
--------------------------------------------------------------------------------
- Intégration de la bibliothèque Frappe Gantt (CDN, gratuite, open source).
- Barres cliquables redirigeant vers Details du projet, affichage de
  l'avancement (%) directement sur chaque barre.
- Estimation de date de début à J-14 avant deadline si DateDebut non renseignée.

5. BONUS - DIAGRAMME DE CHARGE ÉQUIPE
--------------------------------------------------------------------------------
- Déjà réalisé lors du Dashboard IT Manager (graphique ChargeParOwnerIt) —
  confirmé comme couvrant ce point du cahier des charges.

6. HISTORISATION RENFORCÉE
--------------------------------------------------------------------------------
- Nouveau Model HistoriqueProjet (ProjetId, UtilisateurId, TypeAction, Detail,
  DateAction) avec table dédiée en base.
- Traçage automatique sur : Create (création), Edit (changement de statut
  détecté), ChangerStatut (transition de workflow).
- Affiché en bas de la page Details de chaque projet, trié du plus récent
  au plus ancien. Version v1.12.0

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
- Module 11 - Sécurité / Rôles (RBAC) ............. FAIT
- Recherche multicritères + Export ................ FAIT
- Assistant IA (API Gemini) ........................ FAIT
- Design AdminLTE + Mode sombre + Responsive ....... FAIT
- Tests de robustesse .............................. FAIT
- Score de risque automatique ...................... FAIT (BONUS)
- Roadmap trimestrielle ............................ FAIT (BONUS)
- Gantt interactif .................................. FAIT (BONUS)
- Diagramme de charge équipe ........................ FAIT (BONUS)
- Historisation renforcée ........................... FAIT
- Power BI Embedded ................................. EN ATTENTE (licence Socota requise)
- Sélecteur de langue (structure) ................... FAIT (traduction du contenu non réalisée)

================================================================================
                          HISTORIQUE DES VERSIONS GIT
================================================================================
v1.1.0  - Module 1 (CRUD) + Authentification AD/SSO
v1.2.0  - Sécurité RBAC
v1.3.0  - Module 2 (Workflow)
v1.4.0  - Module 3 (RFC)
v1.5.0  - Modules 4-5 (Kanban + Dashboard Exécutif)
v1.6.0  - Modules 7-8-10 + Dashboards COMEX/IT Manager + Recherche/Export
v1.7.0  - Assistant IA + Design AdminLTE
v1.8.0  - Mode sombre + Langue + Responsive
v1.11.0 - Tests de robustesse
v1.12.0 - Bonus (score de risque, roadmap, Gantt) + Historisation renforcée

================================================================================
                          PROCHAINES ÉTAPES PRÉVUES
================================================================================
1. Documentation finale :
   - Rapport de stage
   - Guide utilisateur PDF avec captures d'écran
   - README technique de déploiement (installation, configuration BDD,
     déploiement IIS, structure appsettings.json attendue puisque ce fichier
     n'est plus suivi par Git)
2. Module 9 (Notifications email) dès obtention des paramètres SMTP Socota.
3. Power BI Embedded dès obtention d'un accès licence Socota (sinon documenté
   comme non réalisé pour dépendance externe manquante).

================================================================================
                    DÉPENDANCES EXTERNES EN ATTENTE (SOCOTA)
================================================================================
- Paramètres SMTP (serveur, port, compte d'envoi) → Module 9
- Accès réseau \\fileserver\IT\Digitalisation\ ou SharePoint → Module 10 (amélioration)
- Licence/espace de travail Power BI → Power BI Embedded