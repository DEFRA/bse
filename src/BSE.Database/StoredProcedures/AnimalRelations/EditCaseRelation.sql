
CREATE PROCEDURE [dbo].[EditCaseRelation]
	@ID int,
	@RelationType varchar(11),
	@RelationRBSE char(9),
	@Sex char(1),
	@BirthDay tinyint,
	@BirthMonth tinyint,
	@BirthYear smallint,
	@RelationFate varchar(12),
	@LeftDate datetime,
	@EartagCountry varchar(4),
	@EartagHerdmark varchar(8),
	@Eartag varchar(40),
	@Sire varchar(80),
	@RowStamp timestamp AS

	IF @RelationRBSE IS NULL BEGIN

		UPDATE
			[CaseRelation]
		SET
			[RelationType] = @RelationType,
			[RelationRBSE] = @RelationRBSE,
			[Sex] = @Sex,
			[BirthDay] = @BirthDay,
			[BirthMonth] = @BirthMonth,
			[BirthYear] = @BirthYear,
			[RelationFate] = @RelationFate,
			[LeftDate] = @LeftDate,
			[EartagCountry] = @EartagCountry,
			[EartagHerdmark] = @EartagHerdmark,
			[Eartag] = @Eartag,
			[Sire] = @Sire,
			[IsoFormatEarTag] = dbo.IsIsoEarTag(@EartagCountry, @EartagHerdmark, @Eartag)
		WHERE
			[ID] = @ID AND
			[RowStamp] = @RowStamp

	END ELSE BEGIN

		UPDATE
			[CaseRelation]
		SET
			[RelationType] = @RelationType,
			[RelationRBSE] = @RelationRBSE,
			[Sex] = NULL,
			[BirthDay] = NULL,
			[BirthMonth] = NULL,
			[BirthYear] = NULL,
			[RelationFate] = NULL,
			[LeftDate] = NULL,
			[EartagCountry] = NULL,
			[EartagHerdmark] = NULL,
			[Eartag] = NULL,
			[Sire] = NULL
		WHERE
			[ID] = @ID AND
			[RowStamp] = @RowStamp

	END
