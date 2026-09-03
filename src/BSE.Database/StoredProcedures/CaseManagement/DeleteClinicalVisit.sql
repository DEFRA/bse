
CREATE PROCEDURE DeleteClinicalVisit
	@ID int,
	@RowStamp timestamp AS

	DELETE
		[ClinicalVisit]
	WHERE
		[ID] = @ID AND
		[RowStamp] = @RowStamp
