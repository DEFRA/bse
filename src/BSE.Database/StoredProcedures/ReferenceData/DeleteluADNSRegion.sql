
CREATE PROCEDURE DeleteluADNSRegion
    @ID int
AS
    DECLARE 
        @ErrorCode int, 
        @RowsUpdated int
    
    DELETE FROM luADNSRegion WHERE [ID]=@ID
        
    SELECT @ErrorCode = @@ERROR, @RowsUpdated = @@ROWCOUNT
    
    IF @ErrorCode = 0 BEGIN
        IF @RowsUpdated = 0 BEGIN
            RETURN -1
        END ELSE BEGIN
            RETURN 0
        END
    END ELSE BEGIN
        RETURN @ErrorCode
    END
