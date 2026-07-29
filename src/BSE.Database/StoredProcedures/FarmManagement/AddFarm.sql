
CREATE PROCEDURE dbo.AddFarm
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
	
	IF @RowCount > 0 BEGIN
		RETURN 3
	END
	
	SET NOCOUNT ON

	INSERT INTO [AuditLog]
			(
			[TableName],
			[UserID],
			[AfterValue],
			[Reason],
			[CPHH]
			)
		VALUES
			(
			'Farm',
			@UserID,
			LEFT(@CPHH, 2) + '/' + SUBSTRING(@CPHH, 3, 3) + '/' + SUBSTRING(@CPHH, 6, 4) + '/' + RIGHT(@CPHH, 2) ,
			'Creation',
			@CPHH
			)

	SET @ErrorCode = @@ERROR

	IF @ErrorCode <> 0 BEGIN
		RETURN 1
	END

	SET NOCOUNT OFF

	INSERT INTO [Farm]
		(
		[CPHH],
		[IsNonGBFarm],
		[OwnerName],
		[Address1],
		[Address2],
		[Address3],
		[Postcode],
		[Parish],
		[District],
		[County],
		[CorrespondenceAddress1],
		[CorrespondenceAddress2],
		[CorrespondenceAddress3],
		[CorrespondencePostcode],
		[MapReference],
		[Herdmark1],
		[Herdmark2],
		[Herdmark3],
		[NumericHerdmark1],
		[NumericHerdmark2],
		[AHO],
		[HerdType],
		[PedigreeType],
		[IsDealer],
		[ADNSRegionID]
		)
	 VALUES
		(
		@CPHH,
		CASE WHEN LEFT(@CPHH,6) = '009999' THEN 1 ELSE 0 END,
		@OwnerName,
		@Address1,
		@Address2,
		@Address3,
		@Postcode,
		@Parish,
		@District,
		@County,
		@CorrespondenceAddress1,
		@CorrespondenceAddress2,
		@CorrespondenceAddress3,
		@CorrespondencePostcode,
		@MapReference,
		@Herdmark1,
		@Herdmark2,
		@Herdmark3,
		@NumericHerdmark1,
		@NumericHerdmark2,
		@AHO,
		@HerdType,
		@PedigreeType,
		@IsDealer,
		@ADNSRegionID
	)
	
	SET @ErrorCode = @@ERROR

	IF @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	RETURN 0
