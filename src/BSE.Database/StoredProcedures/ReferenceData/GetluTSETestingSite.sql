


CREATE   PROCEDURE GetluTSETestingSite AS  
  
DECLARE @ttblTSETestingSite TABLE  
  (  
  [ID] int IDENTITY(1, 1),  
  [Name] varchar(50),  
  [Address] varchar(255),  
  [CPH] char(11),
  [AHO] char(2)
  )  
  
 SET NOCOUNT ON  
  
 INSERT INTO @ttblTSETestingSite
  (  
  [Name],
  [Address],
  [CPH],
  [AHO]
  )  
 SELECT  
  [Name],  
  [Address],  
  [CPH],
  [AHO]  
 FROM  
  luTseTestingSite  
 ORDER BY  
  [Name]  
  
 SELECT  
  [ID],  
  [Name],  
  [Address],  
  [CPH],
  [AHO]  
 FROM  
  @ttblTSETestingSite  
  
 SET NOCOUNT OFF  
  
RETURN  



