--create luAHRO table

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[luAHRO](
	[Id] [int] NOT NULL IDENTITY(1,1),
	[Name] [varchar](100) NOT NULL,
	CONSTRAINT [PK_luAHRO] PRIMARY KEY  CLUSTERED 
	(
		[Id]
	 )
) ON [PRIMARY]

GO

--insert data in luAHRO table

INSERT INTO luAHRO ([Name]) VALUES ('South West Animal Health Regional Office')
INSERT INTO luAHRO ([Name]) VALUES ('East of England Animal Health Regional Office')
INSERT INTO luAHRO ([Name]) VALUES ('East Midlands Animal Health Regional Office')
INSERT INTO luAHRO ([Name]) VALUES ('Yorkshire and Humber Animal Health Regional Office')
INSERT INTO luAHRO ([Name]) VALUES ('West Midlands Animal Health Regional Office')
INSERT INTO luAHRO ([Name]) VALUES ('North East Animal Health Regional Office')
INSERT INTO luAHRO ([Name]) VALUES ('North West Animal Health Regional Office')
INSERT INTO luAHRO ([Name]) VALUES ('South East Animal Health Regional Office')


--Insert info about luAHRO table in [dbo].[EditableLookup] table
INSERT INTO [dbo].[EditableLookUp] VALUES (28,'luAHRO','AHRO','GetluAHRO','EditluAHRO','AddluAHRO','DeleteluAHRO')
GO


SET ANSI_PADDING OFF

--add AHRO column in [dbo].[CaseWork] table
ALTER TABLE [dbo].[CaseWork]
  ADD 
      AHRO [int],
	CONSTRAINT [FK_CaseWork_AHRO] FOREIGN KEY 
	(
		[AHRO]
	) REFERENCES [luAHRO] (
		[Id]
	)
GO


--Create Stored Procedures
--1. Get luAHROCode
SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

CREATE  PROCEDURE GetluAHROCode AS  
  
SELECT  
 [Id],  
 [Name]   
FROM  
  luAHRO
ORDER BY  
 [Name]    
RETURN  



GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO



--2. GetluAHRO
SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO



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



GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO



--3. AddluAHRO
SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO


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

GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO



--4.EditluAHRO
SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO


CREATE   PROCEDURE EditluAHRO  
    @ID int,    
    @Name varchar(100)  
    
AS    
 DECLARE     
  @ErrorCode int,     
  @RowsUpdated int    
         
 UPDATE luAHRO SET    
  [Name] = @Name    
 WHERE    
  [ID] = @ID
            
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





--5.DeleteluAHRO
SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

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

GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO


--CaseWorkEntry procedures

--1. Edit CaseWorkEntry
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[EditCaseWorkEntry]  
      @RBSE char(9),  
      @Barcode varchar(20),  
      @AHFReference varchar(40),  
      @PurchaserBSE1ReceivedDate datetime,  
      @BreederBSE1ReceivedDate datetime,  
      @Vendor1BSE1ReceivedDate datetime,  
      @HomebredBSE1ReceivedDate datetime,  
      @SummarySheetReceivedDate datetime,  
      @PaperworkCompleteDate datetime,  
      @ActiveMemoDate datetime,  
      @AnnexADate datetime,  
      @AnnexBDate datetime,  
      @AnnexCDate datetime,  
      @AnnexDDate datetime,  
      @RegionalLab char(4),  
      @ReceivedByRegionalLabDate datetime,  
      @InitialReceivedDate datetime,  
      @FinalReceivedDate datetime,  
      @FinalSentDate datetime,  
      @LabChasedDate datetime,  
      @BarbMinuteSentDate datetime,  
      @Post2000SentDate datetime,  
      @CaseWorkNotes varchar(500),  
      @DataCompleteDate datetime,  
      @IsCaseClosed bit,  
      @UserID int,
      @TseTestingSite varchar(50),
      @SamplingDate datetime,   
      @AHROId int
