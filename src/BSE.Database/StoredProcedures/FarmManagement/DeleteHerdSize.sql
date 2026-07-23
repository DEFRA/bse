
CREATE PROCEDURE DeleteHerdSize
	@ID int,
	@RowStamp timestamp  AS

	DELETE
		[HerdSize]
	WHERE
		[ID] = @ID AND
		[RowStamp] = @RowStamp

