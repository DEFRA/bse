
CREATE PROCEDURE AddluReportedLocation
    @Code varchar(5),
    @Description varchar(50)
AS
	INSERT INTO luReportedLocation
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
