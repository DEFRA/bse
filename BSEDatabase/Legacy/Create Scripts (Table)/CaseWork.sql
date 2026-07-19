/****** Object:  Table [dbo].[CaseWork]    Script Date: 01/24/2008 16:20:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[CaseWork](
	[RBSE] [char](9) COLLATE Latin1_General_CI_AS NOT NULL,
	[RBSEDate] [datetime] NOT NULL,
	[Barcode] [varchar](20) COLLATE Latin1_General_CI_AS NULL,
	[AHFReference] [varchar](40) COLLATE Latin1_General_CI_AS NULL,
	[PurchaserBSE1ReceivedDate] [datetime] NULL,
	[BreederBSE1ReceivedDate] [datetime] NULL,
	[Vendor1BSE1ReceivedDate] [datetime] NULL,
	[HomebredBSE1ReceivedDate] [datetime] NULL,
	[SummarySheetReceivedDate] [datetime] NULL,
	[PaperworkCompleteDate] [datetime] NULL,
	[ActiveMemoDate] [datetime] NULL,
	[AnnexADate] [datetime] NULL,
	[AnnexBDate] [datetime] NULL,
	[AnnexCDate] [datetime] NULL,
	[AnnexDDate] [datetime] NULL,
	[RegionalLab] [char](4) COLLATE Latin1_General_CI_AS NULL,
	[ReceivedByRegionalLabDate] [datetime] NULL,
	[InitialReceivedDate] [datetime] NULL,
	[FinalReceivedDate] [datetime] NULL,
	[FinalSentDate] [datetime] NULL,
	[LabChasedDate] [datetime] NULL,
	[BarbMinuteSentDate] [datetime] NULL,
	[Post2000SentDate] [datetime] NULL,
	[CaseWorkNotes] [varchar](500) COLLATE Latin1_General_CI_AS NULL,
	[DataCompleteDate] [datetime] NULL,
	[IsCaseClosed] [bit] NOT NULL CONSTRAINT [DF_CaseWork_IsCaseClosed]  DEFAULT (0),
	[RowStamp] [timestamp] NOT NULL,
 CONSTRAINT [PK_CaseWork] PRIMARY KEY CLUSTERED 
(
	[RBSE] ASC
) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO

GO
ALTER TABLE [dbo].[CaseWork]  WITH NOCHECK ADD  CONSTRAINT [FK_CaseWork_Case] FOREIGN KEY([RBSE])
REFERENCES [dbo].[Case] ([RBSE])
GO
ALTER TABLE [dbo].[CaseWork] CHECK CONSTRAINT [FK_CaseWork_Case]
GO
ALTER TABLE [dbo].[CaseWork]  WITH NOCHECK ADD  CONSTRAINT [FK_CaseWork_luRegionalLab] FOREIGN KEY([RegionalLab])
REFERENCES [dbo].[luRegionalLab] ([Code])
GO
ALTER TABLE [dbo].[CaseWork] CHECK CONSTRAINT [FK_CaseWork_luRegionalLab]
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_ActiveMemoDate] CHECK  (([ActiveMemoDate] is null or [ActiveMemoDate] >= [RBSEDate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_AnnexADate] CHECK  (([AnnexADate] is null or [AnnexADate] > [RBSEDate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_AnnexBDate] CHECK  (([AnnexBDate] is null or [AnnexBDate] > [AnnexADate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_AnnexCDate] CHECK  (([AnnexCDate] is null or [AnnexCDate] > [RBSEDate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_AnnexDDate] CHECK  (([AnnexDDate] is null or [AnnexDDate] > [AnnexCDate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_BarbMinuteSentDate] CHECK  (([BarbMinuteSentDate] is null or [BarbMinuteSentDate] > [RBSEDate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_BreederBSE1ReceivedDate] CHECK  (([BreederBSE1ReceivedDate] is null or [BreederBSE1ReceivedDate] > [RBSEDate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_DataCompleteDate] CHECK  (([DataCompleteDate] is null or [DataCompleteDate] > [RBSEDate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_FinalReceivedDate] CHECK  (([FinalReceivedDate] is null or [FinalReceivedDate] > [RBSEDate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_FinalSentDate] CHECK  (([FinalSentDate] is null or [FinalSentDate] >= [FinalReceivedDate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_HomebredBSE1ReceivedDate] CHECK  (([HomebredBSE1ReceivedDate] is null or [HomebredBSE1ReceivedDate] > [RBSEDate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_LabChasedDate] CHECK  (([LabChasedDate] is null or [LabChasedDate] > [RBSEDate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_PaperworkCompleteDate] CHECK  (([PaperworkCompleteDate] is null or [PaperworkCompleteDate] > [RBSEDate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_Post2000SentDate] CHECK  (([Post2000SentDate] is null or [Post2000SentDate] > [RBSEDate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_PurchaserBSE1ReceivedDate] CHECK  (([PurchaserBSE1ReceivedDate] is null or [PurchaserBSE1ReceivedDate] > [RBSEDate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_SummarySheetReceivedDate] CHECK  (([SummarySheetReceivedDate] is null or [SummarySheetReceivedDate] > [RBSEDate]))
GO
ALTER TABLE [dbo].[CaseWork]  WITH CHECK ADD  CONSTRAINT [CK_CaseWork_Vendor1BSE1ReceivedDate] CHECK  (([Vendor1BSE1ReceivedDate] is null or [Vendor1BSE1ReceivedDate] > [RBSEDate])) 