BEGIN TRANSACTION;

BEGIN TRY

    -- Supprimer les notifications liées aux projets
    DELETE FROM Notifications;

    -- Supprimer les commentaires liés aux projets
    DELETE FROM Commentaires;

    -- Supprimer l'historique lié aux projets
    DELETE FROM HistoriqueProjets;

    -- Supprimer les projets
    DELETE FROM Projets;

    -- Réinitialiser l'identité de Projets
    DBCC CHECKIDENT ('Projets', RESEED, 0);

    -- Réinitialiser les compteurs de références
    DELETE FROM ReferenceCompteurs;

    COMMIT TRANSACTION;

    PRINT 'Nettoyage terminé avec succès.';

END TRY
BEGIN CATCH

    ROLLBACK TRANSACTION;
    THROW;

END CATCH;