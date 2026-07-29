

CREATE  PROCEDURE AddluAHRO
    @Name varchar(100),     
	@ID int OUTPUT  
AS  
DECLARE  
        @ErrorCode int  

 INSERT INTO luAHRO
          ([Name])  
 VALUES  
  (@Name)            

SET @ErrorCode = @@Error  
      
    IF @ErrorCode = 0 BEGIN  
        SET @ID = SCOPE_IDENTITY()  
        RETURN 0  
    END ELSE BEGIN  
        RETURN @ErrorCode  
    END  

RETURN  

