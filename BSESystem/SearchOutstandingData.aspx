<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SearchOutstandingData.aspx.vb" Inherits="BSESystem.SearchOutstandingData"%>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Outstanding Data Search</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P></P>
			<P>
				<uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 144px" ms_positioning="GridLayout">
				<P></P>
				<HR style="Z-INDEX: 100; LEFT: 16px; WIDTH: 96.13%; POSITION: absolute; TOP: 136px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:label id="lblFormADate" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 56px" runat="server" Width="152px">Form A Date Between</asp:label>
				<asp:label id="lblIncludeNonGBCases" style="Z-INDEX: 109; LEFT: 16px; POSITION: absolute; TOP: 104px" runat="server" Width="152px">Include Non-GB Cases?</asp:label>
				<asp:button id="btnSearch" style="Z-INDEX: 111; LEFT: 520px; POSITION: absolute; TOP: 104px" runat="server" Width="84px" Text="Search"></asp:button>
				<asp:label id="lblError" style="Z-INDEX: 162; LEFT: 352px; POSITION: absolute; TOP: 16px" runat="server" CssClass="ValidatorText" Visible="False" EnableViewState="False" ToolTip="Please select one of these three options">*</asp:label>
				<asp:Button id="btnSearchMenu" style="Z-INDEX: 163; LEFT: 624px; POSITION: absolute; TOP: 104px" runat="server" Text="Search Menu"></asp:Button>
				<asp:RadioButton id="optOutstandingResults" style="Z-INDEX: 106; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Text="Outstanding Results" GroupName="OutstandingData"></asp:RadioButton>
				<asp:RadioButton id="optFate" style="Z-INDEX: 107; LEFT: 184px; POSITION: absolute; TOP: 16px" runat="server" Text="Fate" GroupName="OutstandingData"></asp:RadioButton>
				<asp:RadioButton id="optBSE1s" style="Z-INDEX: 108; LEFT: 272px; POSITION: absolute; TOP: 16px" runat="server" Text="BSE1s" GroupName="OutstandingData"></asp:RadioButton>
				<DIV style="Z-INDEX: 103; LEFT: 184px; WIDTH: 161px; POSITION: absolute; TOP: 48px; HEIGHT: 48px" ms_positioning="FlowLayout">
					<uc1:CalendarDate id="ctlFormADateFrom" runat="server"></uc1:CalendarDate></DIV>
				<asp:label id="lblAnd1" style="Z-INDEX: 104; LEFT: 360px; POSITION: absolute; TOP: 56px" runat="server" Width="24px">and</asp:label>
				<DIV style="Z-INDEX: 105; LEFT: 392px; WIDTH: 161px; POSITION: absolute; TOP: 48px; HEIGHT: 48px" ms_positioning="FlowLayout">
					<uc1:CalendarDate id="ctlFormADateTo" runat="server"></uc1:CalendarDate></DIV>
				<asp:checkbox id="chkIncludeNonGBCases" style="Z-INDEX: 110; LEFT: 184px; POSITION: absolute; TOP: 104px" runat="server"></asp:checkbox>
			</DIV>
			<P>
				<asp:datagrid id="grdResults" runat="server" Width="740px" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="RBSE" SortExpression="RBSE" HeaderText="RBSE"></asp:BoundColumn>
						<asp:BoundColumn DataField="CPHH" SortExpression="CPHH" HeaderText="CPHH"></asp:BoundColumn>
						<asp:BoundColumn DataField="Eartag" SortExpression="Eartag" HeaderText="Eartag"></asp:BoundColumn>
						<asp:BoundColumn DataField="FormADate" SortExpression="FormADate" HeaderText="Form A Date" DataFormatString="{0:d}"></asp:BoundColumn>
						<asp:BoundColumn DataField="BirthDate" SortExpression="BirthDate" HeaderText="Birth Date" DataFormatString="{0:d}"></asp:BoundColumn>
						<asp:BoundColumn DataField="Fate" SortExpression="Fate" HeaderText="Fate"></asp:BoundColumn>
						<asp:BoundColumn DataField="FinalResult" SortExpression="FinalResult" HeaderText="Final Result"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<DIV style="WIDTH: 751px; POSITION: relative; HEIGHT: 50px" ms_positioning="GridLayout">
				<uc1:DataGridPager id="ResultsPager" runat="server"></uc1:DataGridPager>
				<asp:HyperLink id="hlbExcel" style="Z-INDEX: 101; LEFT: 648px; POSITION: absolute; TOP: 32px" runat="server" Width="102px" Visible="False" NavigateUrl="ExcelExport.aspx" Target="_blank">Export to Excel</asp:HyperLink>
				<asp:label id="lblResultError" style="Z-INDEX: 102; LEFT: 0px; POSITION: absolute; TOP: 32px" Visible="False" CssClass="GridPagerErrorText" Runat="server"></asp:label></DIV>
			<P>
				<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
		</form>
	</body>
</HTML>
