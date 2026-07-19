<%@ Import Namespace="System.Data" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ReportPedigree.aspx.vb" Inherits="BSESystem.ReportPedigree" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ReportPedigree</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<style type="text/css">
		BODY, P { COLOR: black; FONT-FAMILY: Arial, Helvetica, sans-serif }
		.ReportTable TH { BORDER-RIGHT: none; PADDING-RIGHT: 4px; BORDER-TOP: none; PADDING-LEFT: 4px; FONT-WEIGHT: bold; FONT-SIZE: 100%; PADDING-BOTTOM: 4px; BORDER-LEFT: none; PADDING-TOP: 4px; BORDER-BOTTOM: none; TEXT-ALIGN: left; VERTICAL-ALIGN: baseline }
		.ReportTable TD { BORDER-RIGHT: none; PADDING-RIGHT: 4px; BORDER-TOP: none; PADDING-LEFT: 4px; FONT-WEIGHT: normal; FONT-SIZE: 90%; PADDING-BOTTOM: 4px; MARGIN: 0px; BORDER-LEFT: none; PADDING-TOP: 4px; BORDER-BOTTOM: none; TEXT-ALIGN: left }
		.ReportTableSmall TH { BORDER-RIGHT: none; PADDING-RIGHT: 4px; BORDER-TOP: none; PADDING-LEFT: 4px; FONT-WEIGHT: bold; FONT-SIZE: 70%; PADDING-BOTTOM: 4px; BORDER-LEFT: none; PADDING-TOP: 4px; BORDER-BOTTOM: none; TEXT-ALIGN: left; VERTICAL-ALIGN: baseline }
		.ReportTableSmall TD { BORDER-RIGHT: none; PADDING-RIGHT: 4px; BORDER-TOP: none; PADDING-LEFT: 4px; FONT-WEIGHT: normal; FONT-SIZE: 50%; PADDING-BOTTOM: 4px; MARGIN: 0px; BORDER-LEFT: none; PADDING-TOP: 4px; BORDER-BOTTOM: none; TEXT-ALIGN: left }
		</style>
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P align="left">
				<TABLE class="ReportTable" cellSpacing="0" cellPadding="0" border="0">
					<tr style="FONT-SIZE: 200%" >
						<td colspan="3">
						Pedigree Report
						</td>
					</tr>
					<tr>
						<th>
							Batch No.</th>
						<th>
							Date Run</th>
						<th>
							Number In Batch</th>
						</TR>
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
					<table class="ReportTable" cellspacing="0" cellpadding="0" border="0">
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
					<table class="ReportTableSmall" cellspacing="0" cellpadding="0" border="0">
						<tr>
							<th style="FONT-SIZE: 80%; TEXT-DECORATION: underline" colspan="8">
							Sire Detail:
							</th>
						</tr>
						<tr>
							<th width="50">
								ID
							</th>
							<th width="50">
								RBSE
							</th>
							<th width="50">
								EARTAG
							</th>
							<th width="120">
								NAME
							</th>
							<th width="90">
								DATE OF BIRTH
							</th>
							<th width="120">
								FATE
							</th>
							<th width="90">
								HERD BOOK
							</th>
							<th width="120">
								ALT NAME
							</th>
						</tr>
						<tr>
							<td width="50">
							<%# DataBinder.Eval(Container, "DataItem.SireID")%>
							</td>
							<td width="50">
							<%# DataBinder.Eval(Container, "DataItem.SireDisplayRBSE")%>
							</td>
							<td width="50">
							<%# DataBinder.Eval(Container, "DataItem.SireEarTag")%>
							</td>
							<td width="120">
							<%# DataBinder.Eval(Container, "DataItem.SireName")%>	
							</td>						
							<td width="90">
							<%# DataBinder.Eval(Container, "DataItem.SireBirthDate")%>
							</td>
							<td width="120">
							<%# DataBinder.Eval(Container, "DataItem.SireFate")%>
							</td>
							<td width="50">
							<%# DataBinder.Eval(Container, "DataItem.SireHerdBook")%>
							</td>
							<td width="50">
							<%# DataBinder.Eval(Container, "DataItem.SireAlternativeName")%>
							</td>
						</tr>
						<tr>
							<th style="FONT-SIZE: 80%; TEXT-DECORATION: underline" colspan="9">
							Dam Detail:
							</th>
						</tr>
						<tr>
							<th width="50">
								ID
							</th>
							<th width="50">
								RBSE
							</th>
							<th width="50">
								EARTAG
							</th>
							<th width="120">
								NAME
							</th>
							<th width="90">
								DATE OF BIRTH
							</th>
							<th width="120">
								FATE
							</th>
							<th width="90">
								HERD BOOK
							</th>
							<th width="120">
								ALT NAME
							</th>
							<th width="120">
								STATUS
							</th>
						</tr>
						<tr>
							<td width="50">
							<%# DataBinder.Eval(Container, "DataItem.DamID")%>
							</td>
							<td width="50">
							<%# DataBinder.Eval(Container, "DataItem.DamDisplayRBSE")%>
							</td>
							<td width="50">
							<%# DataBinder.Eval(Container, "DataItem.DamEarTag")%>
							</td>
							<td width="120">
							<%# DataBinder.Eval(Container, "DataItem.DamName")%>	
							</td>						
							<td width="90">
							<%# DataBinder.Eval(Container, "DataItem.DamBirthDate")%>
							</td>
							<td width="120">
							<%# DataBinder.Eval(Container, "DataItem.DamFate")%>
							</td>
							<td width="50">
							<%# DataBinder.Eval(Container, "DataItem.DamHerdBook")%>
							</td>
							<td width="50">
							<%# DataBinder.Eval(Container, "DataItem.DamAlternativeName")%>
							</td>
							<td width="120">
							<%# Databinder.Eval(Container, "DataItem.DamStatus")%>
							</td>
						</tr>
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
