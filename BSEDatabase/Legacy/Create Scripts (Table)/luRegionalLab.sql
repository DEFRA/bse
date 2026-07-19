
GO
/****** Object:  Table [dbo].[luRegionalLab]    Script Date: 01/23/2008 16:31:45 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[luRegionalLab](
	[Code] [char](4) COLLATE Latin1_General_CI_AS NOT NULL,
	[Description] [varchar](50) COLLATE Latin1_General_CI_AS NOT NULL,
 CONSTRAINT [PK_luRegionalLab] PRIMARY KEY CLUSTERED 
(
	[Code] ASC
) ON [PRIMARY]
) ON [PRIMARY]

GO

INSERT INTO [luRegionalLab] VALUES ('ABER','Aberystwyth')
INSERT INTO [luRegionalLab] VALUES ('BURY','Bury St Edmunds')
INSERT INTO [luRegionalLab] VALUES ('CARM','Carmarthen')
INSERT INTO [luRegionalLab] VALUES ('LANG','Langford')
INSERT INTO [luRegionalLab] VALUES ('LASS','Lasswade')
INSERT INTO [luRegionalLab] VALUES ('LUDD','Luddington')
INSERT INTO [luRegionalLab] VALUES ('NEWC','Newcastle')
INSERT INTO [luRegionalLab] VALUES ('PENR','Penrith')
INSERT INTO [luRegionalLab] VALUES ('PRES','Preston') 
INSERT INTO [luRegionalLab] VALUES ('SHRE','Shrewsbury')
INSERT INTO [luRegionalLab] VALUES ('STAR','Starcross')
INSERT INTO [luRegionalLab] VALUES ('SUTT','Sutton Bonington')
INSERT INTO [luRegionalLab] VALUES ('THIR','Thirsk')
INSERT INTO [luRegionalLab] VALUES ('TRUR','Truro')
INSERT INTO [luRegionalLab] VALUES ('WINC','Winchester')   

GO

SET ANSI_PADDING OFF 