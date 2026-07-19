<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="BSESSCheckByDate.aspx.vb" Inherits="BSESystem.BSESSCheckByDate" %>
<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : BSESS Check : Cases Within A Date Range</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 72px" ms_positioning="GridLayout">
				<P></P>
				<asp:button id="btnSearch" style="Z-INDEX: 100; LEFT: 624px; POSITION: absolute; TOP: 8px" runat="server" Width="84px" Text="Search"></asp:button>
				<asp:label id="Label2" style="Z-INDEX: 101; LEFT: 360px; POSITION: absolute; TOP: 16px" runat="server" Width="24px">and</asp:label>
				<asp:label id="lblDateRange" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px"> Notification Date Between</asp:label>
				<DIV style="Z-INDEX: 103; LEFT: 184px; WIDTH: 161px; POSITION: absolute; TOP: 8px; HEIGHT: 48px" ms_positioning="FlowLayout">
					<uc1:CalendarDate id="ctlStartDate" runat="server"></uc1:CalendarDate></DIV>
				<P>&nbsp;</P>
				<DIV style="Z-INDEX: 105; LEFT: 400px; WIDTH: 161px; POSITION: absolute; TOP: 8px; HEIGHT: 48px" ms_positioning="FlowLayout">
					<uc1:CalendarDate id="ctlEndDate" runat="server"></uc1:CalendarDate></DIV>
				<P></P>
				<HR style="Z-INDEX: 106; LEFT: 8px; WIDTH: 96.14%; POSITION: absolute; TOP: 64px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<INPUT style="Z-INDEX: 107; LEFT: 608px; WIDTH: 122px; POSITION: absolute; TOP: 40px; HEIGHT: 21px" onclick="location.href='BSESSMenu.aspx'" type="button" value="BSESS Menu">
			</DIV>
			<asp:datagrid id="grdResults" runat="server" Width="738px" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
				<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
				<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
				<ItemStyle CssClass="GridItem"></ItemStyle>
				<HeaderStyle CssClass="GridHeader"></HeaderStyle>
				<Columns>
					<asp:BoundColumn DataField="RBSE" SortExpression="RBSE" HeaderText="RBSE"></asp:BoundColumn>
					<asp:BoundColumn DataField="BSESSBirthDate" SortExpression="BSESSBirthDate" HeaderText="BSESS Birth Date" DataFormatString="{0:d}"></asp:BoundColumn>
					<asp:BoundColumn DataField="BSEBirthDate" SortExpression="BSEBirthDate" HeaderText="BSE Birth Date" DataFormatString="{0:d}"></asp:BoundColumn>
					<asp:BoundColumn DataField="BSESSEartag" SortExpression="BSESSEartag" HeaderText="BSESS Eartag"></asp:BoundColumn>
					<asp:BoundColumn DataField="BSEEartag" SortExpression="BSEEartag" HeaderText="BSE Eartag"></asp:BoundColumn>
					<asp:BoundColumn DataField="BSESSTestGroup" SortExpression="BSESSTestGroup" HeaderText="BSESS Test Group"></asp:BoundColumn>
					<asp:BoundColumn DataField="BSETestGroup" SortExpression="BSETestGroup" HeaderText="BSE Test Group"></asp:BoundColumn>
				</Columns>
				<PagerStyle Visible="False"></PagerStyle>
			</asp:datagrid>
			<DIV style="WIDTH: 751px; POSITION: relative; HEIGHT: 54px" ms_positioning="GridLayout"><uc1:datagridpager id="ResultsPager" runat="server"></uc1:datagridpager><asp:hyperlink id="hlbExcel" style="Z-INDEX: 101; LEFT: 648px; POSITION: absolute; TOP: 32px" runat="server" Width="102px" Visible="False" Target="_blank" NavigateUrl="ExcelExport.aspx">Export to Excel</asp:hyperlink></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
