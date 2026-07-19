<%@ Register TagPrefix="uc1" TagName="BatchNumber" Src="BatchNumber.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="OSSExportBSE1b.aspx.vb" Inherits="BSESystem.OSSExportBSE1b"%>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : OSS Export : BSE1b</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 112px" ms_positioning="GridLayout">
				<asp:label id="lblBatchNumber" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="96px">Batch Number</asp:label><asp:label id="lblRBSE" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 48px" runat="server" Width="96px">RBSE</asp:label>
				<DIV style="Z-INDEX: 101; LEFT: 128px; WIDTH: 232px; POSITION: absolute; TOP: 48px; HEIGHT: 27px" ms_positioning="GridLayout"><uc1:rbse id="ctlRBSE" runat="server"></uc1:rbse></DIV>
				<asp:button id="btnAddToGrid" style="Z-INDEX: 104; LEFT: 32px; POSITION: absolute; TOP: 80px" runat="server" Text="Add to Grid"></asp:button>
				<asp:label id="lblBatchNumberValue" style="Z-INDEX: 105; LEFT: 128px; POSITION: absolute; TOP: 16px" runat="server" Width="176px"></asp:label>
				<asp:label id="lblBatchID" style="Z-INDEX: 106; LEFT: 352px; POSITION: absolute; TOP: 16px" runat="server" Visible="False"></asp:label>
				<asp:Label id="lblTruncatedError" style="Z-INDEX: 107; LEFT: 368px; POSITION: absolute; TOP: 56px" runat="server" Visible="False" ForeColor="Red">The Batch Number generated was greater than 89/999 so will be truncated.</asp:Label></DIV>
			<P><asp:datagrid id="grdBSE1b" runat="server" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<HeaderStyle Width="21px"></HeaderStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="RBSE" SortExpression="RBSE" HeaderText="RBSE">
							<HeaderStyle Width="150px"></HeaderStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="CPHH" SortExpression="CPHH" HeaderText="CPHH">
							<HeaderStyle Width="150px"></HeaderStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="OwnerName" SortExpression="OwnerName" HeaderText="Owner Name">
							<HeaderStyle Width="175px"></HeaderStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="Address1" SortExpression="Address1" HeaderText="Address">
							<HeaderStyle Width="175px"></HeaderStyle>
						</asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 21px" ms_positioning="GridLayout"><uc1:datagridpager id="BSE1bPager" runat="server"></uc1:datagridpager></DIV>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 44px" ms_positioning="GridLayout"><asp:button id="btnExportToOSS" style="Z-INDEX: 101; LEFT: 528px; POSITION: absolute; TOP: 16px" runat="server" Text="Export to OSS" Enabled="False"></asp:button><asp:button id="btnHome" style="Z-INDEX: 102; LEFT: 648px; POSITION: absolute; TOP: 16px" runat="server" Text="OSS Menu"></asp:button></DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</form>
	</body>
</HTML>
