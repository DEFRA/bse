

CREATE  PROCEDURE AddluTSETestingSite  
    @Name varchar(50), 
    @Address varchar(255),  
    @CPH char(11),
    @AHO char(2)
AS  
 INSERT INTO [luTseTestingSite]  
          ([Name],[Address],[CPH],[AHO])  
 VALUES  
  (@Name,@Address,@CPH,@AHO)            
RETURN  


