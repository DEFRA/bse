
CREATE PROCEDURE GetClinicalVisitByRBSE
	@RBSE char(9)
AS

	SELECT
		[ID],
		[RBSE],
		[VisitDate],
		[RowStamp]
	FROM
		[ClinicalVisit]
	WHERE
		RBSE = @RBSE
	
	RETURN
