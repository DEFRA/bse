<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CPHH" Src="CPHH.ascx" %>
<%@ Register TagPrefix="uc1" TagName="ExitConfirmation" Src="ExitConfirmation.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="NonGBCaseCreation.aspx.vb" Inherits="BSESystem.NonGBCaseCreation" smartNavigation="true"%>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="Eartag" Src="Eartag.ascx" %>
<%@ Register TagPrefix="uc1" TagName="ThreePartEartag" Src="ThreePartEartag.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Non-GB Case Creation</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<asp:label id="lblRBSEHeader" runat="server" Width="728px" Font-Bold="True">RBSE Number:  12/12/12345</asp:label>
			<DIV style="WIDTH: 753px; POSITION: relative; HEIGHT: 330px; left: 0px; top: 0px;" ms_positioning="GridLayout">
				<DIV id="exitConfirmationDIV" style="Z-INDEX: 167; LEFT: 296px; WIDTH: 316px; POSITION: absolute; TOP: 8px; HEIGHT: 8px" ms_positioning="GridLayout">
					<uc1:ExitConfirmation id="ctlExitConfirmation" runat="server"></uc1:ExitConfirmation></DIV>
				<asp:label id="lblEartag" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 32px" runat="server" Width="152px">Eartag</asp:label>
				<asp:label id="lblFate" style="Z-INDEX: 102; LEFT: 400px; POSITION: absolute; TOP: 32px" runat="server" Width="152px">Fate</asp:label>
				<asp:label id="lblFinalResultDate" style="Z-INDEX: 103; LEFT: 400px; POSITION: absolute; TOP: 64px" runat="server" Width="152px">Final Result Date</asp:label>
				<asp:label id="lblFinalResult" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 64px" runat="server" Width="152px">Final Result</asp:label>
				<asp:label id="lblSlaughterDate" style="Z-INDEX: 105; LEFT: 16px; POSITION: absolute; TOP: 96px" runat="server" Width="152px">Slaughter Date</asp:label>
				<asp:label id="lblCPHH" style="Z-INDEX: 109; LEFT: 16px; POSITION: absolute; TOP: 144px" runat="server" Width="152px">CPHH</asp:label>
				<asp:label id="lblOwnerName" style="Z-INDEX: 110; LEFT: 16px; POSITION: absolute; TOP: 176px" runat="server" Width="152px">Owner Name</asp:label>
				<asp:label id="lblPostcode" style="Z-INDEX: 116; LEFT: 16px; POSITION: absolute; TOP: 304px" runat="server" Width="152px">Postcode</asp:label>
				<asp:label id="lblAddress" style="Z-INDEX: 117; LEFT: 16px; POSITION: absolute; TOP: 208px" runat="server" Width="152px">Address</asp:label>
				<asp:label id="lblCounty" style="Z-INDEX: 118; LEFT: 400px; POSITION: absolute; TOP: 176px" runat="server" Width="154px">County</asp:label>
				<asp:label id="lblHerdmark" style="Z-INDEX: 120; LEFT: 400px; POSITION: absolute; TOP: 208px" runat="server" Width="152px">Herdmark</asp:label>
				<asp:label id="lblNumericHerdmark" style="Z-INDEX: 122; LEFT: 400px; POSITION: absolute; TOP: 240px" runat="server" Width="153px">Numeric Herdmark</asp:label>
				<asp:label id="lblCPHHError" style="Z-INDEX: 126; LEFT: 352px; POSITION: absolute; TOP: 144px" runat="server" ToolTip="Please enter the CPHH of a non-GB farm" Visible="False" CssClass="validatorText" ForeColor="Red">*</asp:label>
				<asp:label id="lblCPHHEmpty" style="Z-INDEX: 135; LEFT: 352px; POSITION: absolute; TOP: 144px" runat="server" ToolTip="You need to enter a CPHH" Visible="False" CssClass="validatorText" ForeColor="Red">*</asp:label>
				<asp:label id="lblEartagEmpty" style="Z-INDEX: 136; LEFT: 370px; POSITION: absolute; TOP: 31px" runat="server" ToolTip="You need to enter an Eartag" Visible="False" CssClass="validatorText" ForeColor="Red">*</asp:label>
				<asp:label id="lblSlaughterDateEmpty" style="Z-INDEX: 137; LEFT: 352px; POSITION: absolute; TOP: 88px" runat="server" ToolTip="You need to enter a Slaughter Date" Visible="False" CssClass="validatorText" ForeColor="Red">*</asp:label>
				<asp:label id="lblFinalResultDateEmpty" style="Z-INDEX: 138; LEFT: 736px; POSITION: absolute; TOP: 64px" runat="server" ToolTip="You need to enter a Final Result Date" Visible="False" CssClass="validatorText" ForeColor="Red">*</asp:label>
				<asp:label id="lblCountyEmpty" style="Z-INDEX: 139; LEFT: 736px; POSITION: absolute; TOP: 176px" runat="server" ToolTip="You must select a County" Visible="False" CssClass="validatorText" ForeColor="Red">*</asp:label>
				<HR style="Z-INDEX: 107; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 128px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:regularexpressionvalidator id="revNumericHerdmark" style="Z-INDEX: 127; LEFT: 736px; POSITION: absolute; TOP: 240px" runat="server" ToolTip="[dddddd]" CssClass="ValidatorText" ControlToValidate="txtNumericHerdmark" ValidationExpression="[0-9][0-9][0-9][0-9][0-9][0-9]" ErrorMessage="*"></asp:regularexpressionvalidator>
				<asp:requiredfieldvalidator id="rfvOwnerName" style="Z-INDEX: 128; LEFT: 352px; POSITION: absolute; TOP: 176px" runat="server" ToolTip="You must enter an Owner Name" CssClass="ValidatorText" ControlToValidate="txtOwnerName" ErrorMessage="*"></asp:requiredfieldvalidator>
				<asp:requiredfieldvalidator id="rfvAddress1" style="Z-INDEX: 129; LEFT: 352px; POSITION: absolute; TOP: 208px" runat="server" ToolTip="You must enter an Address" CssClass="ValidatorText" ControlToValidate="txtAddress1" ErrorMessage="*"></asp:requiredfieldvalidator>
				<asp:requiredfieldvalidator id="rfvFinalResult" style="Z-INDEX: 131; LEFT: 352px; POSITION: absolute; TOP: 64px" runat="server" ToolTip="You must choose a Final Result" CssClass="ValidatorText" ControlToValidate="ddlFinalResult" ErrorMessage="*"></asp:requiredfieldvalidator>
				<asp:requiredfieldvalidator id="rfvFate" style="Z-INDEX: 132; LEFT: 736px; POSITION: absolute; TOP: 32px" runat="server" ToolTip="You must choose a Fate" CssClass="ValidatorText" ControlToValidate="ddlFate" ErrorMessage="*"></asp:requiredfieldvalidator>
				<DIV style="Z-INDEX: 125; LEFT: 184px; WIDTH: 200px; POSITION: absolute; TOP: 32px; HEIGHT: 32px" ms_positioning="GridLayout">
					<uc1:ThreePartEartag id="ctlEartag" runat="server"></uc1:ThreePartEartag>
				</DIV>
				<asp:dropdownlist id="ddlFate" style="Z-INDEX: 101; LEFT: 568px; POSITION: absolute; TOP: 32px" runat="server" Width="168px"></asp:dropdownlist>
				<asp:dropdownlist id="ddlFinalResult" style="Z-INDEX: 130; LEFT: 184px; POSITION: absolute; TOP: 64px" runat="server" Width="168px"></asp:dropdownlist>
				<DIV style="Z-INDEX: 134; LEFT: 568px; WIDTH: 166px; POSITION: absolute; TOP: 64px; HEIGHT: 56px" ms_positioning="GridLayout">
					<uc1:calendardate id="ctlFinalResultDate" runat="server"></uc1:calendardate>
				</DIV>
				<DIV style="Z-INDEX: 133; LEFT: 184px; WIDTH: 208px; POSITION: absolute; TOP: 88px; HEIGHT: 8px" ms_positioning="GridLayout">
					<uc1:calendardate id="ctlSlaughterDate" runat="server"></uc1:calendardate>
				</DIV>
				<DIV style="Z-INDEX: 108; LEFT: 184px; WIDTH: 192px; POSITION: absolute; TOP: 144px; HEIGHT: 27px" ms_positioning="GridLayout">
					<uc1:cphh id="CPHH1" runat="server"></uc1:cphh>
				</DIV>
				<asp:button id="btnLookUp" style="Z-INDEX: 124; LEFT: 368px; POSITION: absolute; TOP: 144px" runat="server" Width="104px" Text="Look Up"></asp:button>
				<asp:textbox id="txtOwnerName" style="Z-INDEX: 111; LEFT: 184px; POSITION: absolute; TOP: 176px" runat="server" Width="168px" ToolTip="You must enter an Owner Name" MaxLength="100"></asp:textbox>
				<asp:textbox id="txtAddress1" style="Z-INDEX: 112; LEFT: 184px; POSITION: absolute; TOP: 208px" runat="server" Width="168px" ToolTip="You must enter an Address" MaxLength="50"></asp:textbox>
				<asp:textbox id="txtAddress2" style="Z-INDEX: 113; LEFT: 184px; POSITION: absolute; TOP: 240px" runat="server" Width="168px" MaxLength="50"></asp:textbox>
				<asp:textbox id="txtAddress3" style="Z-INDEX: 114; LEFT: 184px; POSITION: absolute; TOP: 272px" runat="server" Width="168px" MaxLength="50"></asp:textbox>
				<asp:textbox id="txtPostcode" style="Z-INDEX: 115; LEFT: 184px; POSITION: absolute; TOP: 304px" runat="server" Width="168px" MaxLength="10"></asp:textbox>
				<asp:dropdownlist id="ddlCounty" style="Z-INDEX: 119; LEFT: 568px; POSITION: absolute; TOP: 176px" runat="server" Width="168px"></asp:dropdownlist>
				<asp:textbox id="txtHerdmark" style="Z-INDEX: 121; LEFT: 568px; POSITION: absolute; TOP: 208px" runat="server" Width="168px" MaxLength="8"></asp:textbox>
				<asp:textbox id="txtNumericHerdmark" style="Z-INDEX: 123; LEFT: 568px; POSITION: absolute; TOP: 240px" runat="server" Width="168px" MaxLength="6"></asp:textbox>
				<P></P>
				<HR style="Z-INDEX: 140; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 16px; HEIGHT: 1px" width="96.14%" SIZE="1">
			</DIV>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 48px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 16px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<asp:button id="btnSave" style="Z-INDEX: 102; LEFT: 592px; POSITION: absolute; TOP: 24px" runat="server" Text="  Save  "></asp:button>
				<asp:button id="btnCancel" style="Z-INDEX: 103; LEFT: 664px; POSITION: absolute; TOP: 24px" runat="server" Text=" Cancel "></asp:button>
			</DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
		<P></P>
	</body>
</HTML>
