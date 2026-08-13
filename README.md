# StageSOCOTA
#Jour 1 apres avoir recu le RFC (12/08/2026)
================================================================================
          COMPTE-RENDU D'AVANCEMENT - PROJET GESTIONPROJETSOCOTA
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
                          PROCHAINES ÉTAPES PRÉVUES
================================================================================
1. Module 2 (Workflow) : modélisation des transitions de statut (branches
   in-house et outsourced), interface de changement de statut, historisation
   via StatutPrecedent.