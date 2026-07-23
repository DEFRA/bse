

--GetMinuteDetails procedure
CREATE PROCEDURE [dbo].[GetMinuteDetails]   
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
   [luAHRO].[Name] AS CaseWorkAHRO,
   [luTestResult].[Description] AS TestResult
   

  FROM    
   [CaseWork] LEFT JOIN [Case] ON [CaseWork].[RBSE] = [Case].[RBSE]    
   LEFT JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]    
   LEFT JOIN [CaseTest] ON [Case].[RBSE] = [CaseTest].[RBSE]
   LEFT JOIN [luTestResult] ON [CaseTest].[TestResult] = [luTestResult].[Code]
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

