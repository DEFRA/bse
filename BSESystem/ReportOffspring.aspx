<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ReportOffspring.aspx.vb" Inherits="BSESystem.ReportOffspring" %>
<%@ Import Namespace="System.Data" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ReportOffspring</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<style type="text/css">
		BODY { COLOR: black; FONT-FAMILY: Arial, Helvetica, sans-serif }
		.ReportTable TH { BORDER-RIGHT: medium none; PADDING-RIGHT: 4px; BORDER-TOP: medium none; PADDING-LEFT: 4px; FONT-WEIGHT: bold; FONT-SIZE: 100%; PADDING-BOTTOM: 4px; BORDER-LEFT: medium none; PADDING-TOP: 4px; BORDER-BOTTOM: medium none; TEXT-ALIGN: left; VERTICAL-ALIGN: baseline }
		.ReportTable TD { BORDER-RIGHT: medium none; PADDING-RIGHT: 4px; BORDER-TOP: medium none; PADDING-LEFT: 4px; FONT-WEIGHT: normal; FONT-SIZE: 90%; PADDING-BOTTOM: 4px; MARGIN: 0px; BORDER-LEFT: medium none; PADDING-TOP: 4px; BORDER-BOTTOM: medium none; TEXT-ALIGN: left }
		.ReportTableSmall TH { BORDER-RIGHT: medium none; PADDING-RIGHT: 4px; BORDER-TOP: medium none; PADDING-LEFT: 4px; FONT-WEIGHT: bold; FONT-SIZE: 75%; PADDING-BOTTOM: 4px; BORDER-LEFT: medium none; PADDING-TOP: 4px; BORDER-BOTTOM: medium none; TEXT-ALIGN: left; VERTICAL-ALIGN: baseline}
		.ReportTableSmall TD { BORDER-RIGHT: medium none; PADDING-RIGHT: 4px; BORDER-TOP: medium none; PADDING-LEFT: 4px; FONT-WEIGHT: normal; FONT-SIZE: 65%; PADDING-BOTTOM: 4px; MARGIN: 0px; BORDER-LEFT: medium none; PADDING-TOP: 4px; BORDER-BOTTOM: medium none; TEXT-ALIGN: left }
		</style>
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P align="left">
				<TABLE class="ReportTable" cellSpacing="0" cellPadding="0" border="0">
					<tr style="FONT-SIZE: 200%">
						<td colspan="3">
							Offspring Report
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
				<HR width="100%" SIZE="1">
				<asp:repeater id="parentRepeater" runat="server">
					<HeaderTemplate>
					</HeaderTemplate>
					<ItemTemplate>
						<table class="ReportTable" cellspacing="0" cellpadding="0" border="0" bordercolor="black">
							<tr>
								<th>
									Case -
									<%# DataBinder.Eval(Container, "DataItem.DisplayRBSE")%>
								</th>
								<th>
									CPHH -
									<%# DataBinder.Eval(Container, "DataItem.DisplayCPHH")%>
								</th>
							</tr>
						</table>
						<br>
						<asp:Repeater ID="childRepeater" Runat="server" DataSource='<%#	Container.DataItem.Row.GetChildRows("CaseRelations")%>'>
							<HeaderTemplate>
								<table class="ReportTableSmall" cellpadding="0" cellspacing="0" border="0">
									<tr>
										<th width="55">
											BIRTH</th>
										<th width="55">
											SEX</th>
										<th width="75">
											RELATION TYPE</th>
										<th width="115">
											EARTAG</th>
										<th width="80">
											FATE</th>
										<th width="80">
											DATE LEFT</th>
										<th width="130">
											RBSE OF RELATION</th>
										<th width="120">
											SIRE</th>
									</tr>
							</HeaderTemplate>
							<ItemTemplate>
								<tr>
									<td width="55"><%# Container.DataItem("BirthDate")%></td>
									<td width="55"><%# Container.DataItem("SexDesc")%></td>
									<td width="75"><%# Container.DataItem("RelationTypeDesc")%></td>
									<td width="115"><%# Container.DataItem("EartagHerdmark")%>
										<%# Container.DataItem("Eartag")%>
									</td>
									<td width="80"><%# Container.DataItem("RelationFateDesc")%></td>
									<td width="80"><%# Container.DataItem("LeftDate")%></td>
									<td width="130"><%# Container.DataItem("RelationRBSE")%></td>
									<td width="120"><%# Container.DataItem("Sire")%></td>
								</tr>
							</ItemTemplate>
						</asp:Repeater>
						</table>
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
