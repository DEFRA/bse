CREATE PROCEDURE [dbo].[AddCase]
        @RBSE char(9),
        @CPHH char(11),
        @EartagCountry varchar(4),
        @EartagHerdmark varchar(8),
        @Eartag varchar(25),
        @PreviousEartag varchar(25),
        @BSE1ReceivedDate datetime,
        @FormADate datetime,
        @FormAResubmittedDate datetime,
        @FormBDate datetime,
        @Fate varchar(4),
        @FormCDate datetime,
        @IsPurchaserBSE1Received bit,
        @IsBreederBSE1Received bit,
        @IsVendor1BSE1Received bit,
        @IsHomebredBSE1Received bit,
        @IsSummarySheetReceived bit,
        @IsPaperworkComplete bit,
        @ReportedLocation varchar(5),
        @Survey varchar(4),
        @Notes varchar(500),
        @BirthDate datetime,
        @IsBirthDateEst bit,
        @DamStatus varchar(9),
        @BirthDateSource varchar(5),
        @ValuationAge char(1),
        @Sex char(1),
        @Breed varchar(20),
        @Origin char(1),
        @PurchaseDate datetime,
        @PurchaseAgeInMonths smallint,
        @PurchasedCounty varchar(15),
        @HerdEntryDate datetime,
        @OnsetDate datetime,
        @IsOnsetDateEst bit,
        @MonthsPregnant tinyint,
        @MonthsPostCalving tinyint,
        @OnsetAgeInMonths smallint,
        @SlaughterDate datetime,
        @UserID int,
        @AlternateDiagnosis varchar(255),
        @LabComment varchar(255),
        @CaseType varchar(2) AS
        /*
        Designed to be called from within a caller-managed transaction.
        RETURN 0 = success; RETURN 3 = duplicate RBSE (business rule pre-check).
        SQL errors propagate via THROW to the caller's CATCH block.
        */
        IF EXISTS (SELECT [RBSE] FROM [Case] WHERE [RBSE] = @RBSE) RETURN 3

        BEGIN TRY
                SET NOCOUNT ON
                INSERT INTO [AuditLog]([TableName],[UserID],[AfterValue],[Reason],[RBSE])
                VALUES('Case',@UserID,LEFT(@RBSE,2)+'/'+SUBSTRING(@RBSE,3,2)+'/'+RIGHT(@RBSE,5),'Creation',@RBSE)

                SET NOCOUNT OFF
                INSERT INTO [Case]([RBSE],[CPHH],[EartagCountry],[EartagHerdmark],[Eartag],[IsNonGBCase],
                        [PreviousEartag],[BSE1ReceivedDate],[FormADate],[FormAResubmittedDate],[FormBDate],
                        [Fate],[FormCDate],[IsPurchaserBSE1Received],[IsBreederBSE1Received],[IsVendor1BSE1Received],
                        [IsHomebredBSE1Received],[IsSummarySheetReceived],[IsPaperworkComplete],[ReportedLocation],
                        [Survey],[Notes],[BirthDate],[IsBAB],[IsBirthDateEst],[DamStatus],[BirthDateSource],
                        [ValuationAge],[Sex],[Breed],[Origin],[PurchaseDate],[PurchaseAgeInMonths],[PurchasedCounty],
                        [HerdEntryDate],[OnsetDate],[IsOnsetDateEst],[MonthsPregnant],[MonthsPostCalving],
                        [OnsetAgeInMonths],[SlaughterDate],[AlternateDiagnosis],[LabComment],[CaseType],[IsoFormatEarTag])
                VALUES(@RBSE,@CPHH,@EartagCountry,@EartagHerdmark,@Eartag,
                        CASE LEFT(@RBSE,4) WHEN '6300' THEN 1 WHEN '2300' THEN 1 ELSE 0 END,
                        @PreviousEartag,@BSE1ReceivedDate,@FormADate,@FormAResubmittedDate,@FormBDate,
                        @Fate,@FormCDate,@IsPurchaserBSE1Received,@IsBreederBSE1Received,@IsVendor1BSE1Received,
                        @IsHomebredBSE1Received,@IsSummarySheetReceived,@IsPaperworkComplete,@ReportedLocation,
                        @Survey,@Notes,@BirthDate,CASE WHEN @BirthDate>='18 July 1988' THEN 1 ELSE 0 END,
                        @IsBirthDateEst,@DamStatus,@BirthDateSource,@ValuationAge,@Sex,@Breed,@Origin,
                        @PurchaseDate,@PurchaseAgeInMonths,@PurchasedCounty,@HerdEntryDate,@OnsetDate,
                        @IsOnsetDateEst,@MonthsPregnant,@MonthsPostCalving,@OnsetAgeInMonths,@SlaughterDate,
                        @AlternateDiagnosis,@LabComment,@CaseType,dbo.IsIsoEarTag(@EartagCountry,@EartagHerdmark,@Eartag))
                RETURN 0
        END TRY
        BEGIN CATCH
                THROW
        END CATCH
