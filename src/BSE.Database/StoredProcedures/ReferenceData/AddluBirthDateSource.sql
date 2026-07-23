
CREATE PROCEDURE AddluBirthDateSource
    @Code varchar(5),
    @Description varchar(50)
AS
	INSERT INTO luBirthDateSource
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
