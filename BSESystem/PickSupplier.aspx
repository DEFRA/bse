<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="PickSupplier.aspx.vb" Inherits="BSESystem.PickSupplier"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Pick A Supplier</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 56px" ms_positioning="GridLayout"><asp:label id="lblSupplier" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="718px" Font-Bold="True"> An exact match for XXXXX was not found.</asp:label>
				<HR style="Z-INDEX: 101; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 40px; HEIGHT: 1px" width="96.14%" SIZE="1">
			</DIV>
			<P><asp:datagrid id="grdSupplier" runat="server" Width="740px" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select"></asp:ButtonColumn>
						<asp:BoundColumn DataField="Name" SortExpression="Name" HeaderText="Name"></asp:BoundColumn>
						<asp:BoundColumn DataField="Details" SortExpression="Details" HeaderText="Details"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<P></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 28px" ms_positioning="GridLayout">
				<P><uc1:datagridpager id="SupplierPager" runat="server"></uc1:datagridpager></P>
			</DIV>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 84px" ms_positioning="GridLayout"><asp:button id="btnNew" style="Z-INDEX: 100; LEFT: 496px; POSITION: absolute; TOP: 48px" runat="server" Text="New"></asp:button>
				<asp:Button id="btnReturn" style="Z-INDEX: 101; LEFT: 680px; POSITION: absolute; TOP: 48px" runat="server" Text="Return"></asp:Button>
				<asp:Button id="btnUseSelected" style="Z-INDEX: 102; LEFT: 552px; POSITION: absolute; TOP: 48px" runat="server" Text="Use Selected" Enabled="False"></asp:Button>
				<asp:label id="lblName" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Name</asp:label>
				<asp:label id="lblDetails" style="Z-INDEX: 105; LEFT: 400px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Details</asp:label>
				<asp:textbox id="txtDetails" style="Z-INDEX: 106; LEFT: 568px; POSITION: absolute; TOP: 16px" tabIndex="1" runat="server" Width="168px" MaxLength="60"></asp:textbox>
				<asp:Label id="lblrfvDescription" style="Z-INDEX: 107; LEFT: 736px; POSITION: absolute; TOP: 16px" runat="server" CssClass="ValidatorText" ToolTip="You must enter a description" Visible="False">*</asp:Label>
				<asp:label id="lblSupplierName" style="Z-INDEX: 108; LEFT: 192px; POSITION: absolute; TOP: 16px" runat="server" Width="152px"></asp:label></DIV>
			<P>
				<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
		</form>
	</body>
</HTML>
