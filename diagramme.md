```mermaid
classDiagram
    class Utilisateur {
        +int Id
        +string Nom
        +string Email
        +string MotDePasse
        +string Role
    }

    class Projet {
        +int Id
        +string Nom
        +string Description
        +DateTime DateDebut
        +DateTime DateFin
        +decimal BudgetAlloue
        +string Statut
    }

    class Tache {
        +int Id
        +string Titre
        +string Description
        +DateTime DateEcheance
        +string Statut
        +string Priorite
    }

    class Budget {
        +int Id
        +decimal MontantAlloue
        +decimal MontantConsomme
    }

    class Document {
        +int Id
        +string Nom
        +string Chemin
        +DateTime DateAjout
    }

    class Risque {
        +int Id
        +string Description
        +string Niveau
        +string Statut
    }

    Utilisateur "1" --> "*" Projet : gère
    Projet "1" --> "*" Tache : contient
    Tache "*" --> "1" Utilisateur : assignée à
    Projet "1" --> "1" Budget : possède
    Projet "1" --> "*" Document : contient
    Projet "1" --> "*" Risque : identifie
    ```