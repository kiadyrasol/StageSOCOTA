BEGIN TRANSACTION;

BEGIN TRY

    -- 1. Supprimer les données liées aux projets
    DELETE FROM Actions;
    DELETE FROM Commentaires;
    DELETE FROM PiecesJointes;
    DELETE FROM RFCs;
    DELETE FROM HistoriqueProjets;
    DELETE FROM Notifications;

    -- 2. Supprimer les projets
    DELETE FROM Projets;

    -- 3. Valider
    COMMIT TRANSACTION;

    PRINT 'Tous les projets et leurs données associées ont été supprimés avec succès.';

END TRY
BEGIN CATCH

    -- En cas d'erreur, tout est annulé
    ROLLBACK TRANSACTION;

    PRINT 'ERREUR : aucune suppression définitive n''a été effectuée.';
    THROW;

END CATCH;