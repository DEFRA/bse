
CREATE PROCEDURE AddluRelationType
    @Code varchar(11),
    @Description varchar(50)
AS
	INSERT INTO luRelationType
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
