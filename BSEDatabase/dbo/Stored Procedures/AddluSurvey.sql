
CREATE PROCEDURE AddluSurvey
    @Code varchar(4),
    @Description varchar(50)
AS
	INSERT INTO luSurvey
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
