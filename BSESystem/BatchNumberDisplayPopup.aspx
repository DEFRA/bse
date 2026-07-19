<%@ Page Language="vb" AutoEventWireup="false" Codebehind="BatchNumberDisplayPopup.aspx.vb" Inherits="BSESystem.BatchNumberDisplayPopup" %>
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
	<body bgColor="white">
		<form id="Calendar" method="post" runat="server">
			<asp:datagrid id="grdBatchNumbers" runat="server" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
				<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
				<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
				<ItemStyle CssClass="GridItem"></ItemStyle>
				<HeaderStyle CssClass="GridHeader"></HeaderStyle>
				<Columns>
					<asp:BoundColumn DataField="BatchNumber" SortExpression="BatchNumber" ReadOnly="True" HeaderText="Batch Number">
						<HeaderStyle Width="150px"></HeaderStyle>
					</asp:BoundColumn>
					<asp:BoundColumn DataField="Document" SortExpression="Document" ReadOnly="True" HeaderText="Document">
						<HeaderStyle Width="120px"></HeaderStyle>
					</asp:BoundColumn>
				</Columns>
				<PagerStyle Visible="False"></PagerStyle>
			</asp:datagrid>
		</form>
	</body>
</HTML>
