
CREATE PROCEDURE [dbo].[GetBSESSCheckByRBSE]
	@RBSE char(9),
	@NotificationDate varchar(30) OUTPUT,
	@BSESSEartag varchar(20) OUTPUT,
	@BSESSBirthDate varchar(30) OUTPUT,
	@TestGroupName varchar(50) OUTPUT,
	@BSESSFinalResult varchar(25) OUTPUT,
	@Barcode varchar(20) OUTPUT,
	@FormADate varchar(30) OUTPUT,
	@BSEEartag varchar(33) OUTPUT,
	@BSEBirthDate varchar(30) OUTPUT,
	@Survey varchar(50) OUTPUT,
	@BSEFinalResult varchar(50)  OUTPUT
	AS

	SELECT
		@NotificationDate = CONVERT(varchar(30), [BSESSImport].[NotificationDate], 103),
		@BSESSEartag = [BSESSImport].[Eartag],
		@BSESSBirthDate = CONVERT(varchar(30), [BSESSImport].[BirthDate], 103),
		@TestGroupName = [BSESSImport].[TestGroupName],
		@BSESSFinalResult = [BSESSImport].[FinalResultName],
		@Barcode = [BSESSImport].[Barcode]
	FROM
		[BSESSImport] 
	WHERE
		[BSESSImport].[RBSE] = @RBSE
	
	SELECT
		@FormADate = CONVERT(varchar(30), [Case].[FormADate], 103),
		@BSEEartag = ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], ''),
		@BSEBirthDate = CONVERT(varchar(30), [Case].[BirthDate], 103),
		@Survey = [luSurvey].[Description],
		@BSEFinalResult = [luTestResult].[Description]
	FROM
		[Case] LEFT JOIN [luSurvey] ON [Case].[Survey] = [luSurvey].[Code]
		LEFT JOIN [luTestResult] ON [Case].[FinalResult] = [luTestResult].[Code]
	WHERE
		[Case].[RBSE] = @RBSE

