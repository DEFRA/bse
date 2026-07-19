<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CPHH" Src="CPHH.ascx" %>
<%@ Register TagPrefix="uc1" TagName="BatchNumberDisplay" Src="BatchNumberDisplay.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CaseEntryBAB.aspx.vb" Inherits="BSESystem.CaseEntryBAB" smartNavigation="true"%>
<%@ Register TagPrefix="uc1" TagName="ExitConfirmation" Src="ExitConfirmation.ascx" %>
<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Case Details</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="Z-INDEX: 400; WIDTH: 750px; POSITION: relative; HEIGHT: 40px" ms_positioning="GridLayout"><asp:label id="lblRBSEHeader" style="Z-INDEX: 100; LEFT: 8px; POSITION: absolute; TOP: 2px" runat="server" Width="302px" Font-Bold="True">RBSE Number:  12/12/12345</asp:label>
				<DIV style="Z-INDEX: 101; LEFT: 320px; WIDTH: 242px; POSITION: absolute; TOP: 0px; HEIGHT: 27px" ms_positioning="GridLayout"><uc1:batchnumberdisplay id="BatchNumberDisplay1" runat="server"></uc1:batchnumberdisplay></DIV>
				<asp:button id="btnSave2" style="Z-INDEX: 102; LEFT: 592px; POSITION: absolute; TOP: 0px" runat="server" Text="  Save  "></asp:button>
				<asp:button id="btnCancel2" style="Z-INDEX: 104; LEFT: 664px; POSITION: absolute; TOP: 0px" runat="server" Text=" Cancel " CausesValidation="False"></asp:button>
				<DIV style="Z-INDEX: 104; LEFT: 496px; WIDTH: 88px; POSITION: absolute; TOP: 0px; HEIGHT: 32px" ms_positioning="GridLayout">
					<asp:Literal id="litViewDocs2" runat="server"></asp:Literal></DIV>
			</DIV>
			<div style="WIDTH: 754px; POSITION: relative; HEIGHT: 32px" ms_positioning="GridLayout">&nbsp;
				<div class="TabEnd" style="Z-INDEX: 101; LEFT: 0px; WIDTH: 8px; POSITION: absolute; TOP: 0px; HEIGHT: 27px"></div>
				<div class="UnselectedTabTitle" style="Z-INDEX: 105; LEFT: 8px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbFarm" runat="server" CssClass="tablink">Farm</asp:linkbutton></div>
				<div class="UnselectedTabTitle" style="Z-INDEX: 106; LEFT: 108px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbCaseDEFRA" runat="server" CssClass="TABLINK">Case&nbsp;(DEFRA)</asp:linkbutton></div>
				<div class="SelectedTabTitle" style="Z-INDEX: 102; LEFT: 208px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px">BAB</div>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 107; LEFT: 308px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbCaseVLA" runat="server" CssClass="tablink">Case (VLA)</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 104; LEFT: 408px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbClinical" runat="server" CssClass="tablink">Clinical</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 103; LEFT: 508px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbFeeds" runat="server" CssClass="tablink">Feeds</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 108; LEFT: 608px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbRelations" runat="server" CssClass="tablink">Relations</asp:linkbutton></DIV>
				<DIV class="TabEnd" style="Z-INDEX: 109; LEFT: 708px; WIDTH: 42px; POSITION: absolute; TOP: 0px; HEIGHT: 27px"></DIV>
			</div>
			<DIV style="Z-INDEX: 200; WIDTH: 750px; POSITION: relative; HEIGHT: 324px" ms_positioning="GridLayout"><asp:label id="lblNotes" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Notes</asp:label><asp:label id="lblOrigin" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 120px" runat="server" Width="152px">Origin</asp:label><asp:label id="lblTracedCPHH" style="Z-INDEX: 110; LEFT: 16px; POSITION: absolute; TOP: 152px" runat="server" Width="152px">Traced CPHH</asp:label><asp:label id="lblTracedName" style="Z-INDEX: 111; LEFT: 16px; POSITION: absolute; TOP: 184px" runat="server" Width="152px">Traced Name</asp:label><asp:label id="lblTracedPostcode" style="Z-INDEX: 112; LEFT: 400px; POSITION: absolute; TOP: 216px" runat="server" Width="152px">Traced Postcode</asp:label><asp:label id="lblTracedAddress" style="Z-INDEX: 113; LEFT: 400px; POSITION: absolute; TOP: 120px" runat="server" Width="152px">Traced Address</asp:label><asp:label id="lblFeedRisk" style="Z-INDEX: 115; LEFT: 16px; POSITION: absolute; TOP: 264px" runat="server" Width="152px">Feed Risk</asp:label><asp:label id="lblHoizontalRisk" style="Z-INDEX: 116; LEFT: 400px; POSITION: absolute; TOP: 264px" runat="server" Width="152px">Horizontal Risk</asp:label><asp:label id="lblMaternalRisk" style="Z-INDEX: 117; LEFT: 16px; POSITION: absolute; TOP: 296px" runat="server" Width="152px">Maternal Risk</asp:label>
				<HR style="Z-INDEX: 114; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 248px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<HR style="Z-INDEX: 103; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 104px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<asp:textbox id="txtNotes" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 40px" runat="server" Width="724px" MaxLength="500" TextMode="MultiLine" Height="48px"></asp:textbox><asp:dropdownlist id="ddlOrigin" style="Z-INDEX: 118; LEFT: 184px; POSITION: absolute; TOP: 120px" runat="server" Width="168px" AutoPostBack="True" Font-Size="90%"></asp:dropdownlist>
				<DIV style="Z-INDEX: 122; LEFT: 184px; WIDTH: 264px; POSITION: absolute; TOP: 152px; HEIGHT: 27px" ms_positioning="GridLayout"><uc1:cphh id="ctlTracedCPHH" runat="server"></uc1:cphh></DIV>
				<asp:textbox id="txtTracedName" style="Z-INDEX: 108; LEFT: 184px; POSITION: absolute; TOP: 184px" runat="server" Width="168px" MaxLength="30"></asp:textbox><asp:textbox id="txtTracedAddress1" style="Z-INDEX: 105; LEFT: 560px; POSITION: absolute; TOP: 120px" runat="server" Width="168px" MaxLength="30"></asp:textbox><asp:textbox id="txtTracedAddress2" style="Z-INDEX: 106; LEFT: 560px; POSITION: absolute; TOP: 152px" runat="server" Width="168px" MaxLength="30"></asp:textbox><asp:textbox id="txtTracedAddress3" style="Z-INDEX: 107; LEFT: 560px; POSITION: absolute; TOP: 184px" runat="server" Width="168px" MaxLength="30"></asp:textbox><asp:textbox id="txtTracedPostcode" style="Z-INDEX: 109; LEFT: 560px; POSITION: absolute; TOP: 216px" runat="server" Width="168px" MaxLength="10"></asp:textbox><asp:dropdownlist id="ddlFeedRisk" style="Z-INDEX: 121; LEFT: 184px; POSITION: absolute; TOP: 264px" runat="server" Width="168px" Font-Size="90%"></asp:dropdownlist><asp:dropdownlist id="ddlHoizontalRisk" style="Z-INDEX: 120; LEFT: 560px; POSITION: absolute; TOP: 264px" runat="server" Width="172px" Font-Size="90%"></asp:dropdownlist><asp:dropdownlist id="ddlMaternalRisk" style="Z-INDEX: 119; LEFT: 184px; POSITION: absolute; TOP: 296px" runat="server" Width="168px" Font-Size="90%"></asp:dropdownlist>
				<DIV id="exitConfirmationDIV" style="Z-INDEX: 167; LEFT: 296px; WIDTH: 316px; POSITION: absolute; TOP: 8px; HEIGHT: 8px" ms_positioning="GridLayout">
					<uc1:ExitConfirmation id="ctlExitConfirmation" runat="server"></uc1:ExitConfirmation></DIV>
			</DIV>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 61px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 16px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<asp:button id="btnSave" style="Z-INDEX: 102; LEFT: 592px; POSITION: absolute; TOP: 24px" runat="server" Text="  Save  "></asp:button><asp:button id="btnCancel" style="Z-INDEX: 103; LEFT: 664px; POSITION: absolute; TOP: 24px" runat="server" Text=" Cancel " CausesValidation="False"></asp:button>
				<DIV style="Z-INDEX: 104; LEFT: 496px; WIDTH: 88px; POSITION: absolute; TOP: 24px; HEIGHT: 32px" ms_positioning="GridLayout">
					<asp:Literal id="litViewDocs" runat="server"></asp:Literal></DIV>
			</DIV>
			<P title="Case Details"><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</form>
	</body>
</HTML>
