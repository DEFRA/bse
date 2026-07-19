<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ReportFeeds.aspx.vb" Inherits="BSESystem.ReportFeeds" %>
<%@ Import Namespace="System.Data" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ReportFeeds</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<style type="text/css">
		BODY { COLOR: black; FONT-FAMILY: Arial, Helvetica, sans-serif }
		.ReportTable TH { BORDER-RIGHT: medium none; PADDING-RIGHT: 4px; BORDER-TOP: medium none; PADDING-LEFT: 4px; FONT-WEIGHT: bold; FONT-SIZE: 100%; PADDING-BOTTOM: 4px; VERTICAL-ALIGN: baseline; BORDER-LEFT: medium none; PADDING-TOP: 4px; BORDER-BOTTOM: medium none; TEXT-ALIGN: left }
		.ReportTable TD { BORDER-RIGHT: medium none; PADDING-RIGHT: 4px; BORDER-TOP: medium none; PADDING-LEFT: 4px; FONT-WEIGHT: normal; FONT-SIZE: 90%; PADDING-BOTTOM: 4px; MARGIN: 0px; BORDER-LEFT: medium none; PADDING-TOP: 4px; BORDER-BOTTOM: medium none; TEXT-ALIGN: left }
		.ReportTableSmall TH { BORDER-RIGHT: medium none; PADDING-RIGHT: 4px; BORDER-TOP: medium none; PADDING-LEFT: 4px; FONT-WEIGHT: bold; FONT-SIZE: 70%; PADDING-BOTTOM: 4px; VERTICAL-ALIGN: baseline; BORDER-LEFT: medium none; PADDING-TOP: 4px; BORDER-BOTTOM: medium none; TEXT-ALIGN: left }
		.ReportTableSmall TD { BORDER-RIGHT: medium none; PADDING-RIGHT: 4px; BORDER-TOP: medium none; PADDING-LEFT: 4px; FONT-WEIGHT: normal; FONT-SIZE: 50%; PADDING-BOTTOM: 4px; MARGIN: 0px; BORDER-LEFT: medium none; PADDING-TOP: 4px; BORDER-BOTTOM: medium none; TEXT-ALIGN: left }
		</style>
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P align="left">
				<TABLE class="ReportTable" cellSpacing="0" cellPadding="0" border="0">
					<tr style="FONT-SIZE: 200%">
						<td colspan="3">
							Feeds&nbsp;Report
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
					<asp:Repeater ID="childRepeater" Runat="server" DataSource='<%#	Container.DataItem.Row.GetChildRows("CaseFeedsRelation")%>'>
						<HeaderTemplate>
							<table class="ReportTableSmall" cellpadding="0" cellspacing="0" border="0">
								<tr>
									<th width="90">
										Year From</th>
									<th width="80">
										Year To</th>
									<th width="120">
										Ration Type</th>
									<th width="120">
										Supplier Name</th>
									<th width="200">
										Ration Name</th>
									<th width="130">
										Is PrePurchase</th>
								</tr>
						</HeaderTemplate>
						<ItemTemplate>
							<tr>
								<td width="90"><%# Container.DataItem("YearFrom")%></td>
								<td width="80"><%# Container.DataItem("YearTo")%></td>
								<td width="50"><%# Container.DataItem("RationType")%></td>
								<td width="120"><%# Container.DataItem("Name")%></td>
								<td width="200"><%# Container.DataItem("RationName")%></td>
								<td width="115"><%# Container.DataItem("IsPrePurchase")%></td>
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
