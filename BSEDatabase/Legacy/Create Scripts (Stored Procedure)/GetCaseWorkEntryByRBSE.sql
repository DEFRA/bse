set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go




CREATE PROCEDURE [dbo].[GetCaseWorkEntryByRBSE]
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
		CASE WHEN ISNULL([Case].[Survey], 'PS') != 'PS' AND [CaseWork].[ActiveMemoDate] IS NULL THEN [CaseWork].[RBSEDate] ELSE NULL END AS [ActiveMemoDueDate],
		CASE WHEN [Case].[Fate] IS NULL THEN DATEADD(Day,7,[CaseWork].[RBSEDate]) ELSE NULL END AS [AnnexADueDate],
		CASE WHEN [Case].[Fate] IS NULL AND CONVERT(char(8),[CaseWork].[AnnexADate],112) <= CONVERT(char(8),GetDate(),112) THEN DATEADD(Day,7,[CaseWork].[AnnexADate]) ELSE NULL END AS [AnnexBDueDate],
		CASE WHEN [Case].[IsPaperworkComplete] = 1 THEN DATEADD(Day,7,[CaseWork].[RBSEDate]) ELSE NULL END AS [AnnexCDueDate],
		CASE WHEN [Case].[IsPaperworkComplete] = 1 AND CONVERT(char(8),[CaseWork].[AnnexCDate],112) <= CONVERT(char(8),GetDate(),112) THEN DATEADD(Day,7,[CaseWork].[AnnexCDate]) ELSE NULL END AS [AnnexDDueDate],
		CASE WHEN [Case].[SlaughterDate] IS NOT NULL AND [CaseWork].[FinalReceivedDate] IS NULL THEN DATEADD(Day,10,[Case].[SlaughterDate]) ELSE NULL END AS [LabChaseDueDate],
		CASE WHEN [Case].[BirthDate] > '19960731' AND FinalResult = 'Pos' AND [CaseWork].[BarbMinuteSentDate] IS NULL THEN 'Yes' ELSE NULL END AS [BarbMemoDue]

FROM

[Case] RIGHT JOIN [Casework] ON [Case].[RBSE] = [CaseWork].[RBSE]

WHERE

[Case].[RBSE] = @RBSE

RETURN




