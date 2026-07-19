set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


ALTER PROCEDURE [dbo].[AddCaseRelation]
	@RBSE char(9),
	@RelationType varchar(11),
	@RelationRBSE char(9),
	@Sex char(1),
	@BirthDay tinyint,
	@BirthMonth tinyint,
	@BirthYear smallint,
	@RelationFate varchar(12),
	@LeftDate datetime,
	@EartagCountry varchar(2),
	@EartagHerdmark varchar(8),
	@Eartag varchar(40),
	@Sire varchar(80) AS

	IF @RelationRBSE IS NULL BEGIN	
		INSERT INTO [CaseRelation]
			(
			[RBSE],
			[RelationType],
			[RelationRBSE],
			[Sex],
			[BirthDay],
			[BirthMonth],
			[BirthYear],
			[RelationFate],
			[LeftDate],
			[EartagCountry],
			[EartagHerdmark],
			[Eartag],
			[Sire]
			)
		VALUES
			(
			@RBSE,
			@RelationType,
			@RelationRBSE,
			@Sex,
			@BirthDay,
			@BirthMonth,
			@BirthYear,
			@RelationFate,
			@LeftDate,
			@EartagCountry,
			@EartagHerdmark,
			@Eartag,
			@Sire
			)
		
	END ELSE BEGIN

		INSERT INTO [CaseRelation]
			(
			[RBSE],
			[RelationType],
			[RelationRBSE],
			[Sex],
			[BirthDay],
			[BirthMonth],
			[BirthYear],
			[RelationFate],
			[LeftDate],
			[EartagCountry],
			[EartagHerdmark],
			[Eartag],
			[Sire]
			)
		VALUES
			(
			@RBSE,
			@RelationType,
			@RelationRBSE,
			NULL,
			NULL,
			NULL,
			NULL,
			NULL,
			NULL,
			NULL,
			NULL,
			NULL,
			NULL
			)
	END

 