
CREATE PROCEDURE EditluSex
	@Original_Code char(1),
	@Code char(1),
	@Description varchar(50)
AS
	DECLARE 
		@ErrorCode int, 
		@RowsUpdated int
	    
	UPDATE luSex SET
		[Description]=@Description,
		[Code]=@Code
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
