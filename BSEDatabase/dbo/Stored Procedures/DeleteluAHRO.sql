
CREATE  PROCEDURE DeleteluAHRO
    @Name [varchar] (100)
AS    
    DECLARE     
        @ErrorCode int,     
        @RowsUpdated int    
        
    DELETE FROM luAHRO WHERE [Name]=@Name
            
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

