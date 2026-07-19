<%@ Register TagPrefix="uc1" TagName="BatchNumber" Src="BatchNumber.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="ADNSReference" Src="ADNSReference.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ADNSExportCI.aspx.vb" Inherits="BSESystem.ADNSExportCI" ValidateRequest="false" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : ADNS Export : Channel Island Cases</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 136px" ms_positioning="GridLayout">
				<asp:label id="lblEmailReference" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Email Reference</asp:label>
				<asp:label id="lblADNSReference" style="Z-INDEX: 101; LEFT: 400px; POSITION: absolute; TOP: 16px" runat="server" Width="152px" Height="16px">ADNS Reference</asp:label>
				<asp:label id="lblConfirmationDate" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 80px" runat="server" Width="152px">Confirmation Date</asp:label>
				<asp:label id="lblNumberOfCases" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 48px" runat="server" Width="152px">Number of cases in:</asp:label>
				<asp:label id="lblJersey" style="Z-INDEX: 106; LEFT: 184px; POSITION: absolute; TOP: 48px" runat="server" Width="66px">Jersey</asp:label>
				<asp:label id="lblGuernsey" style="Z-INDEX: 108; LEFT: 360px; POSITION: absolute; TOP: 48px" runat="server" Width="66px">Guernsey</asp:label>
				<asp:label id="lblIsleOfMan" style="Z-INDEX: 111; LEFT: 544px; POSITION: absolute; TOP: 48px" runat="server" Width="66px">Isle of Man</asp:label>
				<asp:requiredfieldvalidator id="rfvEmailReference" style="Z-INDEX: 115; LEFT: 352px; POSITION: absolute; TOP: 16px" runat="server" ControlToValidate="txtEmailReference" ErrorMessage="*" CssClass="ValidatorText" ToolTip="Please enter an email reference"></asp:requiredfieldvalidator>
				<asp:requiredfieldvalidator id="rfvJerseyCases" style="Z-INDEX: 116; LEFT: 312px; POSITION: absolute; TOP: 48px" runat="server" Height="16px" ControlToValidate="txtJerseyCases" ErrorMessage="*" CssClass="ValidatorText" ToolTip="Please enter the number of cases in Jersey"></asp:requiredfieldvalidator>
				<asp:rangevalidator id="rgvJerseyCases" style="Z-INDEX: 117; LEFT: 312px; POSITION: absolute; TOP: 48px" runat="server" ControlToValidate="txtJerseyCases" ErrorMessage="*" CssClass="ValidatorText" ToolTip="Please enter a number of cases between 0 and 10,000" Type="Integer" MinimumValue="0" MaximumValue="10000"></asp:rangevalidator>
				<asp:requiredfieldvalidator id="rfvGuernseyCases" style="Z-INDEX: 118; LEFT: 488px; POSITION: absolute; TOP: 48px" runat="server" Height="16px" ControlToValidate="txtGuernseyCases" ErrorMessage="*" CssClass="ValidatorText" ToolTip="Please enter the number of cases in Guernsey"></asp:requiredfieldvalidator>
				<asp:rangevalidator id="rgvGuernseyCases" style="Z-INDEX: 119; LEFT: 488px; POSITION: absolute; TOP: 48px" runat="server" ControlToValidate="txtGuernseyCases" ErrorMessage="*" CssClass="ValidatorText" ToolTip="Please enter a number of cases between 0 and 10,000" Type="Integer" MinimumValue="0" MaximumValue="10000"></asp:rangevalidator>
				<asp:requiredfieldvalidator id="rfvIsleOfManCases" style="Z-INDEX: 120; LEFT: 672px; POSITION: absolute; TOP: 48px" runat="server" Height="16px" ControlToValidate="txtIsleOfManCases" ErrorMessage="*" CssClass="ValidatorText" ToolTip="Please enter the number of cases in the Isle of Man"></asp:requiredfieldvalidator>
				<asp:rangevalidator id="rgvIsleOfManCases" style="Z-INDEX: 121; LEFT: 672px; POSITION: absolute; TOP: 48px" runat="server" ControlToValidate="txtIsleOfManCases" ErrorMessage="*" CssClass="ValidatorText" ToolTip="Please enter a number of cases between 0 and 10,000" Type="Integer" MinimumValue="0" MaximumValue="10000"></asp:rangevalidator>
				<HR style="Z-INDEX: 109; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 128px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:textbox id="txtEmailReference" style="Z-INDEX: 112; LEFT: 184px; POSITION: absolute; TOP: 16px" runat="server" Width="168px" MaxLength="50"></asp:textbox>
				<DIV style="Z-INDEX: 122; LEFT: 560px; WIDTH: 188px; POSITION: absolute; TOP: 16px; HEIGHT: 35px" ms_positioning="FlowLayout">
					<uc1:adnsreference id="ctlADNSReference" runat="server"></uc1:adnsreference>
				</DIV>
				<asp:textbox id="txtJerseyCases" style="Z-INDEX: 107; LEFT: 256px; POSITION: absolute; TOP: 48px" runat="server" Width="56px" MaxLength="5"></asp:textbox>
				<asp:textbox id="txtGuernseyCases" style="Z-INDEX: 104; LEFT: 432px; POSITION: absolute; TOP: 48px" runat="server" Width="56px" MaxLength="5"></asp:textbox>
				<asp:textbox id="txtIsleOfManCases" style="Z-INDEX: 113; LEFT: 616px; POSITION: absolute; TOP: 48px" runat="server" Width="56px" MaxLength="5"></asp:textbox>
				<DIV style="Z-INDEX: 114; LEFT: 184px; WIDTH: 161px; POSITION: absolute; TOP: 72px; HEIGHT: 48px" ms_positioning="FlowLayout">
					<uc1:calendardate id="ctlConfirmationDate" runat="server"></uc1:calendardate>
				</DIV>
				<asp:button id="btnGenerateReport" style="Z-INDEX: 110; LEFT: 472px; POSITION: absolute; TOP: 96px" runat="server" Width="110px" Text="Generate Report"></asp:button>
				<INPUT style="Z-INDEX: 123; LEFT: 592px; POSITION: absolute; TOP: 96px" onclick="location.href='ADNSExportMenu.aspx'" type="button" value="ADNS Export Menu">
			</DIV>
			<DIV id="litReportDiv" style="WIDTH: 750px; POSITION: relative; HEIGHT: 40px" runat="server" ms_positioning="GridLayout">
				<asp:label id="lblStartADNSReference" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="160px">Starting ADNS Reference:</asp:label><asp:label id="lblStartADNSReferenceValue" style="Z-INDEX: 103; LEFT: 192px; POSITION: absolute; TOP: 16px" runat="server" Width="160px"></asp:label><asp:label id="lblEndADNSReference" style="Z-INDEX: 105; LEFT: 400px; POSITION: absolute; TOP: 16px" runat="server" Width="160px">Ending ADNS Reference:</asp:label><asp:label id="lblEndADNSReferenceValue" style="Z-INDEX: 106; LEFT: 576px; POSITION: absolute; TOP: 16px" runat="server" Width="160px"></asp:label></DIV>
			<asp:datagrid id="grdADNSSummary" runat="server" Visible="False" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True">
				<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
				<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
				<ItemStyle CssClass="GridItem"></ItemStyle>
				<HeaderStyle CssClass="GridHeader"></HeaderStyle>
				<Columns>
					<asp:BoundColumn DataField="ADNSRegionID" SortExpression="ADNSRegionID" HeaderText="ADNS Number" DataFormatString="{0:D5}"></asp:BoundColumn>
					<asp:BoundColumn DataField="ADNSRegionName" SortExpression="ADNSRegionName" HeaderText="Region Name"></asp:BoundColumn>
					<asp:BoundColumn DataField="CasesCount" SortExpression="CasesCount" HeaderText="Reported Cases"></asp:BoundColumn>
				</Columns>
				<PagerStyle Visible="False"></PagerStyle>
			</asp:datagrid>
			<DIV id="litSummaryDiv" style="WIDTH: 750px; POSITION: relative; HEIGHT: 40px" runat="server" ms_positioning="GridLayout">
				<P><uc1:datagridpager id="SummaryPager" runat="server"></uc1:datagridpager></P>
			</DIV>
			<DIV id="litEmailDiv" style="WIDTH: 750px; POSITION: relative; HEIGHT: 282px" runat="server" ms_positioning="GridLayout">
				<P></P>
				<HR style="Z-INDEX: 100; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 256px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:button id="btnSendEmail" style="Z-INDEX: 101; LEFT: 624px; POSITION: absolute; TOP: 224px" runat="server" Width="110px" Text="Send Email"></asp:button>
				<asp:label id="lblFrom" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">From:</asp:label>
				<asp:textbox id="txtFromAddress" style="Z-INDEX: 103; LEFT: 184px; POSITION: absolute; TOP: 16px" runat="server" Width="408px" MaxLength="50" ReadOnly="True"></asp:textbox>
				<asp:label id="lblToAddress" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 48px" runat="server" Width="152px">To:</asp:label>
				<asp:textbox id="txtToAddress" style="Z-INDEX: 105; LEFT: 184px; POSITION: absolute; TOP: 48px" runat="server" Width="408px" MaxLength="50" ReadOnly="True"></asp:textbox>
				<asp:label id="Label1" style="Z-INDEX: 106; LEFT: 16px; POSITION: absolute; TOP: 80px" runat="server" Width="152px">Subject:</asp:label>
				<asp:textbox id="txtSubject" style="Z-INDEX: 107; LEFT: 184px; POSITION: absolute; TOP: 80px" runat="server" Width="408px" MaxLength="50" ReadOnly="True"></asp:textbox>
				<asp:label id="lblBody" style="Z-INDEX: 108; LEFT: 16px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">Message:</asp:label>
				<asp:textbox id="txtBody" style="Z-INDEX: 109; LEFT: 184px; POSITION: absolute; TOP: 112px" runat="server" Width="408px" Height="136px" MaxLength="50" ReadOnly="False" TextMode="MultiLine" EnableViewState="False"></asp:textbox></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
