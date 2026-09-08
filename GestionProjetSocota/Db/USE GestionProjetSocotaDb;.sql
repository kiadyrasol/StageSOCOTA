SELECT 
    OBJECT_NAME(parent_object_id) AS TableEnfant,
    OBJECT_NAME(referenced_object_id) AS TableParent
FROM sys.foreign_keys
WHERE referenced_object_id = OBJECT_ID('Projets');

SELECT COUNT(*) AS NombreProjets
FROM Projets;