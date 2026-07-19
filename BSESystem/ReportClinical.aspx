<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ReportClinical.aspx.vb" Inherits="BSESystem.ReportClinical" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ReportClinical</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<style type="text/css">
		BODY { COLOR: black; FONT-FAMILY: Arial, Helvetica, sans-serif }
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
						Clinical Report
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
			<P align="left"><asp:repeater id="Repeater1" runat="server">
					<HeaderTemplate>
					</HeaderTemplate>
					<ItemTemplate>
						<table class="ReportTable" cellspacing="0" cellpadding="0" border="0">
							<tr>
								<th>Case -
									<%# DataBinder.Eval(Container, "DataItem.DisplayRBSE")%>
								</th>
								<th>CPHH -
									<%# DataBinder.Eval(Container, "DataItem.DisplayCPHH")%>
								</th>
							</tr>
						</table>
						<br>
						<table class="ReportTable" cellspacing="0" cellpadding="0" border="0">
							<tr>
								<td width="50" rowspan="9"></td>
								<td>APPR</td>
								<td><%# DataBinder.Eval(Container, "DataItem.Apprehension")%></td>
								<td>ABNO</td>
								<td><%# DataBinder.Eval(Container, "DataItem.AbnormalBehaviour")%></td>
							</tr>
							<tr>
								<td>TOUCH</td>
								<td><%# DataBinder.Eval(Container, "DataItem.HypersensitiveTouch")%></td>
								<td>SHY</td>
								<td><%# DataBinder.Eval(Container, "DataItem.HeadShyness")%></td>
							</tr>
							<tr>
								<td>SOUND</td>
								<td><%# DataBinder.Eval(Container, "DataItem.HypersensitiveSound")%></td>
								<td>FLANK</td>
								<td><%# DataBinder.Eval(Container, "DataItem.LickingFlank")%></td>
							</tr>
							<tr>
								<td>MANIC</td>
								<td><%# DataBinder.Eval(Container, "DataItem.Maniacal")%></td>
								<td>NOSE</td>
								<td><%# DataBinder.Eval(Container, "DataItem.LickingNose")%></td>
							</tr>	
							<tr>
								<td>PANIC</td>
								<td><%# DataBinder.Eval(Container, "DataItem.PanicStricken")%></td>
								<td>KICK</td>
								<td><%# DataBinder.Eval(Container, "DataItem.Kicking")%></td>
							</tr>
							<tr>
								<td>TEMP</td>
								<td><%# DataBinder.Eval(Container, "DataItem.TemperamentChange")%></td>
								<td>RELUC</td>
								<td><%# DataBinder.Eval(Container, "DataItem.ReluctantDoorways")%></td>
							</tr>
							<tr>
								<td>ABNOR</td>
								<td><%# DataBinder.Eval(Container, "DataItem.AbnormalHeadCarriage")%></td>
								<td>PRESS</td>
								<td><%# DataBinder.Eval(Container, "DataItem.HeadPressing")%></td>
							</tr>
							<tr>
								<td>TWITCH</td>
								<td><%# DataBinder.Eval(Container, "DataItem.EarTwitching")%></td>
								<td>RUB</td>
								<td><%# DataBinder.Eval(Container, "DataItem.HeadRubbing")%></td>
							</tr>
							<tr>
								<td>ANGLE</td>
								<td><%# DataBinder.Eval(Container, "DataItem.EarsOddAngle")%></td>
								<td>TEETH</td>
								<td><%# DataBinder.Eval(Container, "DataItem.TeethGrinding")%></td>
							</tr>
							<tr><td>&nbsp;</td></tr>
							<tr>
								<td>BLIND</td>
								<td><%# DataBinder.Eval(Container, "DataItem.Blindness")%></td>
								<td>FALL</td>
								<td><%# DataBinder.Eval(Container, "DataItem.Falling")%></td>
								<td>REC</td>
								<td><%# DataBinder.Eval(Container, "DataItem.Recumbent")%></td>
							</tr>
							<tr>
								<td>CIRCLE</td>
								<td><%# DataBinder.Eval(Container, "DataItem.Circling")%></td>
								<td>PAR</td>
								<td><%# DataBinder.Eval(Container, "DataItem.Paresis")%></td>
								<td>TREM</td>
								<td><%# DataBinder.Eval(Container, "DataItem.Tremor")%></td>
							</tr>							
							<tr>
								<td>H ATAX</td>
								<td><%# DataBinder.Eval(Container, "DataItem.HindAtaxia")%></td>
								<td>F ATX</td>
								<td><%# DataBinder.Eval(Container, "DataItem.ForeAtaxia")%></td>
								<td>KNUCK</td>
								<td><%# DataBinder.Eval(Container, "DataItem.KnucklingFetlock")%></td>
							</tr>
							<tr><td>&nbsp;</td></tr>
							<tr>
								<td>WT</td>
								<td><%# DataBinder.Eval(Container, "DataItem.WeightLoss")%></td>
								<td>COND</td>
								<td><%# DataBinder.Eval(Container, "DataItem.ConditionLoss")%></td>
								<td>MILK</td>
								<td><%# DataBinder.Eval(Container, "DataItem.MilkYield")%></td>
							</tr>
						</table>
					</ItemTemplate>
					<SeparatorTemplate>
						<HR width="100%" SIZE="1">
						<br>
					</SeparatorTemplate>
					<FooterTemplate>
					</FooterTemplate>
				</asp:repeater><br>
			</P>
			</FONT></form>
	</body>
</HTML>