AS  
BEGIN  
  
  
 INSERT INTO [AuditLog]  
  (  
  [TableName],  
  [FieldName],  
  [UserID],  
  [BeforeValue],  
  [AfterValue],  
  [Reason],  
  [RBSE]  
  )  
 SELECT  
  'CaseWork',  
  'IsCaseClosed',  
  @UserID,  
  [IsCaseClosed],  
  @IsCaseClosed,  
  'Amendment',  
  [RBSE]  
 FROM  
  [CaseWork]  
 WHERE  
  [RBSE] = @RBSE AND  
  (([IsCaseClosed] != @IsCaseClosed) OR ([IsCaseClosed] IS NULL AND @IsCaseClosed IS NOT NULL) OR ([IsCaseClosed] IS NOT NULL AND @IsCaseClosed IS NULL))  
  
  
UPDATE [dbo].[CaseWork]  
   SET   
      [Barcode] = @Barcode,  
      [AHFReference] = @AHFReference,  
      [PurchaserBSE1ReceivedDate] = @PurchaserBSE1ReceivedDate,  
      [BreederBSE1ReceivedDate] = @BreederBSE1ReceivedDate,  
      [Vendor1BSE1ReceivedDate] = @Vendor1BSE1ReceivedDate,  
      [HomebredBSE1ReceivedDate] = @HomebredBSE1ReceivedDate,  
      [SummarySheetReceivedDate] = @SummarySheetReceivedDate,  
      [PaperworkCompleteDate] = @PaperworkCompleteDate,  
      [ActiveMemoDate] = @ActiveMemoDate,  
      [AnnexADate] = @AnnexADate,  
      [AnnexBDate] = @AnnexBDate,  
      [AnnexCDate] = @AnnexCDate,  
      [AnnexDDate] = @AnnexDDate,  
      [RegionalLab] = @RegionalLab,  
      [ReceivedByRegionalLabDate] = @ReceivedByRegionalLabDate,  
      [InitialReceivedDate] = @InitialReceivedDate,  
      [FinalReceivedDate] = @FinalReceivedDate,  
      [FinalSentDate] = @FinalSentDate,  
      [LabChasedDate] = @LabChasedDate,  
      [BarbMinuteSentDate] = @BarbMinuteSentDate,  
      [Post2000SentDate] = @Post2000SentDate,  
      [CaseWorkNotes] = @CaseWorkNotes,  
      [DataCompleteDate] = @DataCompleteDate,  
      [IsCaseClosed] = @IsCaseClosed,
      [TseTestingSite] = @TseTestingSite,
      [SamplingDate] = @SamplingDate, 
      [AHRO] = @AHROId
  
WHERE   
    [RBSE] = @RBSE  
  
END
GO

--2. Add CaseWorkEntry
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
  
ALTER PROCEDURE [dbo].[GetCaseWorkEntryByRBSE]  
 @RBSE char(9)  
