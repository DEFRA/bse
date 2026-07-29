
CREATE PROCEDURE [dbo].[EditCaseWorkEntry]  
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
      @SamplingDate datetime,   
      @AHROId int
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
      [SamplingDate] = @SamplingDate, 
      [AHRO] = @AHROId
  
WHERE   
    [RBSE] = @RBSE  
  
END
