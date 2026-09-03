


CREATE   PROCEDURE GetluAHRO AS  
  
DECLARE @ttblAHRO TABLE  
  (  
  [ttblID] [int] IDENTITY(1, 1),  
  [ID] [int],  
  [Name] [varchar] (100)  
  )  
  
 SET NOCOUNT ON  
  
 INSERT INTO @ttblAHRO
  (  
  [ID],
  [Name]
  )  
 SELECT  
  [Id],
  [Name]
 FROM  
  luAHRO
 ORDER BY  
  [Name]  
  
 SELECT  
  [ttblID],  
  [ID],  
  [Name]    
 FROM  
  @ttblAHRO
  
 SET NOCOUNT OFF  
  
RETURN  