AS  
  
    SELECT   
        [Case].[RBSE],  
        [Case].[Survey],  
        [Case].[FormADate],  
  [Case].[FormBDate],  
        [Case].[SlaughterDate],  
        [Case].[Fate],  
        [Case].[IsPaperworkComplete],  
        [Case].[FinalResult],  
        [Case].[FinalResultDate],  
        [Case].[BirthDate],  
  [Case].[Origin],  
  [Case].[DBSE],  
  [Case].[AlternateDiagnosis],  
        [CaseWork].[Barcode],  
        [CaseWork].[AHFReference],  
        [CaseWork].[RBSEDate],  
        [CaseWork].[PurchaserBSE1ReceivedDate],  
        [CaseWork].[BreederBSE1ReceivedDate],  
        [CaseWork].[Vendor1BSE1ReceivedDate],  
        [CaseWork].[HomebredBSE1ReceivedDate],  
        [CaseWork].[SummarySheetReceivedDate],  
  [CaseWork].[PaperworkCompleteDate],  
        [CaseWork].[ActiveMemoDate],  
        [CaseWork].[AnnexADate],  
        [CaseWork].[AnnexBDate],  
        [CaseWork].[AnnexCDate],  
        [CaseWork].[AnnexDDate],  
        [CaseWork].[RegionalLab],  
        [CaseWork].[ReceivedByRegionalLabDate],  
        [CaseWork].[InitialReceivedDate],  
        [CaseWork].[FinalReceivedDate],  
        [CaseWork].[FinalSentDate],  
        [CaseWork].[LabChasedDate],  
        [CaseWork].[Post2000SentDate],  
        [CaseWork].[BarbMinuteSentDate],  
        [CaseWork].[DataCompleteDate],  
        [CaseWork].[CaseWorkNotes],  
  [CaseWork].[IsCaseClosed],  
  CASE WHEN ISNULL([Case].[Survey], 'PS') != 'PS' AND [CaseWork].[ActiveMemoDate] IS NULL AND [CaseWork].[PaperworkCompleteDate] IS NULL THEN [CaseWork].[RBSEDate] ELSE NULL END AS [ActiveMemoDueDate],  
  CASE WHEN [Case].[Fate] IS NULL AND [CaseWork].[PaperworkCompleteDate] IS NULL AND [CaseWork].[AnnexADate] IS NULL THEN DATEADD(Day,7,[CaseWork].[RBSEDate]) ELSE NULL END AS [AnnexADueDate],  
  CASE WHEN [Case].[Fate] IS NULL AND [CaseWork].[PaperworkCompleteDate] IS NULL AND [CaseWork].[AnnexBDate] IS NULL AND CONVERT(char(8),[CaseWork].[AnnexADate],112) <= CONVERT(char(8),GetDate(),112) THEN DATEADD(Day,7,[CaseWork].[AnnexADate]) ELSE NULL END AS [AnnexBDueDate],  
  CASE WHEN [Case].[IsPaperworkComplete] = 0 AND [CaseWork].[PaperworkCompleteDate] IS NULL AND [CaseWork].[AnnexCDate] IS NULL THEN DATEADD(Day,7,[CaseWork].[RBSEDate]) ELSE NULL END AS [AnnexCDueDate],  
  CASE WHEN [Case].[IsPaperworkComplete] = 0 AND [CaseWork].[PaperworkCompleteDate] IS NULL AND [CaseWork].[AnnexDDate] IS NULL AND CONVERT(char(8),[CaseWork].[AnnexCDate],112) <= CONVERT(char(8),GetDate(),112) THEN DATEADD(Day,7,[CaseWork].[AnnexCDate]) 
ELSE NULL END AS [AnnexDDueDate],  
  CASE WHEN [Case].[SlaughterDate] IS NOT NULL AND [CaseWork].[FinalReceivedDate] IS NULL AND [CaseWork].[LabChasedDate] IS NULL THEN DATEADD(Day,10,[Case].[SlaughterDate]) ELSE NULL END AS [LabChaseDueDate],  
  CASE WHEN [Case].[BirthDate] > '19960731' AND FinalResult = 'Pos' AND [CaseWork].[BarbMinuteSentDate] IS NULL THEN 'Yes' ELSE NULL END AS [BarbMemoDue],
[CaseWork].[TseTestingSite],
[CaseWork].[Samplingdate],
[CaseWork].[AHRO]
  
FROM  
  
[Case] RIGHT JOIN [Casework] ON [Case].[RBSE] = [CaseWork].[RBSE]  


WHERE  
  
[Case].[RBSE] = @RBSE  
  
RETURN  
  
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO  

--GetMinuteDetails procedure
ALTER PROCEDURE [dbo].[GetMinuteDetails]   
 @RBSE char(9),  
 @MinuteType char(10)  
