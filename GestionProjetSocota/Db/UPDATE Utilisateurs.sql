UPDATE Utilisateurs 
SET Role = 3 
WHERE NomADUtilisateur = 'kiady.info';

SELECT
    t.name AS TableName,
    SUM(p.rows) AS NombreDeLignes
FROM sys.tables t
INNER JOIN sys.partitions p
    ON t.object_id = p.object_id
WHERE p.index_id IN (0, 1)
GROUP BY t.name
ORDER BY t.name;