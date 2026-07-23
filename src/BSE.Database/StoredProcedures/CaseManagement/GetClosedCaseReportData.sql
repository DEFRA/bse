


CREATE PROCEDURE [dbo].[GetClosedCaseReportData]

AS

    SELECT 
        [Case].[RBSE],
        [Case].[Survey],
        [Case].[FormADate],
        [Case].[SlaughterDate],
        [Case].[Fate],
        [Case].[IsPaperworkComplete],
        [Case].[FinalResult],
        [Case].[FinalResultDate],
        [Case].[BirthDate],
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
        [CaseWork].[CaseWorkNotes]
FROM

[Case] RIGHT JOIN [Casework] ON [Case].[RBSE] = [CaseWork].[RBSE]

WHERE

[CaseWork].[IsCaseClosed] = 1 
