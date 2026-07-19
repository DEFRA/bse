<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="FarmAuditLogReport.aspx.vb" Inherits="BSESystem.FarmAuditLogReport"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Farm Audit Log Report</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P>
				<uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 54px" ms_positioning="GridLayout">
				<asp:label id="lblCPHH" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="364px" Font-Bold="True">CPHH: 123</asp:label>
				<P>&nbsp;</P>
				<HR style="Z-INDEX: 103; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 48px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:Button id="btnReturn" style="Z-INDEX: 102; LEFT: 672px; POSITION: absolute; TOP: 16px" runat="server" Text="Return"></asp:Button>
			</DIV>
			<P>
				<asp:datagrid id="grdResults" runat="server" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True" EnableViewState="False">
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
			<P>&nbsp;&nbsp;&nbsp;</P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 62px" ms_positioning="GridLayout">
				<P>
					<asp:hyperlink id="hlbExcel" style="Z-INDEX: 101; LEFT: 640px; POSITION: absolute; TOP: 40px" runat="server" Width="102px" Visible="False" Target="_blank" NavigateUrl="ExcelExport.aspx">Export to Excel</asp:hyperlink>
					<uc1:DataGridPager id="ResultsPager" runat="server"></uc1:DataGridPager></P>
			</DIV>
			<P>
				<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
		</form>
	</body>
</HTML>
