<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>

<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="PicklistMaintenanceAHRO.aspx.vb"
    Inherits="BSESystem.PicklistMaintenanceAHRO" %>

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>BSE System : Pick List Maintenance</title>
    <meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
    <meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
    <meta content="JavaScript" name="vs_defaultClientScript">
    <meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
    <link href="Style/vla-ie.css" type="text/css" rel="stylesheet">
</head>
<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 96px" ms_positioning="GridLayout"><asp:dropdownlist id="ddlEditableLookups" style="Z-INDEX: 100; LEFT: 120px; POSITION: absolute; TOP: 14px" runat="server" AutoPostBack="True" Width="160px"></asp:dropdownlist><asp:label id="lblSelectATable" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="88px">Select a Table</asp:label>
				<asp:Label id="lblTruncated" style="Z-INDEX: 102; LEFT: 304px; POSITION: absolute; TOP: 16px" runat="server" Width="430px">If you enter a string longer than the database allows then it will be truncated.  Check the help to see how many characters each field allows.</asp:Label>
				<asp:Label id="lblSystem" style="Z-INDEX: 103; LEFT: 304px; POSITION: absolute; TOP: 56px" runat="server" Width="430px">View the help file before adding any new codes to find out whether it will affect the functionality of other parts of the system.</asp:Label></DIV>
			<P><asp:datagrid id="grdLookup" runat="server" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select"></asp:ButtonColumn>
						
						<asp:BoundColumn DataField="Name" SortExpression="Name" HeaderText="Name">
							<HeaderStyle Width="400px"></HeaderStyle>
						</asp:BoundColumn>											
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 28px" ms_positioning="GridLayout">
				<P><uc1:datagridpager id="Pager" runat="server"></uc1:datagridpager></P>
			</DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</form>
	</body>
</html>
