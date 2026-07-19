
CREATE PROCEDURE EditluTestType

	@Original_Code varchar(10),
	@Code varchar(10),
	@Description varchar(50),
	@IsActive bit

AS
	DECLARE 
		@ErrorCode int, 
		@RowsUpdated int
	    
	UPDATE luTestType SET
		[Code] = @Code,
		[Description] = @Description,
		[IsActive] = @IsActive
	WHERE
		[Code] = @Original_Code
        
	SELECT 
		@ErrorCode = @@ERROR, 
		@RowsUpdated = @@ROWCOUNT
    
	IF @ErrorCode = 0 BEGIN
		IF @RowsUpdated = 0 BEGIN
			RETURN -1
		END ELSE BEGIN
			RETURN 0
		END
	END ELSE BEGIN
		RETURN @ErrorCode
    	END
