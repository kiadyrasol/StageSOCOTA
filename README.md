# StageSOCOTA
#Jour 1 apres avoir recu le RFC (12/08/2026)
================================================================================
          COMPTE-RENDU D'AVANCEMENT - PROJET GESTIONPROJETSOCOTA
================================================================================

1. FINALISATION DU MODULE 1 (CRUD PROJETS)
--------------------------------------------------------------------------------
• Fonctionnalités fondamentales de gestion des projets finalisées et testées.
• Opérations CRUD complètes : Création, consultation, modification et suppression 
  via 'ProjetController.cs'.
• Correction appliquée : Champ 'Description' rendu optionnel.

2. AUTHENTIFICATION ACTIVE DIRECTORY / SSO
--------------------------------------------------------------------------------
• Intégration de l'authentification Windows native (Microsoft.AspNetCore.Authentication.Negotiate).
• Connexion transparente via le compte de domaine (testé avec SOCOTA\kiady.info).
• Suppression de l'ancien système de login local.

3. SYNCHRONISATION AUTOMATIQUE DES UTILISATEURS
--------------------------------------------------------------------------------
• Mise en place du middleware 'SyncUtilisateurMiddleware'.
• Création automatique du profil en BDD lors de la première connexion d'un utilisateur,
  avec attribution du rôle par défaut "Lecteur".

4. NETTOYAGE DU CODE & ENVIRONNEMENT
--------------------------------------------------------------------------------
• Suppression des fichiers obsolètes (AccountController.cs, LoginViewModel.cs, Views/Account/).
• Installation de l'extension SQL Server dans VS Code pour la vérification directe des données.

5. ARCHIVAGE GIT & TAG DE VERSION
--------------------------------------------------------------------------------
• Creation du commit selon les normes Conventional Commits.
• Marquage de la version v1.1.0 (version mineure validant le Module 1 et la sécurité AD).

================================================================================
                          PROCHAINES ÉTAPES PRÉVUES
================================================================================
1. Sécurisation par rôles : Restreindre Create/Edit/Delete aux rôles Admin et Chef de Projet.
2. Module 2 (Workflow) : Gestion des étapes et processus de validation.