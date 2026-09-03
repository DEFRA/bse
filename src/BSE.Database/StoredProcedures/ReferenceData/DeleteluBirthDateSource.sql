
CREATE PROCEDURE DeleteluBirthDateSource
    @Code varchar(5)
AS
    DECLARE 
        @ErrorCode int, 
        @RowsUpdated int
    
    DELETE FROM luBirthDateSource WHERE [Code]=@Code
        
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
