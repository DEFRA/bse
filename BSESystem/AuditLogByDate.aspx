<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="AuditLogByDate.aspx.vb" Inherits="BSESystem.AuditLogByDate" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Daily Audit Log Report</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="WIDTH: 744px; POSITION: relative; HEIGHT: 56px" ms_positioning="GridLayout">
				<P></P>
				<HR style="Z-INDEX: 101; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 48px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:button id="btnSearch" style="Z-INDEX: 102; LEFT: 504px; POSITION: absolute; TOP: 16px" runat="server" Width="84px" Text="Search"></asp:button>
				<asp:label id="lblDate" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px"> Log Entries On</asp:label>
				<DIV style="Z-INDEX: 104; LEFT: 192px; WIDTH: 200px; POSITION: absolute; TOP: 8px; HEIGHT: 27px" ms_positioning="FlowLayout">
					<uc1:CalendarDate id="ctlLogDate" runat="server"></uc1:CalendarDate></DIV>
				<INPUT style="Z-INDEX: 105; LEFT: 608px; POSITION: absolute; TOP: 16px" type="button" value="Audit Log Menu" onclick="location.href='AuditLogMenu.aspx'">
			</DIV>
			<asp:datagrid id="grdResults" runat="server" Width="568px" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
				<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
				<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
				<ItemStyle CssClass="GridItem"></ItemStyle>
				<HeaderStyle CssClass="GridHeader"></HeaderStyle>
				<Columns>
					<asp:BoundColumn DataField="TableName" SortExpression="TableName" ReadOnly="True" HeaderText="Table"></asp:BoundColumn>
					<asp:BoundColumn DataField="FieldName" SortExpression="FieldName" ReadOnly="True" HeaderText="Field"></asp:BoundColumn>
					<asp:BoundColumn DataField="DateTime" SortExpression="DateTime" ReadOnly="True" HeaderText="Date Time" DataFormatString="{0:G}"></asp:BoundColumn>
					<asp:BoundColumn DataField="UserName" SortExpression="UserName" ReadOnly="True" HeaderText="User"></asp:BoundColumn>
					<asp:BoundColumn DataField="BeforeValue" SortExpression="BeforeValue" ReadOnly="True" HeaderText="Before"></asp:BoundColumn>
					<asp:BoundColumn DataField="AfterValue" SortExpression="AfterValue" ReadOnly="True" HeaderText="After"></asp:BoundColumn>
					<asp:BoundColumn DataField="Reason" SortExpression="Reason" HeaderText="Reason"></asp:BoundColumn>
					<asp:BoundColumn DataField="Key" SortExpression="Key" HeaderText="Key"></asp:BoundColumn>
				</Columns>
				<PagerStyle Visible="False"></PagerStyle>
			</asp:datagrid>
			<DIV style="WIDTH: 751px; POSITION: relative; HEIGHT: 54px" ms_positioning="GridLayout"><uc1:datagridpager id="ResultsPager" runat="server"></uc1:datagridpager><asp:hyperlink id="hlbExcel" style="Z-INDEX: 101; LEFT: 648px; POSITION: absolute; TOP: 32px" runat="server" Width="102px" Visible="False" Target="_blank" NavigateUrl="ExcelExport.aspx">Export to Excel</asp:hyperlink></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
