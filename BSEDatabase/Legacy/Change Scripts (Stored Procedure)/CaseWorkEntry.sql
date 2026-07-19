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
      @SamplingDate datetime   
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
      [SamplingDate] = @SamplingDate 
  
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
[CaseWork].[Samplingdate]
  
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
  
  
--Edit getMinuteDetails procedure for new active memo request
  
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
   [luAHO].[Name] As AHOName,    
   [Farm].[OwnerName],    
   [CaseWork].[ActiveMemoDate],    
   [Case].[EartagCountry],  
   [CaseWork].[TseTestingSite],  
   [CaseWork].[SamplingDate],  
   [luTseTestingSite].[AHO] AS TSEAHONumber,  
   [luTseTestingSite].[Address] AS TSEAddress,  
   [luTseTestingSite].[CPH] AS TSECPH 
  FROM    
   [CaseWork] LEFT JOIN [Case] ON [CaseWork].[RBSE] = [Case].[RBSE]    
   LEFT JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]    
   LEFT JOIN [luSurvey] ON [Case].[Survey] = [luSurvey].[Code]   
   LEFT JOIN [luTseTestingSite] ON [CaseWork].[TseTestingSite] = [luTseTestingSite].[Name]   
   LEFT JOIN [luAHO] ON [luTseTestingSite].[AHO] = [luAHO].[Code]    
  WHERE    
   [Case].[RBSE] = @RBSE  
   
    
END
GO
  