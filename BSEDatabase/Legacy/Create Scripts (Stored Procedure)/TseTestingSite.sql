--1. Get luAHOCode
SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

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



GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO



--2. GetluTseTestingSite
SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO



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



GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO



--3. AddluTseTesting
SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO


CREATE  PROCEDURE AddluTSETestingSite  
    @Name varchar(50), 
    @Address varchar(255),  
    @CPH char(11),
    @AHO char(2)
AS  
 INSERT INTO luTSETestingSITE  
          ([Name],[Address],[CPH],[AHO])  
 VALUES  
  (@Name,@Address,@CPH,@AHO)            
RETURN  


GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO



--4.EditluTseTesting
SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

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
  


GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO



--5.DeleteluTseTestingSite
SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

CREATE  PROCEDURE DeleteluTSETestingSite  
    @CPH char(11)    
AS    
    DECLARE     
        @ErrorCode int,     
        @RowsUpdated int    
        
    DELETE FROM luTSETestingSite WHERE [CPH]=@CPH    
            
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

GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO




