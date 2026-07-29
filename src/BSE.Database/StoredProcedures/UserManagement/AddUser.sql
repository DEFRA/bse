
CREATE PROCEDURE AddUser
	@NTLogin varchar(25),
	@Name varchar(35),
	@Email varchar(60),
	@UserGroup int,
	@IsActive bit,
	@ID int OUTPUT
AS
	INSERT INTO [User]
		([NTLogin],
		[Name],
		[Email],
		[UserGroup],
		[IsActive])
	VALUES
		(@NTLogin,
		@Name,
		@Email,
		@UserGroup,
		@IsActive)

	SET @ID = SCOPE_IDENTITY()
