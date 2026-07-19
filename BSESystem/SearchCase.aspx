<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SearchCase.aspx.vb" Inherits="BSESystem.SearchCase"%>
<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Case Search</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="WIDTH: 744px; POSITION: relative; HEIGHT: 440px; left: 0px; top: 0px;" ms_positioning="GridLayout">
			<asp:label id="lblRBSE" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">RBSE</asp:label>
			<asp:label id="lblDBSE" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 48px" runat="server" Width="152px">DBSE</asp:label>
			<asp:label id="lblEartag" style="Z-INDEX: 105; LEFT: 400px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Eartag (starting with...)</asp:label>
			<asp:label id="lblFate" style="Z-INDEX: 106; LEFT: 400px; POSITION: absolute; TOP: 48px" runat="server" Width="152px">Fate</asp:label>
			<asp:label id="lblFinalResult" style="Z-INDEX: 108; LEFT: 16px; POSITION: absolute; TOP: 80px" runat="server" Width="152px">Final Result</asp:label>
			<asp:label id="lblSex" style="Z-INDEX: 110; LEFT: 400px; POSITION: absolute; TOP: 80px" runat="server" Width="152px">Sex</asp:label>
                <asp:Label ID="Label1" runat="server" Font-Italic="True" Style="z-index: 110; left: 18px;
                    position: absolute; top: 386px" Width="470px">Note: % character can be used as wildcard character in text fields</asp:Label>
			<asp:label id="lblSurvey" style="Z-INDEX: 111; LEFT: 16px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">Survey</asp:label>
			<asp:label id="lblFormADate" style="Z-INDEX: 116; LEFT: 16px; POSITION: absolute; TOP: 176px" runat="server" Width="152px"> Form A Date Between</asp:label>
			<asp:label id="lblAnd1" style="Z-INDEX: 117; LEFT: 360px; POSITION: absolute; TOP: 184px" runat="server" Width="24px">and</asp:label>
			<asp:label id="lblAnd2" style="Z-INDEX: 118; LEFT: 360px; POSITION: absolute; TOP: 216px" runat="server" Width="24px">and</asp:label>
			<asp:label id="lblAnd3" style="Z-INDEX: 119; LEFT: 360px; POSITION: absolute; TOP: 248px" runat="server" Width="24px">and</asp:label>
			<asp:label id="lblRangeOfResultDates" style="Z-INDEX: 120; LEFT: 16px; POSITION: absolute; TOP: 208px" runat="server" Width="160px">Final Result Date Between</asp:label>
			<asp:label id="lblRangeOfBirthDates" style="Z-INDEX: 121; LEFT: 16px; POSITION: absolute; TOP: 240px" runat="server" Width="152px"> Birth Date Between</asp:label>
			<asp:label id="lblIncludeNonGBCases" style="Z-INDEX: 124; LEFT: 16px; POSITION: absolute; TOP: 144px" runat="server" Width="152px">Include Non-GB Cases?</asp:label>
                <asp:Label ID="lblIsImportedCase" runat="server" Style="z-index: 124; left: 400px; position: absolute;
                    top: 146px" Width="152px">Imported Cases</asp:Label>
                <asp:label id="lblNotes" style="Z-INDEX: 126; LEFT: 16px; POSITION: absolute; TOP: 288px" runat="server" Width="152px"> Notes (partial search)</asp:label>
                <asp:label id="lblError" style="Z-INDEX: 128; LEFT: 608px; POSITION: absolute; TOP: 384px" runat="server" EnableViewState="False" ToolTip="Please provide one or more search criteria" Visible="False" CssClass="ValidatorText">*</asp:label>
				<HR style="Z-INDEX: 122; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 416px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:regularexpressionvalidator id="revDBSE" style="Z-INDEX: 127; LEFT: 352px; POSITION: absolute; TOP: 48px" runat="server" ToolTip="YY[/]NNNNN" CssClass="ValidatorText" ValidationExpression="\d{2}(/)?\d{5}" ControlToValidate="txtDBSE" ErrorMessage="*"></asp:regularexpressionvalidator>
				<DIV style="Z-INDEX: 103; LEFT: 184px; WIDTH: 168px; POSITION: absolute; TOP: 16px; HEIGHT: 19px" ms_positioning="GridLayout"><uc1:rbse id="ctlRBSE" runat="server"></uc1:rbse></DIV>
				<asp:textbox id="txtEartag" style="Z-INDEX: 104; LEFT: 568px; POSITION: absolute; TOP: 16px" runat="server" Width="168px" MaxLength="35"></asp:textbox>
				<asp:textbox id="txtDBSE" style="Z-INDEX: 102; LEFT: 184px; POSITION: absolute; TOP: 48px" runat="server" Width="168px" ToolTip="DBSE Format: YY[/]NNNNN" MaxLength="8"></asp:textbox>
				<asp:dropdownlist id="ddlFate" style="Z-INDEX: 107; LEFT: 568px; POSITION: absolute; TOP: 48px" runat="server" Width="168px"></asp:dropdownlist>
				<asp:dropdownlist id="ddlFinalResult" style="Z-INDEX: 109; LEFT: 184px; POSITION: absolute; TOP: 80px" runat="server" Width="168px"></asp:dropdownlist>
				<asp:dropdownlist id="ddlSex" style="Z-INDEX: 112; LEFT: 568px; POSITION: absolute; TOP: 80px" runat="server" Width="168px"></asp:dropdownlist>
				<asp:dropdownlist id="ddlSurvey" style="Z-INDEX: 114; LEFT: 184px; POSITION: absolute; TOP: 112px" runat="server" Width="168px"></asp:dropdownlist>
				<asp:checkbox id="chkIncludeNonGBCases" style="Z-INDEX: 125; LEFT: 184px; POSITION: absolute; TOP: 144px" runat="server"></asp:checkbox>
				<asp:checkbox id="chkIsImportedCase" style="Z-INDEX: 125; LEFT: 568px; POSITION: absolute; TOP: 144px" runat="server"></asp:checkbox>
                <DIV style="Z-INDEX: 134; LEFT: 184px; WIDTH: 216px; POSITION: absolute; TOP: 176px; HEIGHT: 32px" ms_positioning="FlowLayout"><uc1:calendardate id="ctlFormADateFrom" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 135; LEFT: 424px; WIDTH: 256px; POSITION: absolute; TOP: 176px; HEIGHT: 24px" ms_positioning="FlowLayout"><uc1:calendardate id="ctlFormADateTo" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 131; LEFT: 184px; WIDTH: 238px; POSITION: absolute; TOP: 216px; HEIGHT: 27px" ms_positioning="FlowLayout"><uc1:calendardate id="ctlFinalResultDateFrom" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 132; LEFT: 424px; WIDTH: 256px; POSITION: absolute; TOP: 216px; HEIGHT: 24px" ms_positioning="FlowLayout"><uc1:calendardate id="ctlFinalResultDateTo" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 130; LEFT: 184px; WIDTH: 208px; POSITION: absolute; TOP: 248px; HEIGHT: 24px" ms_positioning="FlowLayout"><uc1:calendardate id="ctlBirthDateFrom" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 129; LEFT: 424px; WIDTH: 256px; POSITION: absolute; TOP: 248px; HEIGHT: 24px" ms_positioning="FlowLayout"><uc1:calendardate id="ctlBirthDateTo" runat="server"></uc1:calendardate></DIV>
				<asp:textbox id="txtNotes" style="Z-INDEX: 115; LEFT: 184px; POSITION: absolute; TOP: 288px" runat="server" Width="544px" MaxLength="500" TextMode="MultiLine" Height="88px"></asp:textbox>
				<asp:button id="btnSearch" style="Z-INDEX: 123; LEFT: 520px; POSITION: absolute; TOP: 384px" runat="server" Width="84px" Text="Search"></asp:button>
				<asp:button id="btnSearchMenu" style="Z-INDEX: 133; LEFT: 624px; POSITION: absolute; TOP: 384px" runat="server" Text="Search Menu"></asp:button>
				<asp:label id="lblPassiveActive" style="Z-INDEX: 136; LEFT: 400px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">Passive/Active</asp:label>
				<asp:dropdownlist id="ddlPassiveActive" style="Z-INDEX: 137; LEFT: 568px; POSITION: absolute; TOP: 112px" runat="server" Width="168px"></asp:dropdownlist></DIV>
			<P><asp:datagrid id="grdResults" runat="server" Width="740px" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="RBSE" SortExpression="RBSE" HeaderText="RBSE"></asp:BoundColumn>
						<asp:BoundColumn DataField="CPHH" SortExpression="CPHH" HeaderText="CPHH"></asp:BoundColumn>
						<asp:BoundColumn DataField="Sex" SortExpression="Sex" HeaderText="Sex"></asp:BoundColumn>
						<asp:BoundColumn DataField="Survey" SortExpression="Survey" HeaderText="Survey"></asp:BoundColumn>
						<asp:BoundColumn DataField="Eartag" SortExpression="Eartag" HeaderText="Eartag"></asp:BoundColumn>
						<asp:BoundColumn DataField="BirthDate" SortExpression="BirthDate" HeaderText="Birth Date" DataFormatString="{0:d}"></asp:BoundColumn>
						<asp:BoundColumn DataField="IsBirthDateEst" SortExpression="IsBirthDateEst" HeaderText="Birth Date Est?"></asp:BoundColumn>
						<asp:BoundColumn DataField="Origin" SortExpression="Origin" HeaderText="Origin"></asp:BoundColumn>
						<asp:BoundColumn DataField="FormADate" SortExpression="FormADate" HeaderText="Form A Date" DataFormatString="{0:d}"></asp:BoundColumn>
						<asp:BoundColumn DataField="Fate" SortExpression="Fate" HeaderText="Fate"></asp:BoundColumn>
						<asp:BoundColumn DataField="FinalResult" SortExpression="FinalResult" HeaderText="Final Result"></asp:BoundColumn>
						<asp:BoundColumn DataField="FinalResultDate" SortExpression="FinalResultDate" HeaderText="Final Result Date" DataFormatString="{0:d}"></asp:BoundColumn>
						<asp:BoundColumn DataField="DBSE" SortExpression="DBSE" HeaderText="DBSE"></asp:BoundColumn>
						<asp:BoundColumn DataField="ValuationAge" SortExpression="ValuationAge" HeaderText="Valuation Age"></asp:BoundColumn>
						<asp:BoundColumn DataField="Notes" SortExpression="Notes" HeaderText="Notes"></asp:BoundColumn>
						<asp:BoundColumn DataField="BabNotes" SortExpression="BabNotes" HeaderText="Bab Notes"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<DIV style="WIDTH: 751px; POSITION: relative; HEIGHT: 54px" ms_positioning="GridLayout"><uc1:datagridpager id="ResultsPager" runat="server"></uc1:datagridpager><asp:hyperlink id="hlbExcel" style="Z-INDEX: 101; LEFT: 648px; POSITION: absolute; TOP: 32px" runat="server" Width="102px" Visible="False" Target="_blank" NavigateUrl="ExcelExport.aspx">Export to Excel</asp:hyperlink>
				<asp:label id="lblResultError" style="Z-INDEX: 102; LEFT: 0px; POSITION: absolute; TOP: 32px" Visible="False" CssClass="GridPagerErrorText" Runat="server"></asp:label></DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</form>
	</body>
</HTML>
