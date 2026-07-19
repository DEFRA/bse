
GO
/****** Object:  Table [dbo].[luBSEForm]    Script Date: 01/24/2008 11:02:48 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[luBSEForm](
	[Code] [char](2) COLLATE Latin1_General_CI_AS NOT NULL,
	[Description] [varchar](50) COLLATE Latin1_General_CI_AS NOT NULL
	CONSTRAINT [PK_luBSEForm] PRIMARY KEY CLUSTERED 
(
	[Code] ASC
) ON [PRIMARY]
) ON [PRIMARY]

GO

INSERT INTO [luBSEForm] VALUES ('H','Homebred')
INSERT INTO [luBSEForm] VALUES ('B','Breeder')
INSERT INTO [luBSEForm] VALUES ('P','Purchaser')
INSERT INTO [luBSEForm] VALUES ('V','Vendor')
INSERT INTO [luBSEForm] VALUES ('SS','Summary Sheet')

GO

SET ANSI_PADDING OFF 