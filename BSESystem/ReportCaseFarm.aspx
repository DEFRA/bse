<%@ Import Namespace="System.Data" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ReportCaseFarm.aspx.vb" Inherits="BSESystem.ReportCaseFarm" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ReportCaseFarm</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<style type="text/css">
		 BODY { COLOR: black; FONT-FAMILY: Arial, Helvetica, sans-serif }
		 .ReportTable TH { BORDER-RIGHT: medium none; PADDING-RIGHT: 8px; BORDER-TOP: medium none; PADDING-LEFT: 8px; FONT-WEIGHT: bold; FONT-SIZE: 90%; PADDING-BOTTOM: 4px; VERTICAL-ALIGN: baseline; BORDER-LEFT: medium none; PADDING-TOP: 4px; BORDER-BOTTOM: medium none; TEXT-ALIGN: left }
		 .ReportTable TD { BORDER-RIGHT: medium none; PADDING-RIGHT: 8px; BORDER-TOP: medium none; PADDING-LEFT: 8px; FONT-WEIGHT: normal; FONT-SIZE: 80%; PADDING-BOTTOM: 4px; MARGIN: 0px; BORDER-LEFT: medium none; PADDING-TOP: 4px; BORDER-BOTTOM: medium none; TEXT-ALIGN: left }
		 .ReportTableSmall TH { BORDER-RIGHT: medium none; PADDING-RIGHT: 4px; BORDER-TOP: medium none; PADDING-LEFT: 4px; FONT-WEIGHT: bold; FONT-SIZE: 70%; PADDING-BOTTOM: 4px; VERTICAL-ALIGN: baseline; BORDER-LEFT: medium none; PADDING-TOP: 4px; BORDER-BOTTOM: medium none; TEXT-ALIGN: left }
		 .ReportTableSmall TD { BORDER-RIGHT: medium none; PADDING-RIGHT: 4px; BORDER-TOP: medium none; PADDING-LEFT: 4px; FONT-WEIGHT: normal; FONT-SIZE: 65%; PADDING-BOTTOM: 4px; MARGIN: 0px; BORDER-LEFT: medium none; PADDING-TOP: 4px; BORDER-BOTTOM: medium none; TEXT-ALIGN: left }
		</style>
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P align="left">
				<TABLE class="ReportTable" cellSpacing="0" cellPadding="0" border="1">
					<tr style="FONT-SIZE: 200%">
						<td colspan="3">
							Case And Farm Report
						</td>
					</tr>
					<TR>
						<th>
							Batch No.</th>
						<th>
							Date Run</th>
						<th>
							Number In Batch</th></TR>
					<TR>
						<td><asp:label id="lblBatchNumber" runat="server">[lblBatchNumber]</asp:label></td>
						<td><asp:label id="lblDateRun" runat="server">[lblDateRun]</asp:label></td>
						<td><asp:label id="lblNumberInBatch" runat="server">[lblNumberInBatch]</asp:label></td>
					</TR>
				</TABLE>
			</P>
			<HR width="100%" SIZE="1">
			<asp:repeater id="parentRepeater" runat="server">
				<HeaderTemplate>
				</HeaderTemplate>
				<ItemTemplate>
					<table class="ReportTable" cellpadding="0" cellspacing="0" border="1">
						<tr>
							<th width="80">
								RBSE
							</th>
							<th width="70">
								EARTAG
							</th>
							<th width="100">
								CPHH
							</th>
							<th width="200">
								OWNER
							</th>
							<th width="200">
								Address
							</th>
						</tr>
						<tr>
							<td width="80">
								<%# DataBinder.Eval(Container, "DataItem.DisplayRBSE")%>
							</td>
							<td width="70">
								<%# DataBinder.Eval(Container, "DataItem.EartagHerdMark")%>
								<%# DataBinder.Eval(Container, "DataItem.EarTag")%>
							</td>
							<td width="100">
								<%# DataBinder.Eval(Container, "DataItem.DisplayCPHH")%>
							</td>
							<td width="200">
								<%# DataBinder.Eval(Container, "DataItem.OwnerName")%>
							</td>
							<td width="200">
								<%# DataBinder.Eval(Container, "DataItem.Address1")%>
							</td>
						</tr>
					</table>
					<br />
					<table class="ReportTableSmall" cellpadding="0" cellspacing="0" border="1">
						<tr>
							<th width="50">
								BIRTH
							</th>
							<th width="50">
								BAB
							</th>
							<th width="50">
								AGE
							</th>
							<th width="50">
								SEX
							</th>
							<th width="50">
								BREED
							</th>
							<th width="50">
								HB/P
							</th>
							<th width="70">
								DATE P
							</th>
							<th width="70">
								AGE P
							</th>
						</tr>
						<tr>
							<td width="50">&nbsp;<%# DataBinder.Eval(Container, "DataItem.BirthDate")%>&nbsp;</td>
							<td width="50">&nbsp;<%# DataBinder.Eval(Container, "DataItem.IsBAB")%>&nbsp;</td>
							<td width="50">&nbsp;<%# DataBinder.Eval(Container, "DataItem.OnsetAgeInMonths")%>&nbsp;</td>
							<td width="50">&nbsp;<%# DataBinder.Eval(Container, "DataItem.Sex")%>&nbsp;</td>
							<td width="50">&nbsp;<%# DataBinder.Eval(Container, "DataItem.Breed")%>&nbsp;</td>
							<td width="50">&nbsp;<%# DataBinder.Eval(Container, "DataItem.Origin")%>&nbsp;</td>
							<td width="70">&nbsp;<%# DataBinder.Eval(Container, "DataItem.PurchaseDate")%>&nbsp;</td>
							<td width="70">&nbsp;<%# DataBinder.Eval(Container, "DataItem.PurchaseAgeInMonths")%>&nbsp;</td>
						</tr>
					</table>
					<br />
					<table class="ReportTableSmall" cellpadding="0" cellspacing="0" border="1">
						<tr>
							<th width="300">
								PREVIOUS OWNER
							</th>
						</tr>
						
							<asp:Repeater ID="PreviousOwnerRepeater" Runat="server" DataSource='<%#	Container.DataItem.Row.GetChildRows("CasePreviousOwnerRelation")%>'>
								<HeaderTemplate>
								</HeaderTemplate>
								<ItemTemplate>
									<tr>
										<td width="300">&nbsp;<%# Container.DataItem("PreviousOwner")%>&nbsp;</td>
									</tr>
								</ItemTemplate>
							</asp:Repeater>
					</table>
					<br />
					<table class="ReportTableSmall" cellpadding="0" cellspacing="0" border="1">
						<tr>
							<th width="100">
								ENTRY
							</th>
							<th width="100">
								ONSET
							</th>
							<th width="100">
								PREG
							</th>
							<th width="300">
								MONTHS POST CALVING.
							</th>
						</tr>
						<tr>
							<td width="100">&nbsp;<%# DataBinder.Eval(Container, "DataItem.HerdEntryDate")%>&nbsp;</td>
							<td width="100">&nbsp;<%# DataBinder.Eval(Container, "DataItem.OnsetDate")%>&nbsp;</td>
							<td width="100">&nbsp;<%# DataBinder.Eval(Container, "DataItem.MonthsPregnant")%>&nbsp;</td>
							<td width="300">&nbsp;<%# DataBinder.Eval(Container, "DataItem.MonthsPostCalving")%>&nbsp;</td>
						</tr>
					</table>
					<br />
					<table class="ReportTableSmall" cellpadding="0" cellspacing="0" border="0">
						<tr>
							<td valign="top" align="left">
								<table class="ReportTableSmall" cellpadding="0" cellspacing="0" border="1">
									<tr>
										<th>
											H TYPE
										</th>
									</tr>
									<tr>
										<td>&nbsp;<%# DataBinder.Eval(Container, "DataItem.HerdType")%>
											<%# DataBinder.Eval(Container, "DataItem.PedigreeType")%>
											&nbsp;</td>
									</tr>
								</table>
							</td>
							<td>&nbsp;</td>
							<td>
								<asp:Repeater ID="childRepeater" Runat="server" DataSource='<%#	Container.DataItem.Row.GetChildRows("FarmHerdRelation")%>'>
									<HeaderTemplate>
										<table class="ReportTableSmall" cellpadding="0" cellspacing="0" border="1" width="700px">
											<tr>
												<th>
													&nbsp;H SIZE&nbsp;</th>
												<th colspan="11">
													&nbsp;LACTATION DISTRIBUTION&nbsp;</th>
											</tr>
									</HeaderTemplate>
									<ItemTemplate>
										<tr>
											<td width="150">
												<%# Container.DataItem("HerdYear")%>
												<%# Container.DataItem("TotalSize")%>
											</td>
											<td width="30">
												<%# Container.DataItem("Lactation1Size")%>
											</td>
											<td width="30">
												<%# Container.DataItem("Lactation2Size")%>
											</td>
											<td width="30">
												<%# Container.DataItem("Lactation3Size")%>
											</td>
											<td width="30">
												<%# Container.DataItem("Lactation4Size")%>
											</td>
											<td width="30">
												<%# Container.DataItem("Lactation5Size")%>
											</td>
											<td width="30">
												<%# Container.DataItem("Lactation6Size")%>
											</td>
											<td width="30">
												<%# Container.DataItem("Lactation7Size")%>
											</td>
											<td width="30">
												<%# Container.DataItem("Lactation8Size")%>
											</td>
											<td width="30">
												<%# Container.DataItem("Lactation9Size")%>
											</td>
											<td width="30">
												<%# Container.DataItem("Lactation10Size")%>
											</td>
											<td width="30">
												<%# Container.DataItem("Lactation10PlusSize")%>
											</td>
										</tr>
									</ItemTemplate>
								</asp:Repeater>
					</table>
					</td> </tr> </table>
				</ItemTemplate>
				<FooterTemplate>
				</FooterTemplate>
				<SeparatorTemplate>
					<HR width="100%" SIZE="1">
					<br>
				</SeparatorTemplate>
			</asp:repeater>
		</form>
	</body>
</HTML>
