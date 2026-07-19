<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CalendarPopup.aspx.vb" Inherits="BSESystem.CalendarPopup" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>Popup</title>
		<meta content="True" name="vs_showGrid">
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
		<STYLE>
		</STYLE>
	</HEAD>
	<body>
		<form id="Calendar" method="post" runat="server">
			<div style="LEFT:0px;POSITION:absolute;TOP:0px">
				<asp:Calendar id="Calendar1" runat="server" BorderStyle="Solid" NextPrevFormat="ShortMonth" BackColor="White" Width="187px" ForeColor="Black" CellSpacing="1" Height="141px" Font-Size="9pt" Font-Names="Verdana" BorderColor="Black">
					<TodayDayStyle ForeColor="Black" BackColor="White"></TodayDayStyle>
					<DayStyle BackColor="#33CCFF"></DayStyle>
					<NextPrevStyle Font-Size="8pt" Font-Bold="True" ForeColor="#333399"></NextPrevStyle>
					<DayHeaderStyle Font-Size="8pt" Font-Bold="True" Height="8pt" ForeColor="#333333"></DayHeaderStyle>
					<SelectedDayStyle ForeColor="White" BackColor="#333399"></SelectedDayStyle>
					<TitleStyle Font-Size="12pt" Font-Bold="True" Height="12pt" ForeColor="#333399" BackColor="White"></TitleStyle>
					<OtherMonthDayStyle ForeColor="#999999"></OtherMonthDayStyle>
				</asp:Calendar>
			</div>
			<asp:Literal id="litText" runat="server"></asp:Literal>
		</form>
	</body>
</HTML>
