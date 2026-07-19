<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="AuditLogByUser.aspx.vb" Inherits="BSESystem.AuditLogByUser" %>
<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : User Audit Log Report</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="WIDTH: 744px; POSITION: relative; HEIGHT: 96px" ms_positioning="GridLayout">
				<asp:button id="btnSearch" style="Z-INDEX: 101; LEFT: 500px; POSITION: absolute; TOP: 48px" runat="server" Width="84px" Text="Search"></asp:button>
				<asp:label id="lblUser" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 56px" runat="server" Width="152px">User</asp:label>
				<asp:label id="Label2" style="Z-INDEX: 103; LEFT: 380px; POSITION: absolute; TOP: 16px" runat="server" Width="24px">and</asp:label>
				<asp:label id="lblDateRange" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px"> Log Entries Between</asp:label>
				<asp:label id="lblError" style="Z-INDEX: 106; LEFT: 352px; POSITION: absolute; TOP: 56px" runat="server" Visible="False" EnableViewState="False" CssClass="ValidatorText" ToolTip="Please select a user">*</asp:label>
				<DIV style="Z-INDEX: 107; LEFT: 184px; WIDTH: 161px; POSITION: absolute; TOP: 8px; HEIGHT: 48px" ms_positioning="FlowLayout">
					<uc1:CalendarDate id="ctlStartDate" runat="server"></uc1:CalendarDate>
				</DIV>
				<DIV style="Z-INDEX: 108; LEFT: 420px; WIDTH: 161px; POSITION: absolute; TOP: 8px; HEIGHT: 48px" ms_positioning="FlowLayout">
					<uc1:CalendarDate id="ctlEndDate" runat="server"></uc1:CalendarDate>
				</DIV>
				<asp:DropDownList id="ddlUser" style="Z-INDEX: 105; LEFT: 184px; POSITION: absolute; TOP: 56px" runat="server" Width="168px"></asp:DropDownList>
				<HR style="Z-INDEX: 109; LEFT: 8px; WIDTH: 96.14%; POSITION: absolute; TOP: 88px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<INPUT style="Z-INDEX: 110; LEFT: 600px; POSITION: absolute; TOP: 48px" type="button" value="Audit Log Menu" onclick="location.href='AuditLogMenu.aspx'">
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
			<DIV style="WIDTH: 751px; POSITION: relative; HEIGHT: 54px" ms_positioning="GridLayout">
				<uc1:datagridpager id="ResultsPager" runat="server"></uc1:datagridpager>
				<asp:hyperlink id="hlbExcel" style="Z-INDEX: 101; LEFT: 648px; POSITION: absolute; TOP: 32px" runat="server" Width="102px" NavigateUrl="ExcelExport.aspx" Target="_blank" Visible="False">Export to Excel</asp:hyperlink></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
