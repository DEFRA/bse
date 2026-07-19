<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="UserMaintenance.aspx.vb" Inherits="BSESystem.UserMaintenance"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>User Maintenance</title>
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
			<P>
				<asp:datagrid id="grdUser" runat="server" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<HeaderStyle Width="25px"></HeaderStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="NTLogin" SortExpression="NTLogin" HeaderText="NT Login">
							<HeaderStyle Width="125px"></HeaderStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="Name" SortExpression="Name" HeaderText="Name">
							<HeaderStyle Width="150px"></HeaderStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="Email" SortExpression="Email" HeaderText="Email">
							<HeaderStyle Width="175px"></HeaderStyle>
						</asp:BoundColumn>
						<asp:TemplateColumn SortExpression="UserGroup" HeaderText="User Group">
							<HeaderStyle Width="150px"></HeaderStyle>
							<ItemTemplate>
								<asp:Label id="lblUserGroupDisplay" runat="server"></asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:DropDownList id=ddlUserGroupEdit runat="server" DataSource="<%# GetUserGroups() %>" DataValueField="ID" DataTextField="Name">
								</asp:DropDownList>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="IsActive" HeaderText="IsActive">
							<HeaderStyle Width="25px"></HeaderStyle>
							<ItemTemplate>
								<asp:CheckBox id="chkIsActiveDisplay" runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:CheckBox id="chkIsActiveEdit" runat="server"></asp:CheckBox>
							</EditItemTemplate>
						</asp:TemplateColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 28px" ms_positioning="GridLayout">
				<P>
					<uc1:DataGridPager id="UserPager" runat="server"></uc1:DataGridPager></P>
			</DIV>
			<P>
				<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
		</form>
	</body>
</HTML>
