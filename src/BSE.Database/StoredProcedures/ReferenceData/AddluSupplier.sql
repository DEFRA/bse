
CREATE PROCEDURE AddluSupplier
    @Name varchar(30),
    @Details varchar(60),
    @ID int OUTPUT
AS
    DECLARE
        @ErrorCode int

    INSERT INTO luSupplier
        ([Name], [Details])
    VALUES
        (@Name, @Details)
        
    SET @ErrorCode = @@Error
    
    IF @ErrorCode = 0 BEGIN
        SET @ID = SCOPE_IDENTITY()
        RETURN 0
    END ELSE BEGIN
        RETURN @ErrorCode
    END
