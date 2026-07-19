/* Creates ADNS_Refs table for storing last ADNS reference details */


GO
/****** Object:  Table [dbo].[ADNSArea]    Script Date: 01/22/2008 10:58:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ADNSArea](
	[Area] [char](2) COLLATE Latin1_General_CI_AS NOT NULL,
	[LastEmailReference] [varchar](50) COLLATE Latin1_General_CI_AS NOT NULL,
	[LastADNSReferenceYear] [smallint] NOT NULL,
	[LastADNSReferenceNumber] [int] NOT NULL,
 CONSTRAINT [PK_ADNS_Refs] PRIMARY KEY CLUSTERED 
(
	[Area] ASC
) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO

GO
ALTER TABLE [dbo].[ADNSArea]  WITH CHECK ADD  CONSTRAINT [CK_ADNS_Refs] CHECK  (([Area] = 'GB' or [Area] = 'CI' or [Area] = 'NI'))
GO
EXEC dbo.sp_addextendedproperty @name=N'MS_Description', @value=N'Area must be GB, CI, or NI' ,@level0type=N'USER', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'ADNSArea', @level2type=N'CONSTRAINT', @level2name=N'CK_ADNS_Refs'

