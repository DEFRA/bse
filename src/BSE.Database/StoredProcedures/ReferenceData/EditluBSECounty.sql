
CREATE PROCEDURE EditluBSECounty
	@IDColumn char(2),
	@Original_Code char(15),
	@Code char(15),
	@Description varchar(50),
	@BSERegionID int

AS
	DECLARE 
		@ErrorCode int, 
		@RowsUpdated int
	    
	UPDATE luBSECounty SET
		[ID] = @IDColumn,
		[Code]=@Code,
		[Description] = @Description,
		[BSERegionID] = @BSERegionID
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
