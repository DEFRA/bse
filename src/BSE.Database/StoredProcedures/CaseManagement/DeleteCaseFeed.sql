
CREATE PROCEDURE DeleteCaseFeed
	@ID int,
	@RowStamp timestamp AS

	DELETE
		[CaseFeed]
	WHERE
		[ID] = @ID AND
		[RowStamp] = @RowStamp
