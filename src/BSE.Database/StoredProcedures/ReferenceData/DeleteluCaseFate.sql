
CREATE PROCEDURE DeleteluCaseFate
    @Code varchar(4)
AS
    DECLARE 
        @ErrorCode int, 
        @RowsUpdated int
    
    DELETE FROM luCaseFate WHERE [Code]=@Code
        
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
