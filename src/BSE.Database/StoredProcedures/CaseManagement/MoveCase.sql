
CREATE PROCEDURE MoveCase
	@RBSE char(9),
	@NewCPHH char(11),
	@UserID int
AS
DECLARE
        @OldCasesOnFarm int,
        @OldCPHH char(11)

/*
Refactored from @@ERROR to BEGIN TRY / BEGIN CATCH / ROLLBACK TRANSACTION.
Return codes: 0=success, 1=new farm not found, 2=RBSE not found, 3=no cases on old farm,
              4=audit log error, 5=case update error, 6=farm deletion error.
Business rule pre-checks retain specific codes; SQL errors propagate via THROW.
*/
BEGIN TRY
        BEGIN TRANSACTION

        IF NOT EXISTS (SELECT [CPHH] FROM [Farm] WHERE [CPHH] = @NewCPHH)
        BEGIN ROLLBACK TRANSACTION; RETURN 1 END

        SELECT @OldCPHH = [CPHH] FROM [Case] WHERE [RBSE] = @RBSE
        IF @@ROWCOUNT = 0 BEGIN ROLLBACK TRANSACTION; RETURN 2 END

        SELECT @OldCasesOnFarm = COUNT(*) FROM [Case] WHERE [CPHH] = @OldCPHH
        IF @OldCasesOnFarm = 0 BEGIN ROLLBACK TRANSACTION; RETURN 3 END

        SET NOCOUNT ON

        INSERT INTO [AuditLog]([TableName],[FieldName],[UserID],[BeforeValue],[AfterValue],[Reason],[RBSE])
        VALUES('Case','CPHH',@UserID,
               LEFT(@OldCPHH,2)+'/'+SUBSTRING(@OldCPHH,3,3)+'/'+SUBSTRING(@OldCPHH,6,4)+'/'+RIGHT(@OldCPHH,2),
               LEFT(@NewCPHH,2)+'/'+SUBSTRING(@NewCPHH,3,3)+'/'+SUBSTRING(@NewCPHH,6,4)+'/'+RIGHT(@NewCPHH,2),
               'Case Move',@RBSE)

        UPDATE [Case] SET [CPHH] = @NewCPHH WHERE [RBSE] = @RBSE

        IF @OldCasesOnFarm = 1 BEGIN
                INSERT INTO [AuditLog]([TableName],[UserID],[BeforeValue],[Reason],[CPHH])
                VALUES('Farm',@UserID,
                       LEFT(@OldCPHH,2)+'/'+SUBSTRING(@OldCPHH,3,3)+'/'+SUBSTRING(@OldCPHH,6,4)+'/'+RIGHT(@OldCPHH,2),
                       'Deletion',@OldCPHH)
                DELETE FROM [Farm] WHERE [CPHH] = @OldCPHH
        END

        COMMIT TRANSACTION
        RETURN 0

END TRY
BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
        THROW
END CATCH
