
CREATE PROCEDURE GetTestByRBSE
	@RBSE char(9)  AS

	SELECT
		[CaseTest].[ID],
		[CaseTest].[RBSE],
		[CaseTest].[TestType],
		[luTestType].[Description] AS [TestTypeDescription],
		[CaseTest].[TestResult],
		[luTestResult].[Description] AS [TestResultDescription],
		[CaseTest].[RowStamp]
	FROM
		[CaseTest] INNER JOIN [luTestType] ON [CaseTest].[TestType] = [luTestType].[Code]
		LEFT JOIN [luTestResult] ON [CaseTest].[TestResult] = [luTestResult].[Code]
	WHERE
		[CaseTest].[RBSE] = @RBSE
