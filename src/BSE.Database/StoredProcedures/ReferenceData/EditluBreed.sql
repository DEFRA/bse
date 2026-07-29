
CREATE PROCEDURE EditluBreed
	@Original_Code  varchar(20),
	@Code varchar(20),
	@FullName varchar(50),
	@AmalgamatedName varchar(50)

AS
	DECLARE 
		@ErrorCode int, 
		@RowsUpdated int
	    
	UPDATE luBreed SET
		[Code] = @Code,
		[FullName] = @FullName,
		[AmalgamatedName] = @AmalgamatedName
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
