UPDATE Projets
SET DateCreation = GETDATE()
WHERE DateCreation = '0001-01-01';