
CREATE PROCEDURE EditluSupplier
    @ID int,
    @Name varchar(30),
    @Details varchar(60)
AS
    DECLARE 
        @ErrorCode int, 
        @RowsUpdated int
    
    UPDATE luSupplier SET
        [Name]=@Name,
        [Details]=@Details
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
