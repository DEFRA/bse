<%@ Page Language="vb" AutoEventWireup="false" Codebehind="PickSireDam.aspx.vb" Inherits="BSESystem.PickSireDam" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Pick A
			<% If Request.Querystring("sex") = "F" Then Response.Write ("Dam") Else Response.Write ("Sire") %>
		</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 80px" ms_positioning="GridLayout"><asp:label id="lblError" style="Z-INDEX: 100; LEFT: 384px; POSITION: absolute; TOP: 48px" runat="server" Font-Bold="True" Width="364px">An exact match for this animal was not found</asp:label>
				<P>&nbsp;</P>
				<HR style="Z-INDEX: 101; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 72px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:label id="lblEartag" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Font-Bold="True" Width="216px">Eartag: XXX</asp:label><asp:label id="lblName" style="Z-INDEX: 104; LEFT: 384px; POSITION: absolute; TOP: 16px" runat="server" Font-Bold="True" Width="216px">Name: XXX</asp:label>
				<asp:label id="lblHerdbook" style="Z-INDEX: 105; LEFT: 16px; POSITION: absolute; TOP: 48px" runat="server" Width="216px" Font-Bold="True">Herdbook: XXX</asp:label></DIV>
			<P><asp:datagrid id="grdPickSireDam" runat="server" Width="568px" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select"></asp:ButtonColumn>
						<asp:BoundColumn DataField="Eartag" SortExpression="Eartag" HeaderText="Eartag"></asp:BoundColumn>
						<asp:BoundColumn DataField="Name" SortExpression="Name" HeaderText="Name"></asp:BoundColumn>
						<asp:BoundColumn DataField="Herdbook" SortExpression="Herdbook" HeaderText="Herdbook"></asp:BoundColumn>
						<asp:TemplateColumn HeaderText="RBSE">
							<ItemTemplate>
								<asp:Label runat="server" Text='<%# FormatRBSE(DataBinder.Eval(Container, "DataItem.RBSE").ToString()) %>'>
								</asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:TextBox runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.RBSE") %>'>
								</asp:TextBox>
							</EditItemTemplate>
						</asp:TemplateColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<P></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 28px" ms_positioning="GridLayout">
				<P><uc1:datagridpager id="PickSireDamPager" runat="server"></uc1:datagridpager></P>
			</DIV>
			<P>&nbsp;&nbsp;&nbsp;
				<asp:button id="btnUseSelected" runat="server" Enabled="False" Text="Use Selected"></asp:button>&nbsp; 
				&nbsp;
				<asp:button id="btnNew" runat="server" Text=" New "></asp:button>&nbsp; &nbsp;
				<asp:button id="btnExit" runat="server" Text="  Exit  "></asp:button>&nbsp;
			</P>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</form>
	</body>
</HTML>
