<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="PickListMaintenanceRelationFate.aspx.vb" Inherits="BSESystem.PickListMaintenanceRelationFate"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Pick List Maintenance</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<link href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P>
				<uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 96px" ms_positioning="GridLayout">
				<asp:DropDownList id="ddlEditableLookups" style="Z-INDEX: 100; LEFT: 120px; POSITION: absolute; TOP: 16px" runat="server" Width="160px" AutoPostBack="True"></asp:DropDownList>
				<asp:Label id="lblSelectATable" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="88px">Select a Table</asp:Label>
				<asp:Label id="lblTruncated" style="Z-INDEX: 102; LEFT: 304px; POSITION: absolute; TOP: 16px" runat="server" Width="430px">If you enter a string longer than the database allows then it will be  truncated. Check the help to see how many characters each field allows.</asp:Label>
				<asp:Label id="lblSystem" style="Z-INDEX: 103; LEFT: 304px; POSITION: absolute; TOP: 56px" runat="server" Width="430px">View the help file before adding any new codes to find out whether it will affect the functionality of other parts of the system.</asp:Label></DIV>
			<P>
				<asp:datagrid id="grdLookup" runat="server" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<HeaderStyle Width="25px"></HeaderStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="Code" SortExpression="Code" HeaderText="Code">
							<HeaderStyle Width="150px"></HeaderStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="Description" SortExpression="Description" HeaderText="Description">
							<HeaderStyle Width="250px"></HeaderStyle>
						</asp:BoundColumn>
						<asp:TemplateColumn SortExpression="IsActive" HeaderText="Is Active">
							<HeaderStyle Width="150px"></HeaderStyle>
							<ItemTemplate>
								<asp:CheckBox id="cbActiveDisplay" runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:CheckBox id="cbActiveEdit" runat="server"></asp:CheckBox>
							</EditItemTemplate>
						</asp:TemplateColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 28px" ms_positioning="GridLayout">
				<P>
					<uc1:DataGridPager id="Pager" runat="server"></uc1:DataGridPager></P>
			</DIV>
			<P>
				<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
		</form>
	</body>
</HTML>
