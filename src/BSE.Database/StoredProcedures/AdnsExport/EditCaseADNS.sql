
CREATE PROCEDURE EditCaseADNS
	@RBSE char(9),
	@SentDate datetime,
	@ADNSRegionID int,
	@ADNSYear smallint,
	@ADNSNumber int,
	@RowStamp timestamp AS

	DECLARE
		@RowCount int,
		@CurrentADNSRegionID int,
		@CurrentADNSYear smallint,
		@CurrentADNSNumber int,
		@CurrentEmailSentToADNSDate datetime

	BEGIN TRY

	-- ── Business rule pre-checks ─────────────────────────────────────────────
	-- RETURN 1: RowStamp mismatch (concurrency)
	IF NOT EXISTS (SELECT 1 FROM [Case] WHERE [RBSE] = @RBSE AND [RowStamp] = @RowStamp)
	BEGIN
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
		[Case].[RBSE] = @RBSE

	-- RETURN 2: already reported
	IF @CurrentEmailSentToADNSDate IS NOT NULL BEGIN
		RETURN 2
	END

	-- RETURN 3: region changed
	IF @CurrentADNSRegionID != @ADNSRegionID BEGIN
		RETURN 3
	END

	-- RETURN 4: reference already assigned
	IF @CurrentADNSYear IS NOT NULL OR @CurrentADNSNumber IS NOT NULL BEGIN
		RETURN 4
	END

	-- ── DML ─────────────────────────────────────────────────────────────────
	UPDATE
		[Case]
	SET
		[EmailSentToADNSDate] = @SentDate,
		[ADNSReferenceYear] = @ADNSYear,
		[ADNSReferenceNumber] = @ADNSNumber
	WHERE
		[RBSE] = @RBSE

	SET @RowCount = @@ROWCOUNT
	IF @RowCount <> 1 BEGIN
		RETURN 5
	END

	RETURN 0

	END TRY
	BEGIN CATCH
		THROW
	END CATCH
