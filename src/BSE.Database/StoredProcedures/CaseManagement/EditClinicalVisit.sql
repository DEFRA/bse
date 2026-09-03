
CREATE PROCEDURE EditClinicalVisit
	@ID int,
	@VisitDate datetime,
	@RowStamp timestamp AS

	UPDATE
		[ClinicalVisit]
	SET
		[VisitDate] = @VisitDate
	WHERE
		[ID] = @ID AND
		[RowStamp] = @RowStamp
