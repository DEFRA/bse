
CREATE PROCEDURE EditCaseADNS
	@RBSE char(9),
	@SentDate datetime,
	@ADNSRegionID int,
	@ADNSYear smallint,
	@ADNSNumber int,
	@RowStamp timestamp AS

	DECLARE
		@RowCount int,
		@ErrorCode int,
		@CurrentADNSRegionID int,
		@CurrentADNSYear smallint,
		@CurrentADNSNumber int,
		@CurrentEmailSentToADNSDate datetime

	SELECT
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		[RowStamp] = @RowStamp

	SELECT
		@RowCount = @@ROWCOUNT,
		@ErrorCode = @@ERROR

	IF @RowCount <> 1 OR @ErrorCode <> 0 BEGIN
		RETURN 1
	END

	SELECT
		@CurrentADNSRegionID = [Farm].[ADNSRegionID],
		@CurrentADNSYear = [Case].[ADNSReferenceYear],
		@CurrentADNSNumber = [Case].[ADNSReferenceNumber],
		@CurrentEmailSentToADNSDate = [Case].[EmailSentToADNSDate]
	FROM
		[Case] INNER JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]
	WHERE
		[Case].[RBSE] = @RBSE AND
		[Case].[RowStamp] = @RowStamp

	IF @CurrentEmailSentToADNSDate IS NOT NULL BEGIN
		RETURN 2
	END

	IF @CurrentADNSRegionID != @ADNSRegionID BEGIN
		RETURN 3
	END

	IF @CurrentADNSYear IS NOT NULL OR @CurrentADNSNumber IS NOT NULL BEGIN
		RETURN 4
	END

	UPDATE
		[Case]
	SET
		[EmailSentToADNSDate] = @SentDate,
		[ADNSReferenceYear] = @ADNSYear,
		[ADNSReferenceNumber] = @ADNSNumber
	WHERE
		[RBSE] = @RBSE


	SELECT
		@RowCount = @@ROWCOUNT,
		@ErrorCode = @@ERROR

	IF @RowCount <> 1 OR @ErrorCode <> 0 BEGIN
		RETURN 5
	END
