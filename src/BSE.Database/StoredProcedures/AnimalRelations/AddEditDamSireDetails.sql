
CREATE PROCEDURE AddEditDamSireDetails
	@RBSE char(9),
	@DamID int,
	@DamRBSE char(9),
	@DamName varchar(80),
	@DamEartag varchar(15),
	@DamHerdbook varchar(15),
	@DamBirthDay tinyint,
	@DamBirthMonth tinyint,
	@DamBirthYear smallint,
	@DamRowStamp timestamp,
	@SireID int,
	@SireRBSE char(9),
	@SireName varchar(80),
	@SireEartag varchar(15),
	@SireHerdbook varchar(15),
	@SireBirthDay tinyint,
	@SireBirthMonth tinyint,
	@SireBirthYear smallint,
	@SireRowStamp timestamp,
	@CaseHerdbook varchar(15),
	@CaseRowStamp timestamp
 AS

	DECLARE
		@CasePedigreeID int,
		@ErrorCode int,
		@RowCount int

	/*
	This stored procedure is designed to be called from within a transaction, which should be rolled back if a return code greater than 0 is returned
	*/

	-- work out what the ID of the case's Pedigree record is, if any
	SELECT
		@CasePedigreeID = [ID]
	FROM
		[Pedigree]
	WHERE
		[RBSE] = @RBSE

	--create or update the dam record
	IF @DamID = 0 BEGIN

		INSERT INTO [Pedigree]
			(
			[RBSE],
			[Eartag],
			[Name],
			[Herdbook],
			[Sex],
			[BirthDay],
			[BirthMonth],
			[BirthYear]
			)
		VALUES
			(
			@DamRBSE,
			@DamEartag,
			@DamName,
			@DamHerdbook,
			CASE WHEN @DamRBSE IS NULL THEN 'F' ELSE NULL END,
			CASE WHEN @DamRBSE IS NULL THEN @DamBirthDay ELSE NULL END,
			CASE WHEN @DamRBSE IS NULL THEN @DamBirthMonth ELSE NULL END,
			CASE WHEN @DamRBSE IS NULL THEN @DamBirthYear ELSE NULL END
			)

		SELECT
			@DamID = SCOPE_IDENTITY(),
			@RowCount = @@ROWCOUNT,
			@ErrorCode = @@ERROR

		IF @RowCount <> 1 OR @ErrorCode <> 0 BEGIN
			RETURN 1
		END

	END ELSE BEGIN

		IF @DamID IS NOT NULL BEGIN

			DECLARE
				@CurrentDamRBSE char(9),
				@CurrentDamEartag varchar(15),
				@CurrentDamName varchar(80),
				@CurrentDamHerdbook varchar(15),
				@CurrentDamBirthDay tinyint,
				@CurrentDamBirthMonth tinyint,
				@CurrentDamBirthYear smallint

			SELECT
				@CurrentDamRBSE = [RBSE],
				@CurrentDamEartag = [Eartag],
				@CurrentDamName = [Name],
				@CurrentDamHerdbook = [Herdbook],
				@CurrentDamBirthDay = [BirthDay],
				@CurrentDamBirthMonth = [BirthMonth],
				@CurrentDamBirthYear = [BirthYear]
			FROM
				[Pedigree]
			WHERE
				[ID] = @DamID

			IF @DamRBSE IS NOT NULL BEGIN

				SET @DamBirthDay = NULL
				SET @DamBirthMonth = NULL
				SET @DamBirthYear = NULL
			
			END

			IF (((@CurrentDamRBSE != @DamRBSE) OR (@CurrentDamRBSE IS NULL AND @DamRBSE IS NOT NULL) OR (@CurrentDamRBSE IS NOT NULL AND @DamRBSE IS NULL)) OR
			((@CurrentDamEartag != @DamEartag) OR (@CurrentDamEartag IS NULL AND @DamEartag IS NOT NULL) OR (@CurrentDamEartag IS NOT NULL AND @DamEartag IS NULL)) OR
			((@CurrentDamName != @DamName) OR (@CurrentDamName IS NULL AND @DamName IS NOT NULL) OR (@CurrentDamName IS NOT NULL AND @DamName IS NULL)) OR
			((@CurrentDamHerdbook != @DamHerdbook) OR (@CurrentDamHerdbook IS NULL AND @DamHerdbook IS NOT NULL) OR (@CurrentDamHerdbook IS NOT NULL AND @DamHerdbook IS NULL)) OR
			((@CurrentDamBirthDay != @DamBirthDay) OR (@CurrentDamBirthDay IS NULL AND @DamBirthDay IS NOT NULL) OR (@CurrentDamBirthDay IS NOT NULL AND @DamBirthDay IS NULL)) OR
			((@CurrentDamBirthMonth != @DamBirthMonth) OR (@CurrentDamBirthMonth IS NULL AND @DamBirthMonth IS NOT NULL) OR (@CurrentDamBirthMonth IS NOT NULL AND @DamBirthMonth IS NULL)) OR
			((@CurrentDamBirthYear != @DamBirthYear) OR (@CurrentDamBirthYear IS NULL AND @DamBirthYear IS NOT NULL) OR (@CurrentDamBirthYear IS NOT NULL AND @DamBirthYear IS NULL)) ) BEGIN
				
				UPDATE
					[Pedigree]
				SET
					[RBSE] = @DamRBSE,
					[Eartag] = @DamEartag,
					[Name] = @DamName,
					[Herdbook] = @DamHerdbook,
					[BirthDay] = CASE WHEN @DamRBSE IS NULL THEN @DamBirthDay ELSE NULL END,
					[BirthMonth] = CASE WHEN @DamRBSE IS NULL THEN @DamBirthMonth ELSE NULL END,
					[BirthYear] = CASE WHEN @DamRBSE IS NULL THEN @DamBirthYear ELSE NULL END
				WHERE
					[ID] = @DamID AND 
					[RowStamp] = @DamRowStamp
	
				SELECT
					@RowCount = @@ROWCOUNT,
					@ErrorCode = @@ERROR
		
				IF @RowCount <> 1 OR @ErrorCode <> 0 BEGIN
					RETURN 1
				END
			END
		END
	END

	-- create or update the sire record
	IF @SireID = 0 BEGIN

		INSERT INTO [Pedigree]
			(
			[RBSE],
			[Eartag],
			[Name],
			[Herdbook],
			[Sex],
			[BirthDay],
			[BirthMonth],
			[BirthYear]
			)
		VALUES
			(
			@SireRBSE,
			@SireEartag,
			@SireName,
			@SireHerdbook,
			CASE WHEN @SireRBSE IS NULL THEN 'M' ELSE NULL END,
			CASE WHEN @SireRBSE IS NULL THEN @SireBirthDay ELSE NULL END,
			CASE WHEN @SireRBSE IS NULL THEN @SireBirthMonth ELSE NULL END,
			CASE WHEN @SireRBSE IS NULL THEN @SireBirthYear ELSE NULL END
			)

		SELECT
			@SireID = SCOPE_IDENTITY(),
			@RowCount = @@ROWCOUNT,
			@ErrorCode = @@ERROR

		IF @RowCount <> 1 OR @ErrorCode <> 0 BEGIN
			RETURN 2
		END

	END ELSE BEGIN

		IF @SireID IS NOT NULL BEGIN

			DECLARE
				@CurrentSireRBSE char(9),
				@CurrentSireEartag varchar(15),
				@CurrentSireName varchar(80),
				@CurrentSireHerdbook varchar(15),
				@CurrentSireBirthDay tinyint,
				@CurrentSireBirthMonth tinyint,
				@CurrentSireBirthYear smallint

			SELECT
				@CurrentSireRBSE = [RBSE],
				@CurrentSireEartag = [Eartag],
				@CurrentSireName = [Name],
				@CurrentSireHerdbook = [Herdbook],
				@CurrentSireBirthDay = [BirthDay],
				@CurrentSireBirthMonth = [BirthMonth],
				@CurrentSireBirthYear = [BirthYear]
			FROM
				[Pedigree]
			WHERE
				[ID] = @SireID

			IF @SireRBSE IS NOT NULL BEGIN

				SET @SireBirthDay = NULL
				SET @SireBirthMonth = NULL
				SET @SireBirthYear = NULL
			
			END

			IF (((@CurrentSireRBSE != @SireRBSE) OR (@CurrentSireRBSE IS NULL AND @SireRBSE IS NOT NULL) OR (@CurrentSireRBSE IS NOT NULL AND @SireRBSE IS NULL)) OR
			((@CurrentSireEartag != @SireEartag) OR (@CurrentSireEartag IS NULL AND @SireEartag IS NOT NULL) OR (@CurrentSireEartag IS NOT NULL AND @SireEartag IS NULL)) OR
			((@CurrentSireName != @SireName) OR (@CurrentSireName IS NULL AND @SireName IS NOT NULL) OR (@CurrentSireName IS NOT NULL AND @SireName IS NULL)) OR
			((@CurrentSireHerdbook != @SireHerdbook) OR (@CurrentSireHerdbook IS NULL AND @SireHerdbook IS NOT NULL) OR (@CurrentSireHerdbook IS NOT NULL AND @SireHerdbook IS NULL)) OR
			((@CurrentSireBirthDay != @SireBirthDay) OR (@CurrentSireBirthDay IS NULL AND @SireBirthDay IS NOT NULL) OR (@CurrentSireBirthDay IS NOT NULL AND @SireBirthDay IS NULL)) OR
			((@CurrentSireBirthMonth != @SireBirthMonth) OR (@CurrentSireBirthMonth IS NULL AND @SireBirthMonth IS NOT NULL) OR (@CurrentSireBirthMonth IS NOT NULL AND @SireBirthMonth IS NULL)) OR
			((@CurrentSireBirthYear != @SireBirthYear) OR (@CurrentSireBirthYear IS NULL AND @SireBirthYear IS NOT NULL) OR (@CurrentSireBirthYear IS NOT NULL AND @SireBirthYear IS NULL)) ) BEGIN
				
				UPDATE
					[Pedigree]
				SET
					[RBSE] = @SireRBSE,
					[Eartag] = @SireEartag,
					[Name] = @SireName,
					[Herdbook] = @SireHerdbook,
					[BirthDay] = CASE WHEN @SireRBSE IS NULL THEN @SireBirthDay ELSE NULL END,
					[BirthMonth] = CASE WHEN @SireRBSE IS NULL THEN @SireBirthMonth ELSE NULL END,
					[BirthYear] = CASE WHEN @SireRBSE IS NULL THEN @SireBirthYear ELSE NULL END
				WHERE
					[ID] = @SireID AND 
					[RowStamp] = @SireRowStamp
		
				SELECT
					@RowCount = @@ROWCOUNT,
					@ErrorCode = @@ERROR
			
				IF @RowCount <> 1 OR @ErrorCode <> 0 BEGIN
					RETURN 2
				END
			END
		END

	END

	-- create or update the case's Pedigree record
	IF @CasePedigreeID IS NULL AND (@DamID IS NOT NULL OR @SireID IS NOT NULL OR @CaseHerdbook IS NOT NULL) BEGIN

		INSERT INTO [Pedigree]
			(
			[RBSE],
			[DamID],
			[SireID],
			[Herdbook]
			)
		VALUES
			(
			@RBSE,
			@DamID,
			@SireID,
			@CaseHerdbook
			)

		SELECT
			@RowCount = @@ROWCOUNT,
			@ErrorCode = @@ERROR
	
		IF @RowCount <> 1 OR @ErrorCode <> 0 BEGIN
			RETURN 3
		END

	END ELSE BEGIN

		-- the record may already exist; check we need to update it (we only need to update if the sire or the dam id or the herdbook has changed)
		SELECT
			[ID]
		FROM
			[Pedigree]
		WHERE
			[ID] = @CasePedigreeID AND
			((([DamID] != @DamID) OR ([DamID] IS NULL AND @DamID IS NOT NULL) OR ([DamID] IS NOT NULL AND @DamID IS NULL)) OR
			(([SireID] != @SireID) OR ([SireID] IS NULL AND @SireID IS NOT NULL) OR ([SireID] IS NOT NULL AND @SireID IS NULL))  OR
			(([Herdbook] != @CaseHerdbook) OR ([Herdbook] IS NULL AND @CaseHerdbook IS NOT NULL) OR ([Herdbook] IS NOT NULL AND @CaseHerdbook IS NULL))) 

		SET @RowCount = @@ROWCOUNT

		-- if 1 row was returned, we need to do an update
		IF @RowCount = 1 BEGIN
			UPDATE
				[Pedigree]
			SET
				[DamID] = @DamID,
				[SireID] = @SireID,
				[Herdbook] = @CaseHerdbook
			WHERE
				[ID] = @CasePedigreeID AND
				[RowStamp] = @CaseRowStamp

			SELECT
				@RowCount = @@ROWCOUNT,
				@ErrorCode = @@ERROR
		
			IF @RowCount <> 1 OR @ErrorCode <> 0 BEGIN
				RETURN 4
			END

		END

		
	END
