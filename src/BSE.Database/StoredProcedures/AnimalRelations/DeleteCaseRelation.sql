
CREATE PROCEDURE DeleteCaseRelation
	@ID int,
	@RowStamp timestamp AS

	DELETE
		[CaseRelation]
	WHERE
		[ID] = @ID AND
		[RowStamp] = @RowStamp
