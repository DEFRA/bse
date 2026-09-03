
CREATE PROCEDURE AddluFeedRisk
    @Code varchar(10),
    @Description varchar(50)
AS
	INSERT INTO luFeedRisk
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
