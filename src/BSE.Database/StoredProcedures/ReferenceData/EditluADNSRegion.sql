
CREATE PROCEDURE EditluADNSRegion
    @ID int,
    @Name varchar(50)
AS
    DECLARE 
        @ErrorCode int, 
        @RowsUpdated int
    
    UPDATE luADNSRegion SET
        [Name]=@Name
    WHERE
        [ID]=@ID
        
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
