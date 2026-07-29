
CREATE PROCEDURE DeleteOtherOwner
	@ID int,
	@RowStamp timestamp AS

	DELETE
		[OtherOwner]
	WHERE
		[ID] = @ID AND
		[RowStamp] = @RowStamp
