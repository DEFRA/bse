
CREATE PROCEDURE GetPossibleSuppliers
	@Name varchar(30)
AS

	SELECT
		[ID],
		[Name],
		[Details]
	FROM
		[luSupplier]
	WHERE
		(Name LIKE '%' + @Name + '%')
	ORDER BY
		[Name]
	
	RETURN
