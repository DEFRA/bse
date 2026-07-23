
CREATE  PROCEDURE EditluTSETestingSite  
    @Original_CPH char(11),    
    @Name varchar(50),    
    @Address varchar(255),    
    @CPH char(11),  
    @AHO char(2)    
    
AS    
 DECLARE     
  @ErrorCode int,     
  @RowsUpdated int    
         
 UPDATE luTSETestingSite SET    
  [Name] = @Name,    
  [Address] = @Address,    
  [CPH]=@CPH,  
  [AHO]=@AHO     
 WHERE    
  [CPH] = @Original_CPH    
            
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
  


