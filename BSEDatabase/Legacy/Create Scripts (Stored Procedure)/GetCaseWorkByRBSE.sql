SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE GetCaseWorkByRBSE 
	@RBSE char(9) 
AS

SELECT 
	[RBSE],
	[RBSEDate],
	[Barcode],
	[AHFReference],
	[PurchaserBSE1ReceivedDate],
	[BreederBSE1ReceivedDate],
	[Vendor1BSE1ReceivedDate],
	[HomebredBSE1ReceivedDate],
	[SummarySheetReceivedDate],
	[PaperworkCompleteDate],
	[RowStamp],
	[IsCaseClosed]
FROM
	[CaseWork]
WHERE
	[RBSE] = @RBSE

RETURN