AS  
BEGIN  
 IF @MinuteType = 'ActiveMemo'   
  SELECT   
   [Case].[RBSE],  
   [Case].[CPHH],  
   [Case].[EartagHerdmark],  
   [Case].[Eartag],  
   [Case].[BirthDate],  
   [Case].[SlaughterDate],  
   [luSurvey].[Description] As SurveyDescription,  
   [Farm].[AHO] As AHONumber,  
   [luAHO].[Name] As AHOName,  
   [Farm].[OwnerName],  
   [CaseWork].[ActiveMemoDate],  
   [Case].[EartagCountry]  
  FROM  
   [CaseWork] LEFT JOIN [Case] ON [CaseWork].[RBSE] = [Case].[RBSE]  
   LEFT JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]  
   LEFT JOIN [luAHO] ON [Farm].[AHO] = [luAHO].[Code]  
   LEFT JOIN [luSurvey] ON [Case].[Survey] = [luSurvey].[Code]  
  WHERE  
   [Case].[RBSE] = @RBSE  
    
 ELSE IF @MinuteType = 'AnnexA'   
  SELECT  
   [Case].[RBSE],    
   [CaseWork].[AnnexADate],  
   [luAHO].[Name] As AHOName  
  FROM  
   [CaseWork] LEFT JOIN [Case] ON [CaseWork].[RBSE] = [Case].[RBSE]  
   LEFT JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]  
   LEFT JOIN [luAHO] ON [Farm].[AHO] = [luAHO].[Code]  
  WHERE  
    [Case].[RBSE] = @RBSE  
    
    
 ELSE IF @MinuteType = 'AnnexB'   
  SELECT  
   [Case].[RBSE],    
   [CaseWork].[AnnexADate],  
   [CaseWork].[AnnexBDate],  
   [luAHO].[Name] As AHOName  
  FROM  
   [CaseWork] LEFT JOIN [Case] ON [CaseWork].[RBSE] = [Case].[RBSE]  
   LEFT JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]  
   LEFT JOIN [luAHO] ON [Farm].[AHO] = [luAHO].[Code]  
  WHERE  
    [Case].[RBSE] = @RBSE  
    
 ELSE IF @MinuteType = 'AnnexC'   
  SELECT  
   [Case].[RBSE],    
   [CaseWork].[AnnexCDate],  
   [luAHO].[Name] As AHOName  
  FROM  
   [CaseWork] LEFT JOIN [Case] ON [CaseWork].[RBSE] = [Case].[RBSE]  
   LEFT JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]  
   LEFT JOIN [luAHO] ON [Farm].[AHO] = [luAHO].[Code]  
  WHERE  
    [Case].[RBSE] = @RBSE  
   
 ELSE IF @MinuteType = 'AnnexD'  
  SELECT  
   [Case].[RBSE],    
   [CaseWork].[AnnexCDate],  
   [CaseWork].[AnnexDDate],  
   [luAHO].[Name] As AHOName  
  FROM  
   [CaseWork] LEFT JOIN [Case] ON [CaseWork].[RBSE] = [Case].[RBSE]  
   LEFT JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]  
   LEFT JOIN [luAHO] ON [Farm].[AHO] = [luAHO].[Code]  
  WHERE  
    [Case].[RBSE] = @RBSE  
   
ELSE  IF @MinuteType = 'AMFS'   
  SELECT     
   [Case].[RBSE],    
   [Case].[CPHH],    
   [Case].[EartagHerdmark],    
   [Case].[Eartag],    
   [Case].[BirthDate],    
   [Case].[SlaughterDate],    
   [luSurvey].[Description] As SurveyDescription,    
   [luAHO].[Name] As TSETestingAHOName,    
   [FARMAHO].[Name] AS FarmAHOName,
   [Farm].[OwnerName],    
   [CaseWork].[ActiveMemoDate],    
   [Case].[EartagCountry],  
   [CaseWork].[TseTestingSite],  
   [CaseWork].[SamplingDate],  
   [Farm].[AHO] AS FarmAHONumber,  
   [luTseTestingSite].[Address] AS TSEAddress,  
   [luTseTestingSite].[CPH] AS TSECPH,
   [luAHRO].[Name] AS CaseWorkAHRO

  FROM    
   [CaseWork] LEFT JOIN [Case] ON [CaseWork].[RBSE] = [Case].[RBSE]    
   LEFT JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]    
   LEFT JOIN [luSurvey] ON [Case].[Survey] = [luSurvey].[Code]   
   LEFT JOIN [luTseTestingSite] ON [CaseWork].[TseTestingSite] = [luTseTestingSite].[Name]   
   LEFT JOIN [luAHO] ON [luTseTestingSite].[AHO] = [luAHO].[Code]
   LEFT JOIN [luAHRO] ON [CaseWork].[AHRO]=[luAHRO].[Id]
   LEFT JOIN
	(
		SELECT DISTINCT [luAHO].[CODE],[luAHO].[NAME],[CASE].[CPHH] FROM [luAHO]
			LEFT JOIN [FARM] ON [FARM].[AHO] = [luAHO].[CODE] 
				LEFT JOIN [CASE] ON [CASE].[CPHH] = [FARM].[CPHH]
	) AS [FARMAHO] ON [FARMAHO].[CPHH] = [CASE].[CPHH]
  WHERE    
   [Case].[RBSE] = @RBSE    
    
END
GO





 