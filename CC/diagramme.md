```mermaid
classDiagram
    class Utilisateur {
        +int Id
        +string NomADUtilisateur
        +string Nom
        +string Email
        +RoleUtilisateur Role
    }

    class Projet {
        +int Id
        +string TicketId
        +string Nom
        +string Description
        +Unite Unite
        +Departement Departement
        +TypeProjet Type
        +Plateforme Plateforme
        +PrioriteProjet Priorite
        +StatutProjet Statut
        +StatutProjet StatutPrecedent
        +int PourcentageAvancement
        +DateTime DateCreation
        +DateTime Deadline
    }

    class RFC {
        +int Id
        +string BusinessCase
        +decimal RoiEstime
        +string GainsAttendus
        +string Priorite
        +bool EstValide
    }

    class ActionProjet {
        +int Id
        +string Description
        +DateTime DateEcheance
        +StatutAction Statut
    }

    class Commentaire {
        +int Id
        +string Contenu
        +DateTime DatePublication
    }

    class PieceJointe {
        +int Id
        +string NomFichier
        +string CheminStockage
        +TypePieceJointe Type
    }

    class HistoriqueProjet {
        +int Id
        +string TypeAction
        +string Detail
        +DateTime DateAction
    }

    class WorkflowService {
        +GetTransitionsPossibles(statut, statutPrecedent, type) List
    }

    class ScoreRisqueService {
        +CalculerScore(projet, dernierCommentaire) int
    }

    class GeminiService {
        +GenererCompteRendu(prompt) string
    }

    Utilisateur "1" --> "*" Projet : OwnerIT gère
    Utilisateur "1" --> "*" Projet : PowerUser suit
    Projet "1" --> "0..1" RFC : associé à
    RFC "*" --> "1" Utilisateur : Champion
    RFC "*" --> "1" Utilisateur : Sponsor
    Projet "1" --> "*" ActionProjet : contient
    ActionProjet "*" --> "1" Utilisateur : Responsable
    Projet "1" --> "*" Commentaire : reçoit
    Commentaire "*" --> "1" Utilisateur : Auteur
    Projet "1" --> "*" PieceJointe : contient
    Projet "1" --> "*" HistoriqueProjet : trace
    HistoriqueProjet "*" --> "1" Utilisateur : Auteur
    WorkflowService ..> Projet : gère les transitions
    ScoreRisqueService ..> Projet : calcule le risque
    GeminiService ..> Projet : génère un compte-rendu
    ```