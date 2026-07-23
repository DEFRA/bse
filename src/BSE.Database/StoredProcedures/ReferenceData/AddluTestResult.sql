
CREATE PROCEDURE AddluTestResult
    @Code varchar(5),
    @Description varchar(50)
AS
	INSERT INTO luTestResult
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
