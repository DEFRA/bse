

CREATE PROCEDURE [dbo].[DeleteluBSEForm]
    @Code char(2)
AS
    DECLARE 
        @ErrorCode int, 
        @RowsUpdated int
    
    DELETE FROM luBSEForm WHERE [Code]=@Code
        
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
