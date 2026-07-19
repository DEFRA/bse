
CREATE PROCEDURE DeleteFarmRelation
	@ID int,
	@RowStamp timestamp AS

	DELETE
		[FarmRelation]
	WHERE
		[ID] = @ID AND
		[RowStamp] = @RowStamp
