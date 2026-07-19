<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CaseAuditLogReport.aspx.vb" Inherits="BSESystem.CaseAuditLogReport"%>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Case Audit Log Report</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 56px" ms_positioning="GridLayout"><asp:label id="lblRBSE" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Font-Bold="True" Width="364px">RBSE: 123</asp:label>
				<P>&nbsp;</P>
				<HR style="Z-INDEX: 103; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 40px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:Button id="btnReturn" style="Z-INDEX: 104; LEFT: 664px; POSITION: absolute; TOP: 8px" runat="server" Text="Return"></asp:Button>
			</DIV>
			<P><asp:datagrid id="grdResults" runat="server" Width="568px" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True" EnableViewState="False">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="TableName" SortExpression="TableName" ReadOnly="True" HeaderText="Table"></asp:BoundColumn>
						<asp:BoundColumn DataField="FieldName" SortExpression="FieldName" ReadOnly="True" HeaderText="Field"></asp:BoundColumn>
						<asp:BoundColumn DataField="DateTime" SortExpression="DateTime" ReadOnly="True" HeaderText="Date Time"></asp:BoundColumn>
						<asp:BoundColumn DataField="UserName" SortExpression="UserName" ReadOnly="True" HeaderText="User"></asp:BoundColumn>
						<asp:BoundColumn DataField="BeforeValue" SortExpression="BeforeValue" ReadOnly="True" HeaderText="Before"></asp:BoundColumn>
						<asp:BoundColumn DataField="AfterValue" SortExpression="AfterValue" ReadOnly="True" HeaderText="After"></asp:BoundColumn>
						<asp:BoundColumn DataField="Reason" SortExpression="Reason" HeaderText="Reason"></asp:BoundColumn>
						<asp:BoundColumn DataField="Key" SortExpression="Key" HeaderText="Key"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 62px" ms_positioning="GridLayout">
				<P><asp:hyperlink id="hlbExcel" style="Z-INDEX: 101; LEFT: 640px; POSITION: absolute; TOP: 40px" runat="server" Width="102px" Visible="False" NavigateUrl="ExcelExport.aspx" Target="_blank">Export to Excel</asp:hyperlink><uc1:datagridpager id="ResultsPager" runat="server"></uc1:datagridpager></P>
			</DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</form>
	</body>
</HTML>
