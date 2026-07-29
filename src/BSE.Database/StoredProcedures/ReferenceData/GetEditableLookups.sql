
CREATE PROCEDURE GetEditableLookups AS
    	SELECT
		[ID],
		[TableName],
		[Description]
	FROM 
		EditableLookup
	ORDER BY
		[Description]

	RETURN 0
