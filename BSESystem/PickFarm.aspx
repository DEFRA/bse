<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="PickFarm.aspx.vb" Inherits="BSESystem.PickFarm"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Pick A Farm</title>
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
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 96px" ms_positioning="GridLayout">
				<asp:label id="lblCPHH" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="364px" Font-Bold="True">CPHH: 123</asp:label>
				<asp:label id="lblError" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 48px" runat="server" Width="364px" Font-Bold="True">An exact match for this CPHH was not found</asp:label>
				<P>&nbsp;</P>
				<HR style="Z-INDEX: 102; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 80px; HEIGHT: 1px" width="96.14%" SIZE="1">
			</DIV>
			<P>
				<asp:datagrid id="grdPickFarm" runat="server" Width="568px" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select"></asp:ButtonColumn>
						<asp:BoundColumn DataField="CPHH" SortExpression="CPHH" HeaderText="CPHH"></asp:BoundColumn>
						<asp:BoundColumn DataField="OwnerName" SortExpression="OwnerName" HeaderText="Owner Name"></asp:BoundColumn>
						<asp:BoundColumn DataField="Address1" SortExpression="Address1" HeaderText="Address"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<P>
			</P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 28px" ms_positioning="GridLayout">
				<P>
					<uc1:DataGridPager id="PickFarmPager" runat="server"></uc1:DataGridPager></P>
			</DIV>
			<P>&nbsp;&nbsp;&nbsp;
				<asp:Button id="btnUseSelected" runat="server" Text="Use Selected" Enabled="False"></asp:Button>&nbsp; 
				&nbsp;
				<asp:Button id="btnNew" runat="server" Text=" New "></asp:Button>&nbsp; &nbsp;
				<asp:Button id="btnExit" runat="server" Text="  Exit  "></asp:Button>&nbsp;
			</P>
			<P>
				<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
		</form>
	</body>
</HTML>
