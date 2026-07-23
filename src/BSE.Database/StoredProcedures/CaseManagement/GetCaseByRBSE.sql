




CREATE PROCEDURE [dbo].[GetCaseByRBSE]
	@RBSE char(9)
AS

SELECT
	[Case].[RBSE],
	[Case].[CPHH],
	[Case].[EartagCountry],
	[Case].[EartagHerdmark],
	[Case].[Eartag],
	[Case].[IsNonGBCase],
	[Case].[PreviousEartag],
	[Case].[BSE1ReceivedDate],
	[Case].[FormADate],
	[Case].[FormAResubmittedDate],
	[Case].[FormBDate],
	[Case].[Fate],
	[Case].[FormCDate],
	[Case].[IsPurchaserBSE1Received],
	[Case].[IsBreederBSE1Received],
	[Case].[IsVendor1BSE1Received],
	[Case].[IsHomebredBSE1Received],
	[Case].[IsSummarySheetReceived],
	[Case].[IsPaperworkComplete],
	[Case].[FinalResultDate],
	[Case].[FinalResult],
	[Case].[DBSE],
	[Case].[ReportedLocation],
	[Case].[Survey],
	[Case].[Notes],
	[Case].[BirthDate],
	[Case].[IsBAB],
	[Case].[IsBirthDateEst],
	[Case].[DamStatus],
	[Case].[BirthDateSource],
	[Case].[ValuationAge],
	[Case].[Sex],
	[Case].[Breed],
	[Case].[Origin],
	[Case].[PurchaseDate],
	[Case].[PurchaseAgeInMonths],
	[Case].[PurchasedCounty],
	[Case].[HerdEntryDate],
	[Case].[OnsetDate],
	[Case].[IsOnsetDateEst],
	[Case].[MonthsPregnant],
	[Case].[MonthsPostCalving],
	[Case].[OnsetAgeInMonths],
	[Case].[SlaughterDate],
	[Case].[EmailSentToADNSDate],
	[Case].[RowStamp],
	[Case].[AlternateDiagnosis],
	[Case].[LabComment],
	[Case].[CaseType],
	[Pedigree].[RowStamp] AS [PedigreeRowStamp],
	[Pedigree].[Herdbook]
FROM
	[Case] LEFT JOIN [Pedigree] ON [case].[RBSE] = [Pedigree].[RBSE]
WHERE
	[Case].[RBSE] = @RBSE

RETURN




