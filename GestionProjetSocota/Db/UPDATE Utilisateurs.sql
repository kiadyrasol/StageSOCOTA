UPDATE Utilisateurs SET Role = 0 WHERE NomADUtilisateur = 'SOCOTA\kiady.info';
SELECT Id, NomADUtilisateur, Nom, Email, Role, EstActif 
FROM Utilisateurs;