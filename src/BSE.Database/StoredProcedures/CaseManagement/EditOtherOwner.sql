
CREATE PROCEDURE EditOtherOwner
	@ID int,
	@Type varchar(10),
	@Name varchar(150),
	@CPHH char(11),
	@RowStamp timestamp AS

	UPDATE
		[OtherOwner]
	SET
		[Type] = @Type,
		[Name] = @Name,
		[CPHH] = @CPHH
	WHERE
		[ID] = @ID AND
		[RowStamp] = @RowStamp
