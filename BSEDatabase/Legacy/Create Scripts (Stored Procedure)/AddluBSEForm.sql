set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


CREATE PROCEDURE [dbo].[AddluBSEForm]
    @Code char(2),
    @Description varchar(50)
AS
	INSERT INTO luBSEForm
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN 