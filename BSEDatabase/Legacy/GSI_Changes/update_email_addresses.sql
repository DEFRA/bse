DECLARE @emailChange TABLE(id INT, old_suffix VARCHAR(50), new_suffix VARCHAR(50))
INSERT INTO @emailChange(id, old_suffix, new_suffix) VALUES (1,'@apha.gsi.gov.uk','@apha.gov.uk')
INSERT INTO @emailChange(id, old_suffix, new_suffix) VALUES (2,'@vla.defra.gsi.gov.uk','@apha.gov.uk')
INSERT INTO @emailChange(id, old_suffix, new_suffix) VALUES (3,'@animalhealth.gsi.gov.uk','@apha.gov.uk')
INSERT INTO @emailChange(id, old_suffix, new_suffix) VALUES (4,'@ahvla.gsi.gov.uk','@apha.gov.uk')
INSERT INTO @emailChange(id, old_suffix, new_suffix) VALUES (5,'@scotland.gsi.gov.uk','@gov.scot')
INSERT INTO @emailChange(id, old_suffix, new_suffix) VALUES (6,'@defra.gsi.gov.uk','@defra.gov.uk')

DECLARE @email_suffix_count INT  = (SELECT COUNT(*) FROM @emailChange)
DECLARE @i INT = 0

WHILE @i < @email_suffix_count
BEGIN
SELECT @i = @i + 1

DECLARE @old_email_suffix VARCHAR(50) = (SELECT old_suffix FROM @emailChange WHERE ID=@i)
DECLARE @new_email_suffix VARCHAR(50) = (SELECT new_suffix FROM @emailChange WHERE ID=@i)

	UPDATE [User]
	SET Email = REPLACE(Email, @old_email_suffix, @new_email_suffix)
	WHERE Email like '%' + @old_email_suffix
 
END

GO