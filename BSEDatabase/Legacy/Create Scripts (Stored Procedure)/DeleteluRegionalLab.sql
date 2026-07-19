set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


CREATE PROCEDURE [dbo].[DeleteluRegionalLab]
    @Code char(4)
AS
    DECLARE 
        @ErrorCode int, 
        @RowsUpdated int
    
    DELETE FROM luRegionalLab WHERE [Code]=@Code
        
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

 