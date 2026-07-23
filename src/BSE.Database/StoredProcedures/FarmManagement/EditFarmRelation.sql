
CREATE PROCEDURE EditFarmRelation
	@ID int,
	@RelatedCPHH char(11),
	@RowStamp timestamp AS

	UPDATE
		[FarmRelation]
	SET
		[RelatedCPHH] = @RelatedCPHH
	WHERE
		[ID] = @ID AND
		[RowStamp] = @RowStamp
