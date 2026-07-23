
CREATE PROCEDURE [dbo].[AddNonGBCase] 
	@RBSE char(9),
	@Eartag varchar(25),
	@EartagHerdmark varchar(8),
	@EartagCountry varchar(4),
	@Fate varchar(4),
	@FinalResult varchar(5),
	@FinalResultDate datetime,
	@SlaughterDate datetime,
	@CPHH char(11),
	@OwnerName varchar(100),
	@Address1 varchar(50),
	@Address2 varchar(50),
	@Address3 varchar(50),
	@Postcode varchar(10),
	@County varchar(15),
	@Herdmark1 varchar(8),
	@NumericHerdmark1 char(6),
	@UserID int,
	@RBSEDate datetime,
	@Barcode varchar(20),
	@AHFReference varchar(40)
AS

	DECLARE
		@RowCount int,
		@ErrorCode int

BEGIN TRANSACTION

	-- Check to ensure that Case doesn't already exist
	SELECT
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE

	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 1
	END

	-- Check to see if the Farm already exists, don't need to raise an error if it does.
	SELECT
		[CPHH]
	FROM
		[Farm]
	WHERE
		[CPHH] = @CPHH

	SET @RowCount = @@ROWCOUNT

	-- If the Farm already exists, perform any updates to the information
	IF @RowCount = 1 BEGIN

		UPDATE
			[Farm]
		SET
			[OwnerName] = @OwnerName,
			[IsNonGBFarm] = 1,
			[Address1] = @Address1,
			[Address2] = @Address2,
			[Address3] = @Address3,
			[Postcode] = @Postcode,
			[County] = @County,
			[Herdmark1] = @Herdmark1,
			[NumericHerdmark1] = @NumericHerdmark1
		WHERE
			[CPHH] = @CPHH

		SET @ErrorCode = @@ERROR
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 2
		END

		-- Update the Audit Log when updating the Farm
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
			[CPHH] = @CPHH AND [County] != @County

		SET @ErrorCode = @@ERROR
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 3
		END

	END ELSE BEGIN
		-- Create the Farm
		INSERT INTO [Farm]
			(
				[CPHH],
				[OwnerName],
				[IsNonGBFarm],
				[Address1],
				[Address2],
				[Address3],
				[Postcode],
				[County],
				[Herdmark1],
				[NumericHerdmark1],
				[IsDealer]
			)
		VALUES
			(
				@CPHH,
				@OwnerName,
				1,
				@Address1,
				@Address2,
				@Address3,
				@Postcode,
				@County,
				@Herdmark1,
				@NumericHerdmark1,
				0
			)

		SET @ErrorCode = @@ERROR
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 4
		END

		--Update the Audit Log with the Farm creation
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
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 5
		END

	END

	-- Create the Case
	INSERT INTO [Case]
		(
			[RBSE] ,
			[CPHH],
			[IsNonGBCase],
			[Eartag],
			[EartagHerdmark],
			[EartagCountry],
			[Fate],
			[FinalResult],
			[FinalResultDate],
			[SlaughterDate]
		)
	VALUES
		(
			@RBSE,
			@CPHH,
			1,
			@Eartag,
			@EartagHerdmark,
			@EartagCountry,
			@Fate,
			@FinalResult,
			@FinalResultDate,
			@SlaughterDate
		)	
	
	SET @ErrorCode = @@ERROR
	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 6
	END

	-- Update the Audit Log with the Case Creation
	INSERT INTO [AuditLog]
		(
		[TableName],
		[UserID],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	VALUES
		(
		'Case',
		@UserID,
		LEFT(@RBSE, 2) + '/' + SUBSTRING(@RBSE, 3, 2) + '/' + RIGHT(@RBSE, 5),
		'Creation',
		@RBSE
		)

	SET @ErrorCode = @@ERROR
	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 7
	END

	-- Create CaseWork
	INSERT INTO [CaseWork]
           (
           [RBSE],
           [RBSEDate],
           [Barcode],
           [AHFReference]
           )
     VALUES
           (
            @RBSE,
            @RBSEDate,
            @Barcode,
            @AHFReference
           )

	SET @ErrorCode = @@ERROR
	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 8
	END

COMMIT TRANSACTION

