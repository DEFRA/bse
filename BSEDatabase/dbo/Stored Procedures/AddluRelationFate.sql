
CREATE PROCEDURE AddluRelationFate
	@Code varchar(12),
	@Description varchar(50),
	@IsActive bit
AS
	INSERT INTO luRelationFate
        		([Code], [Description], [IsActive])
	VALUES
		(@Code, @Description, @IsActive)
        
RETURN
