CREATE PROCEDURE DeleteCase
	@RBSE char(9),
	@UserID int
AS
DECLARE
        @FarmRowCount int,
        @CPHH char(11)

/*
Refactored from @@ERROR to BEGIN TRY / BEGIN CATCH / ROLLBACK TRANSACTION.
Business rule pre-checks return specific codes; SQL errors propagate via THROW.
Return codes: 0=success, 1=RBSE not found, 2=farm count error, 3=has linked records,
              4=audit log error, 5=delete error, 6=farm delete error.
*/
BEGIN TRY
        BEGIN TRANSACTION

        SELECT @CPHH = [CPHH] FROM [Case] WHERE [RBSE] = @RBSE
        IF @@ROWCOUNT = 0 BEGIN ROLLBACK TRANSACTION; RETURN 1 END

        SELECT [RBSE] FROM [Case] WHERE [CPHH] = @CPHH
        SET @FarmRowCount = @@ROWCOUNT
        IF @FarmRowCount = 0 BEGIN ROLLBACK TRANSACTION; RETURN 2 END

        -- Block deletion if linked records exist
        IF EXISTS (SELECT [RBSE] FROM [CaseFeed]     WHERE [RBSE] = @RBSE)
        OR EXISTS (SELECT [RBSE] FROM [CaseClinical]  WHERE [RBSE] = @RBSE)
        OR EXISTS (SELECT [RBSE] FROM [CaseRelation]  WHERE [RBSE] = @RBSE)
        OR EXISTS (SELECT [RBSE] FROM [OtherOwner]    WHERE [RBSE] = @RBSE)
        OR EXISTS (SELECT [RBSE] FROM [lnkBatchCase]  WHERE [RBSE] = @RBSE)
        OR EXISTS (SELECT [RBSE] FROM [ClinicalVisit] WHERE [RBSE] = @RBSE)
        BEGIN ROLLBACK TRANSACTION; RETURN 3 END

        SET NOCOUNT ON

        INSERT INTO [AuditLog]([TableName],[UserID],[BeforeValue],[Reason],[RBSE])
        VALUES('Case',@UserID,LEFT(@RBSE,2)+'/'+SUBSTRING(@RBSE,3,2)+'/'+RIGHT(@RBSE,5),'Deletion',@RBSE)

        DELETE FROM [CaseBAB]  WHERE [RBSE] = @RBSE
        DELETE FROM [CaseTest] WHERE [RBSE] = @RBSE
        DELETE FROM [CaseWork] WHERE [RBSE] = @RBSE
        DELETE FROM [Case]     WHERE [RBSE] = @RBSE

        IF @FarmRowCount = 1 BEGIN
                INSERT INTO [AuditLog]([TableName],[UserID],[BeforeValue],[Reason],[CPHH])
                VALUES('Farm',@UserID,LEFT(@CPHH,2)+'/'+SUBSTRING(@CPHH,3,3)+'/'+SUBSTRING(@CPHH,6,4)+'/'+RIGHT(@CPHH,2),'Deletion',@CPHH)
                DELETE FROM [Farm] WHERE [CPHH] = @CPHH
        END

        COMMIT TRANSACTION
        RETURN 0

END TRY
BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
        THROW
END CATCH


