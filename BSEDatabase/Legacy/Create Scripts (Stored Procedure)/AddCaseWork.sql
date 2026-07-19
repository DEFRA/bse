set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


CREATE PROCEDURE [dbo].[AddCaseWork] 
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
	@ErrorCode int

INSERT INTO [dbo].[CaseWork]
      (
           [RBSE],
           [RBSEDate],
           [Barcode],
           [AHFReference],
           [PurchaserBSE1ReceivedDate],
           [BreederBSE1ReceivedDate],
           [Vendor1BSE1ReceivedDate],
           [HomebredBSE1ReceivedDate],
           [SummarySheetReceivedDate],
           [PaperworkCompleteDate]
     )
     VALUES
     (
	       @RBSE,
           @RBSEDate, 
           @Barcode, 
           @AHFReference,
           @PurchaserBSE1ReceivedDate, 
           @BreederBSE1ReceivedDate,
           @Vendor1BSE1ReceivedDate,
           @HomebredBSE1ReceivedDate, 
           @SummarySheetReceivedDate,
           @PaperworkCompleteDate
     )

SET @ErrorCode = @@ERROR

IF @ErrorCode <> 0 BEGIN
	RETURN 1
END

RETURN 0