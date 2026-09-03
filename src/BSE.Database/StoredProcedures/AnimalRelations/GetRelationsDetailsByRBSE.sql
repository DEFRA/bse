
CREATE PROCEDURE GetRelationsDetailsByRBSE
	@RBSE char(9)  AS

	EXEC GetDamDetailsByRBSE @RBSE
	EXEC GetSireDetailsByRBSE @RBSE
	EXEC GetRelationsByRBSE @RBSE

