set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go



CREATE PROCEDURE [dbo].[EditCaseWork] 
	@RBSE char(9), 
    @RBSEDate datetime,
    @Barcode varchar(20),
    @AHFReference varchar(40),
    @PurchaserBSE1ReceivedDate datetime,
    @BreederBSE1ReceivedDate datetime,
    @Vendor1BSE1ReceivedDate datetime,
    @HomebredBSE1ReceivedDate datetime,
    @SummarySheetReceivedDate datetime,
    @PaperworkCompleteDate datetime
AS

DECLARE
	@RowCount int,
	@ErrorCode int

UPDATE [VLA_BSE].[dbo].[CaseWork]
   SET [RBSE] = @RBSE,
       [Barcode] = @Barcode,
       [AHFReference] = @AHFReference,
       [PurchaserBSE1ReceivedDate] = @PurchaserBSE1ReceivedDate,
       [BreederBSE1ReceivedDate] = @BreederBSE1ReceivedDate,
       [Vendor1BSE1ReceivedDate] = @Vendor1BSE1ReceivedDate,
       [HomebredBSE1ReceivedDate] = @HomebredBSE1ReceivedDate,
       [SummarySheetReceivedDate] = @SummarySheetReceivedDate,
       [PaperworkCompleteDate] = @PaperworkCompleteDate
WHERE
		[RBSE] = @RBSE
		
SELECT
		@RowCount = @@ROWCOUNT,
		@ErrorCode = @@ERROR

IF @RowCount <> 1 BEGIN
	RETURN 1
END

IF @ErrorCode <> 0 BEGIN
	RETURN 2
END

RETURN 0