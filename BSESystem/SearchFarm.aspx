<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SearchFarm.aspx.vb" Inherits="BSESystem.SearchFarm" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CPHH" Src="CPHH.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Farm Search</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 248px" ms_positioning="GridLayout"><asp:label id="lblCPHH" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">CPH(H)</asp:label>
				<asp:label id="lblOwnerName" style="Z-INDEX: 102; LEFT: 400px; POSITION: absolute; TOP: 16px" runat="server" Width="152px" Height="8px">Owner Name</asp:label>
				<asp:label id="lblAddress" style="Z-INDEX: 103; LEFT: 400px; POSITION: absolute; TOP: 64px" runat="server" Width="152px">Address (partial search)</asp:label>
				<asp:label id="lblCounty" style="Z-INDEX: 104; LEFT: 400px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">County</asp:label>
				<asp:label id="lblHerdmark" style="Z-INDEX: 107; LEFT: 16px; POSITION: absolute; TOP: 64px" runat="server" Width="152px">Herdmark</asp:label>
				<asp:label id="lblNumericHerdmark" style="Z-INDEX: 109; LEFT: 16px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">Numeric Herdmark</asp:label>
				<asp:label id="lblDealer" style="Z-INDEX: 111; LEFT: 16px; POSITION: absolute; TOP: 160px" runat="server" Width="152px">Dealer</asp:label>
				<asp:label id="lblAHO" style="Z-INDEX: 112; LEFT: 400px; POSITION: absolute; TOP: 160px" runat="server" Width="152px">AHO</asp:label>
				<asp:label id="lblIncludeNonGBFarms" style="Z-INDEX: 118; LEFT: 16px; POSITION: absolute; TOP: 208px" runat="server" Width="152px">Include Non-GB Farms?</asp:label>
				<asp:label id="lblError" style="Z-INDEX: 120; LEFT: 616px; POSITION: absolute; TOP: 192px" runat="server" EnableViewState="False" Visible="False" CssClass="ValidatorText" ToolTip="Please provide one or more search criteria">*</asp:label>
				<HR style="Z-INDEX: 115; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 240px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<DIV style="Z-INDEX: 101; LEFT: 184px; WIDTH: 144px; POSITION: absolute; TOP: 16px; HEIGHT: 24px" ms_positioning="GridLayout">
					<uc1:cphh id="ctlCPHH" runat="server"></uc1:cphh>
				</DIV>
				<asp:textbox id="txtOwnerName" style="Z-INDEX: 105; LEFT: 568px; POSITION: absolute; TOP: 16px" runat="server" Width="168px" MaxLength="100"></asp:textbox>
				<asp:textbox id="txtHerdmark" style="Z-INDEX: 108; LEFT: 184px; POSITION: absolute; TOP: 64px" runat="server" Width="168px" MaxLength="8"></asp:textbox>
				<asp:textbox id="txtAddress" style="Z-INDEX: 106; LEFT: 568px; POSITION: absolute; TOP: 64px" runat="server" Width="168px" MaxLength="160"></asp:textbox>
				<asp:textbox id="txtNumericHerdmark" style="Z-INDEX: 110; LEFT: 184px; POSITION: absolute; TOP: 112px" runat="server" Width="168px" MaxLength="6"></asp:textbox>
				<asp:dropdownlist id="ddlCounty" style="Z-INDEX: 117; LEFT: 568px; POSITION: absolute; TOP: 112px" runat="server" Width="168px"></asp:dropdownlist>
				<asp:dropdownlist id="ddlDealer" style="Z-INDEX: 113; LEFT: 184px; POSITION: absolute; TOP: 160px" runat="server" Width="168px"></asp:dropdownlist>
				<asp:dropdownlist id="ddlAHO" style="Z-INDEX: 114; LEFT: 568px; POSITION: absolute; TOP: 160px" runat="server" Width="168px"></asp:dropdownlist>
				<asp:CheckBox id="chkIncludeNonGBFarms" style="Z-INDEX: 119; LEFT: 184px; POSITION: absolute; TOP: 208px" runat="server"></asp:CheckBox>
				<asp:Button id="btnSearch" style="Z-INDEX: 116; LEFT: 528px; POSITION: absolute; TOP: 208px" runat="server" Width="84px" Text="Search"></asp:Button>
				<asp:Button id="btnSearchMenu" style="Z-INDEX: 121; LEFT: 632px; POSITION: absolute; TOP: 208px" runat="server" Text="Search Menu"></asp:Button>
				<asp:label id="Label1" style="Z-INDEX: 122; LEFT: 400px; POSITION: absolute; TOP: 32px" runat="server" Width="152px" Height="8px">(partial search)</asp:label>
				<asp:label id="Label2" style="Z-INDEX: 123; LEFT: 400px; POSITION: absolute; TOP: 80px" runat="server" Width="152px" Height="8px">(partial search)</asp:label>
			</DIV>
			<P><asp:datagrid id="grdResults" runat="server" Width="740px" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="CPHH" SortExpression="CPHH" HeaderText="CPHH"></asp:BoundColumn>
						<asp:BoundColumn DataField="OwnerName" SortExpression="OwnerName" HeaderText="Owner Name"></asp:BoundColumn>
						<asp:BoundColumn DataField="Address" SortExpression="Address" HeaderText="Address"></asp:BoundColumn>
						<asp:BoundColumn DataField="County" SortExpression="County" HeaderText="County"></asp:BoundColumn>
						<asp:BoundColumn DataField="Herdmark" SortExpression="Herdmark" HeaderText="Herdmark(s)"></asp:BoundColumn>
						<asp:BoundColumn DataField="NumericHerdmark" SortExpression="NumericHerdmark" HeaderText="Numeric Herdmark(s)"></asp:BoundColumn>
						<asp:BoundColumn DataField="MapReference" SortExpression="MapReference" HeaderText="Map Ref"></asp:BoundColumn>
						<asp:BoundColumn DataField="AHO" SortExpression="AHO" HeaderText="AHO"></asp:BoundColumn>
						<asp:BoundColumn DataField="HerdType" SortExpression="HerdType" HeaderText="Herd Type"></asp:BoundColumn>
						<asp:BoundColumn DataField="CorrespondenceAddress" SortExpression="CorrespondenceAddress" HeaderText="Correspondence Address"></asp:BoundColumn>
						<asp:BoundColumn DataField="CasesCount" SortExpression="CasesCount" HeaderText="Cases"></asp:BoundColumn>
						<asp:BoundColumn DataField="ConfirmedCasesCount" SortExpression="ConfirmedCasesCount" HeaderText="Confirmed Cases"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 62px" ms_positioning="GridLayout">
				<P><uc1:datagridpager id="ResultsPager" runat="server"></uc1:datagridpager>
					<asp:HyperLink id="hlbExcel" style="Z-INDEX: 101; LEFT: 640px; POSITION: absolute; TOP: 40px" runat="server" Width="102px" Visible="False" NavigateUrl="ExcelExport.aspx" Target="_blank">Export to Excel</asp:HyperLink></P>
				<asp:label id="lblResultError" style="Z-INDEX: 102; LEFT: 0px; POSITION: absolute; TOP: 40px" CssClass="GridPagerErrorText" Visible="False" Runat="server"></asp:label>
			</DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</form>
	</body>
</HTML>
