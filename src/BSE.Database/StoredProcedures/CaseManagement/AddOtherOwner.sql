
CREATE PROCEDURE AddOtherOwner
	@RBSE char(9),
	@Type varchar(10),
	@Name varchar(150),
	@CPHH char(11) AS

	INSERT INTO [OtherOwner]
		(
		[RBSE],
		[Type],
		[Name],
		[CPHH]
		)
	VALUES
		(
		@RBSE,
		@Type,
		@Name,
		@CPHH
		)
