
CREATE  PROCEDURE GetluAHOCode AS  
  
SELECT  
 [Code],  
 [Name],  
 [BSERegionID]
FROM  
  luAHO
ORDER BY  
 [Name]    
RETURN  



