
CREATE PROCEDURE AddluTestType
	@Code varchar(10),
	@Description varchar(50),
	@IsActive bit
AS
	INSERT INTO luTestType
        		([Code], [Description], [IsActive])
	VALUES
		(@Code, @Description, @IsActive)
        
RETURN
