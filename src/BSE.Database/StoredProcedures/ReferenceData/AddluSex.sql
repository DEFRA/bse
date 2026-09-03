
CREATE PROCEDURE AddluSex
    @Code char(1),
    @Description varchar(50)
AS
	INSERT INTO luSex
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
