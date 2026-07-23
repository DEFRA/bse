
CREATE PROCEDURE dbo.EditFarm
	@CPHH char(11),
	@OwnerName varchar(100),
	@Address1 varchar(50),
	@Address2 varchar(50),
	@Address3 varchar(50),
	@Postcode varchar(10),
	@Parish varchar(50),
	@District varchar(50),
	@County varchar(15),
	@CorrespondenceAddress1 varchar(50),
	@CorrespondenceAddress2 varchar(50),
	@CorrespondenceAddress3 varchar(50),
	@CorrespondencePostcode varchar(10),
	@MapReference char(8),
	@Herdmark1 varchar(8),
	@Herdmark2 varchar(8),
	@Herdmark3 varchar(8),
	@NumericHerdmark1 char(6),
	@NumericHerdmark2 char(6),
	@AHO char(2),
	@HerdType char(1),
	@PedigreeType char(2),
	@IsDealer bit,
	@ADNSRegionID int,
	@RowStamp timestamp,
	@UserID int AS
	
	DECLARE
		@RowCount int,
		@ErrorCode int

	/*
	This stored procedure is designed to be called from within a transaction, which should be rolled back if a return code greater than 0 is returned
	*/

	SELECT
		[CPHH]
	FROM
		[Farm]
	WHERE
		[CPHH] = @CPHH

	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount = 0 BEGIN
		RETURN 1
	END

	SET NOCOUNT ON

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[CPHH]
		)
	SELECT
		'Farm',
		'County',
		@UserID,
		[County],
		@County,
		'Amendment',
		[CPHH]
	FROM
		[Farm]
	WHERE
		[CPHH] = @CPHH AND
		[County] != @County

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[CPHH]
		)
	SELECT
		'Farm',
		'AHO',
		@UserID,
		[AHO],
		@AHO,
		'Amendment',
		[CPHH]
	FROM
		[Farm]
	WHERE
		[CPHH] = @CPHH AND
		(([AHO]  != @AHO) OR ([AHO] IS NULL AND @AHO IS NOT NULL) OR ([AHO] IS NOT NULL AND @AHO IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[CPHH]
		)
	SELECT
		'Farm',
		'HerdType',
		@UserID,
		[HerdType],
		@HerdType,
		'Amendment',
		[CPHH]
	FROM
		[Farm]
	WHERE
		[CPHH] = @CPHH AND
		(([HerdType]  != @HerdType) OR ([HerdType] IS NULL AND @HerdType IS NOT NULL) OR ([HerdType] IS NOT NULL AND @HerdType IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[CPHH]
		)
	SELECT
		'Farm',
		'ADNSRegionID',
		@UserID,
		[ADNSRegionID],
		@ADNSRegionID,
		'Amendment',
		[CPHH]
	FROM
		[Farm]
	WHERE
		[CPHH] = @CPHH AND
		(([ADNSRegionID]  != @ADNSRegionID) OR ([ADNSRegionID] IS NULL AND @ADNSRegionID IS NOT NULL) OR ([ADNSRegionID] IS NOT NULL AND @ADNSRegionID IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	SET NOCOUNT OFF

	UPDATE
		[Farm]
	SET
		[OwnerName] =@ownerName,
		[Address1] = @Address1,
		[Address2] =@Address2,
		[Address3] =@Address3,
		[Postcode] = @Postcode,
		[Parish] = @Parish,
		[District] =@District,
		[County] = @County,
		[CorrespondenceAddress1] = @CorrespondenceAddress1,
		[CorrespondenceAddress2] = @CorrespondenceAddress2,
		[CorrespondenceAddress3] =  @CorrespondenceAddress3,
		[CorrespondencePostcode] =  @CorrespondencePostcode,
		[MapReference] = @MapReference,
		[Herdmark1] =  @Herdmark1,
		[Herdmark2] = @Herdmark2,
		[Herdmark3] =@Herdmark3,
		[NumericHerdmark1] =@NumericHerdmark1,
		[NumericHerdmark2] = @NumericHerdmark2,
		[AHO] = @AHO,
		[HerdType] =  @HerdType,
		[PedigreeType] = @PedigreeType,
		[IsDealer] =@IsDealer,
		[ADNSRegionID] = @ADNSRegionID
	WHERE
		[CPHH] = @CPHH AND
		[RowStamp] = @RowStamp

	SELECT
		@RowCount = @@ROWCOUNT,
		@ErrorCode = @@ERROR

	IF @RowCount <> 1 BEGIN
		RETURN 3
	END

	IF @ErrorCode <> 0 BEGIN
		RETURN 4
	END

	SET NOCOUNT OFF

	RETURN 0
