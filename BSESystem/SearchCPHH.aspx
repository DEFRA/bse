<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SearchCPHH.aspx.vb" Inherits="BSESystem.SearchCPHH"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CPHH" Src="CPHH.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : List Of Cases For A Given Holding (for organic farmer checks)</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 133px" ms_positioning="GridLayout"><asp:label id="lblCPHH" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">CPH(H)</asp:label><asp:label id="lblHerdmark" style="Z-INDEX: 104; LEFT: 400px; POSITION: absolute; TOP: 16px" runat="server" Width="152px"> Herdmark</asp:label><asp:label id="lblNumericHerdmark" style="Z-INDEX: 106; LEFT: 16px; POSITION: absolute; TOP: 56px" runat="server" Width="152px" Height="40px">Numeric Herdmark</asp:label><asp:label id="lblIncludeNonGBCases" style="Z-INDEX: 108; LEFT: 400px; POSITION: absolute; TOP: 56px" runat="server" Width="152px">Include Non-GB Cases?</asp:label><asp:label id="lblError" style="Z-INDEX: 162; LEFT: 608px; POSITION: absolute; TOP: 88px" runat="server" ToolTip="Please provide one or more search criteria" EnableViewState="False" Visible="False" CssClass="ValidatorText">*</asp:label>
				<HR style="Z-INDEX: 107; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 112px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<DIV style="Z-INDEX: 101; LEFT: 184px; WIDTH: 176px; POSITION: absolute; TOP: 16px; HEIGHT: 24px" ms_positioning="GridLayout"><uc1:cphh id="ctlCPHH" runat="server"></uc1:cphh></DIV>
				<asp:textbox id="txtHerdmark" style="Z-INDEX: 103; LEFT: 568px; POSITION: absolute; TOP: 16px" runat="server" Width="168px" MaxLength="8"></asp:textbox><asp:textbox id="txtNumericHerdmark" style="Z-INDEX: 105; LEFT: 184px; POSITION: absolute; TOP: 56px" runat="server" Width="168px" MaxLength="6"></asp:textbox><asp:checkbox id="chkIncludeNonGBCases" style="Z-INDEX: 109; LEFT: 568px; POSITION: absolute; TOP: 56px" runat="server"></asp:checkbox><asp:button id="btnSearch" style="Z-INDEX: 110; LEFT: 520px; POSITION: absolute; TOP: 88px" runat="server" Width="84px" Text="Search"></asp:button><asp:button id="btnSearchMenu" style="Z-INDEX: 163; LEFT: 624px; POSITION: absolute; TOP: 88px" runat="server" Text="Search Menu"></asp:button></DIV>
			<P><asp:datagrid id="grdResults" runat="server" Width="740px" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="RBSE" SortExpression="RBSE" HeaderText="RBSE"></asp:BoundColumn>
						<asp:BoundColumn DataField="CPHH" SortExpression="CPHH" HeaderText="CPHH"></asp:BoundColumn>
						<asp:BoundColumn DataField="Sex" SortExpression="Sex" HeaderText="Sex"></asp:BoundColumn>
						<asp:BoundColumn DataField="Eartag" SortExpression="Eartag" HeaderText="Eartag"></asp:BoundColumn>
						<asp:BoundColumn DataField="BirthDate" SortExpression="BirthDate" HeaderText="Birth Date" DataFormatString="{0:d}"></asp:BoundColumn>
						<asp:BoundColumn DataField="Origin" SortExpression="Origin" HeaderText="Origin"></asp:BoundColumn>
						<asp:BoundColumn DataField="PurchaseDate" SortExpression="PurchaseDate" HeaderText="Date Purchased" DataFormatString="{0:d}"></asp:BoundColumn>
						<asp:BoundColumn DataField="PurchaseAgeInMonths" SortExpression="PurchaseAgeInMonths" HeaderText="Age at Purchase"></asp:BoundColumn>
						<asp:BoundColumn DataField="OnsetDate" SortExpression="OnsetDate" HeaderText="Date Onset" DataFormatString="{0:d}"></asp:BoundColumn>
						<asp:BoundColumn DataField="FormADate" SortExpression="FormADate" HeaderText="Form A Date" DataFormatString="{0:d}"></asp:BoundColumn>
						<asp:BoundColumn DataField="SlaughterDate" SortExpression="SlaughterDate" HeaderText="Slaughter Date" DataFormatString="{0:d}"></asp:BoundColumn>
						<asp:BoundColumn DataField="FinalResultDate" SortExpression="FinalResultDate" HeaderText="Final Result Date" DataFormatString="{0:d}"></asp:BoundColumn>
						<asp:BoundColumn DataField="OnsetAgeInMonths" SortExpression="OnsetAgeInMonths" HeaderText="Age at Onset"></asp:BoundColumn>
						<asp:BoundColumn DataField="Fate" SortExpression="Fate" HeaderText="Fate"></asp:BoundColumn>
						<asp:BoundColumn DataField="FinalResult" SortExpression="FinalResult" HeaderText="Final Result"></asp:BoundColumn>
						<asp:BoundColumn DataField="Survey" SortExpression="Survey" HeaderText="Survey"></asp:BoundColumn>
						<asp:BoundColumn DataField="CaseStatus" SortExpression="CaseStatus" HeaderText="Case Status"></asp:BoundColumn>
						<asp:BoundColumn DataField="TimeElapsed" SortExpression="DaysElapsed" HeaderText="Time Elapsed"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<DIV style="WIDTH: 751px; POSITION: relative; HEIGHT: 54px" ms_positioning="GridLayout"><uc1:datagridpager id="ResultsPager" runat="server"></uc1:datagridpager><asp:hyperlink id="hlbExcel" style="Z-INDEX: 101; LEFT: 648px; POSITION: absolute; TOP: 32px" runat="server" Width="102px" Visible="False" Target="_blank" NavigateUrl="ExcelExport.aspx">Export to Excel</asp:hyperlink><asp:label id="lblResultError" style="Z-INDEX: 102; LEFT: 0px; POSITION: absolute; TOP: 32px" Visible="False" CssClass="GridPagerErrorText" Runat="server"></asp:label></DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</form>
	</body>
</HTML>
