
CREATE PROCEDURE EditUser
	@ID int,
	@NTLogin varchar(25),
	@Name varchar(35),
	@Email varchar(60),
	@UserGroup int,
	@IsActive bit
AS
	DECLARE 
		@ErrorCode int, 
		@RowsUpdated int
    
	UPDATE [User] SET
		[NTLogin] = @NTLogin,
		[Name] = @Name,
		[Email] = @Email,
		[UserGroup] = @UserGroup,
		[IsActive] = @IsActive
	WHERE
		[ID]=@ID
