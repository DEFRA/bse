<%@ Register TagPrefix="uc1" TagName="BatchNumber" Src="BatchNumber.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="ADNSReference" Src="ADNSReference.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ADNSExportNI.aspx.vb" Inherits="BSESystem.ADNSExportNI" validateRequest="False" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : ADNS Export : Northern Ireland Cases</title>
		<META content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<META content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<META content="JavaScript" name="vs_defaultClientScript">
		<META content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<BODY>
		<FORM id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 176px" ms_positioning="GridLayout">
				<asp:label id="lblEmailReference" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Email Reference</asp:label>
				<asp:label id="lblADNSReference" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 72px" runat="server" Width="152px" Height="16px">ADNS Reference</asp:label>
				<asp:label id="lblConfirmationDate" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 128px" runat="server" Width="152px">Confirmation Date</asp:label>
				<asp:label id="lblRegion" style="Z-INDEX: 109; LEFT: 432px; POSITION: absolute; TOP: 80px" runat="server" Width="152px" Height="16px">Region</asp:label>
				<asp:label id="lblRegionError" style="Z-INDEX: 112; LEFT: 640px; POSITION: absolute; TOP: 80px" runat="server" CssClass="ValidatorText" ToolTip="Please enter a region code" Visible="False" EnableViewState="False">*</asp:label>
				<asp:label id="lblEmailError" style="Z-INDEX: 113; LEFT: 352px; POSITION: absolute; TOP: 16px" runat="server" CssClass="ValidatorText" ToolTip="Please enter an email reference" Visible="False" EnableViewState="False">*</asp:label>
				<HR style="Z-INDEX: 103; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 48px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:regularexpressionvalidator id="revRegion" style="Z-INDEX: 111; LEFT: 640px; POSITION: absolute; TOP: 80px" runat="server" ControlToValidate="txtRegion" ErrorMessage="*" CssClass="ValidatorText" ToolTip="Please enter a 5 digit region code" ValidationExpression="[0-9][0-9][0-9][0-9][0-9]"></asp:regularexpressionvalidator>
				<asp:textbox id="txtEmailReference" style="Z-INDEX: 106; LEFT: 184px; POSITION: absolute; TOP: 16px" runat="server" Width="168px" MaxLength="50"></asp:textbox>
				<INPUT style="Z-INDEX: 114; LEFT: 600px; POSITION: absolute; TOP: 16px" onclick="location.href='ADNSExportMenu.aspx'" type="button" value="ADNS Export Menu">
				<DIV style="Z-INDEX: 108; LEFT: 184px; WIDTH: 212px; POSITION: absolute; TOP: 72px; HEIGHT: 43px" ms_positioning="FlowLayout">
					<uc1:adnsreference id="ctlADNSReference" runat="server"></uc1:adnsreference>
				</DIV>
				<asp:textbox id="txtRegion" style="Z-INDEX: 110; LEFT: 576px; POSITION: absolute; TOP: 80px" runat="server" Width="60px" MaxLength="5"></asp:textbox>
				<DIV style="Z-INDEX: 107; LEFT: 184px; WIDTH: 161px; POSITION: absolute; TOP: 120px; HEIGHT: 48px" ms_positioning="FlowLayout">
					<uc1:calendardate id="ctlConfirmationDate" runat="server"></uc1:calendardate>
				</DIV>
				<asp:button id="btnAddToGrid" style="Z-INDEX: 104; LEFT: 624px; POSITION: absolute; TOP: 144px" runat="server" Width="110px" Text="Add To Grid"></asp:button>
                <asp:Label ID="regionHelpLabel" style="Z-INDEX: 100; LEFT: 460px; POSITION: absolute; TOP: 105px" runat="server" CssClass="smalltext" Text="Enter a five digit number, with two zeroes at the end and one at the front eg. 073 = 07300"></asp:Label></DIV>
			<P>
				<asp:datagrid id="grdADNS" runat="server" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<HeaderStyle Width="21px"></HeaderStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="ADNSReference" SortExpression="ADNSReference" HeaderText="ADNS Reference"></asp:BoundColumn>
						<asp:BoundColumn DataField="ADNSRegionID" SortExpression="ADNSRegionID" HeaderText="Region" DataFormatString="{0:D5}"></asp:BoundColumn>
						<asp:BoundColumn DataField="ConfirmationDate" SortExpression="ConfirmationDate" HeaderText="Confirmation Date" DataFormatString="{0:d}">
							<HeaderStyle Width="175px"></HeaderStyle>
						</asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 88px" ms_positioning="GridLayout">
				<uc1:datagridpager id="ADNSPager" runat="server"></uc1:datagridpager>
				<P></P>
				<HR style="Z-INDEX: 103; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 40px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:button id="btnGenerateReport" style="Z-INDEX: 109; LEFT: 624px; POSITION: absolute; TOP: 56px" runat="server" Width="110px" Text="Generate Report"></asp:button>
				<asp:label id="lblNoDataError" style="Z-INDEX: 112; LEFT: 736px; POSITION: absolute; TOP: 56px" runat="server" CssClass="ValidatorText" ToolTip="Please add at least one case to the table before generating a report" Visible="False" EnableViewState="False">*</asp:label></DIV>
			<DIV id="litReportDiv" style="WIDTH: 750px; POSITION: relative; HEIGHT: 40px" runat="server" ms_positioning="GridLayout">
				<P></P>
				<asp:label id="lblStartADNSReference" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="160px">Starting ADNS Reference:</asp:label>
				<asp:label id="lblStartADNSReferenceValue" style="Z-INDEX: 103; LEFT: 192px; POSITION: absolute; TOP: 16px" runat="server" Width="160px"></asp:label>
				<asp:label id="lblEndADNSReference" style="Z-INDEX: 105; LEFT: 400px; POSITION: absolute; TOP: 16px" runat="server" Width="160px">Ending ADNS Reference:</asp:label>
				<asp:label id="lblEndADNSReferenceValue" style="Z-INDEX: 106; LEFT: 576px; POSITION: absolute; TOP: 16px" runat="server" Width="160px"></asp:label></DIV>
			<P>
				<asp:datagrid id="grdADNSSummary" runat="server" Visible="False" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
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
				</asp:datagrid></P>
			<DIV id="litSummaryDiv" style="WIDTH: 750px; POSITION: relative; HEIGHT: 40px" runat="server" ms_positioning="GridLayout">
				<P>
					<uc1:datagridpager id="SummaryPager" runat="server"></uc1:datagridpager></P>
			</DIV>
			<P></P>
			<DIV id="litEmailDiv" style="WIDTH: 750px; POSITION: relative; HEIGHT: 282px" runat="server" ms_positioning="GridLayout">
				<P></P>
				<HR style="Z-INDEX: 100; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 256px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:label id="lblFrom" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">From:</asp:label>
				<asp:textbox id="txtFromAddress" style="Z-INDEX: 103; LEFT: 184px; POSITION: absolute; TOP: 16px" tabIndex="1" runat="server" Width="408px" MaxLength="50" ReadOnly="True"></asp:textbox>
				<asp:label id="lblToAddress" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 48px" runat="server" Width="152px">To:</asp:label>
				<asp:textbox id="txtToAddress" style="Z-INDEX: 105; LEFT: 184px; POSITION: absolute; TOP: 48px" tabIndex="1" runat="server" Width="408px" MaxLength="50" ReadOnly="True"></asp:textbox>
				<asp:label id="Label1" style="Z-INDEX: 106; LEFT: 16px; POSITION: absolute; TOP: 80px" runat="server" Width="152px">Subject:</asp:label>
				<asp:textbox id="txtSubject" style="Z-INDEX: 107; LEFT: 184px; POSITION: absolute; TOP: 80px" tabIndex="1" runat="server" Width="408px" MaxLength="50" ReadOnly="True"></asp:textbox>
				<asp:label id="lblBody" style="Z-INDEX: 108; LEFT: 16px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">Message:</asp:label>
				<asp:textbox id="txtBody" style="Z-INDEX: 109; LEFT: 184px; POSITION: absolute; TOP: 112px" tabIndex="1" runat="server" Width="408px" Height="136px" MaxLength="50" ReadOnly="False" TextMode="MultiLine"></asp:textbox>
				<asp:button id="btnSendEmail" style="Z-INDEX: 101; LEFT: 624px; POSITION: absolute; TOP: 224px" runat="server" Width="110px" Text="Send Email"></asp:button>
			</DIV>
			<P>
				<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</FORM>
	</BODY>
</HTML>
