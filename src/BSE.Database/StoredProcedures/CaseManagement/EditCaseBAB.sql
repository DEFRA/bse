
CREATE PROCEDURE EditCaseBAB
	@RBSE char(9),
	@NatalCPHH char(11),
	@Notes varchar(500),
	@TracedName varchar(30),
	@TracedAddress1 varchar(30),
	@TracedAddress2 varchar(30),
	@TracedAddress3 varchar(30),
	@TracedPostcode varchar(10),
	@FeedRisk varchar(10),
	@HorizontalRisk varchar(10),
	@MaternalRisk varchar(10),
	@RowStamp timestamp AS

	DECLARE
		@RowCount int,
		@ErrorCode int

	UPDATE
		[CaseBAB]
	SET 
		NatalCPHH = @NatalCPHH,
		Notes = @Notes,
		TracedName = @TracedName,
		TracedAddress1 = @TracedAddress1,
		TracedAddress2 = @TracedAddress2,
		TracedAddress3 = @TracedAddress3,
		TracedPostcode = @TracedPostcode,
		FeedRisk = @FeedRisk,
		HorizontalRisk = @HorizontalRisk,
		MaternalRisk = @MaternalRisk
	WHERE
		[RBSE] = @RBSE AND
		[RowStamp] = @RowStamp

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
