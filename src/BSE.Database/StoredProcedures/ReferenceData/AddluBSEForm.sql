

CREATE PROCEDURE [dbo].[AddluBSEForm]
    @Code char(2),
    @Description varchar(50)
AS
	INSERT INTO luBSEForm
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN 
