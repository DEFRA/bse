
CREATE PROCEDURE AddCaseBAB
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
	@MaternalRisk varchar(10) AS
	
	DECLARE @ErrorCode int

	INSERT INTO [CaseBAB]
		(
		[RBSE],
		[NatalCPHH],
		[Notes],
		[TracedName],
		[TracedAddress1],
		[TracedAddress2],
		[TracedAddress3],
		[TracedPostcode],
		[FeedRisk],
		[HorizontalRisk],
		[MaternalRisk]
		)
	VALUES
		(
		@RBSE,
		@NatalCPHH,
		@Notes,
		@TracedName,
		@TracedAddress1,
		@TracedAddress2,
		@TracedAddress3,
		@TracedPostcode,
		@FeedRisk,
		@HorizontalRisk,
		@MaternalRisk
		)

	SET @ErrorCode = @@ERROR

	IF @ErrorCode <> 0 BEGIN
		RETURN 1
	END

	RETURN 0
