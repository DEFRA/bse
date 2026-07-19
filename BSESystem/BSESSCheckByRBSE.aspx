<%@ Register TagPrefix="uc1" TagName="CPHH" Src="CPHH.ascx" %>
<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="BSESSCheckByRBSE.aspx.vb" Inherits="BSESystem.BSECheckByRBSE" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : BSESS Check : Compare Details By RBSE</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="WIDTH: 777px; POSITION: relative; HEIGHT: 264px" ms_positioning="GridLayout">
				<DIV style="Z-INDEX: 100; LEFT: 184px; WIDTH: 184px; POSITION: absolute; TOP: 16px; HEIGHT: 27px" ms_positioning="GridLayout"><uc1:rbse id="ctlRBSE" runat="server"></uc1:rbse></DIV>
				<asp:label id="lblRBSE" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 24px" runat="server" Width="152px">RBSE </asp:label>
				<P></P>
				<P>&nbsp;</P>
				<asp:label id="lblNotificationDate" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 88px" runat="server" Width="152px">Notification Date</asp:label><asp:label id="lblNotificationDateValue" style="Z-INDEX: 103; LEFT: 184px; POSITION: absolute; TOP: 88px" runat="server" Width="152px"></asp:label><asp:label id="lblBSESSEartagValue" style="Z-INDEX: 104; LEFT: 184px; POSITION: absolute; TOP: 112px" runat="server" Width="152px"></asp:label><asp:label id="lblBSESSBirthDateValue" style="Z-INDEX: 106; LEFT: 184px; POSITION: absolute; TOP: 136px" runat="server" Width="152px"></asp:label><asp:label id="lblTestGroupValue" style="Z-INDEX: 107; LEFT: 184px; POSITION: absolute; TOP: 160px" runat="server" Width="152px"></asp:label>
				<P>&nbsp;</P>
				<asp:button id="btnLookUp" style="Z-INDEX: 108; LEFT: 376px; POSITION: absolute; TOP: 16px" runat="server" Width="104px" Text="Look Up"></asp:button>
				<asp:label id="lblEartag" style="Z-INDEX: 109; LEFT: 16px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">Eartag</asp:label>
				<asp:label id="lblBirthDate" style="Z-INDEX: 110; LEFT: 16px; POSITION: absolute; TOP: 136px" runat="server" Width="152px">Birth Date</asp:label>
				<asp:label id="lblTestingGroup" style="Z-INDEX: 111; LEFT: 16px; POSITION: absolute; TOP: 160px" runat="server" Width="152px">Testing Group</asp:label>
				<asp:label id="lblFinalResult" style="Z-INDEX: 112; LEFT: 16px; POSITION: absolute; TOP: 184px" runat="server" Width="152px">Final Result</asp:label>
				<asp:label id="lblBarcode" style="Z-INDEX: 113; LEFT: 16px; POSITION: absolute; TOP: 208px" runat="server" Width="152px">Barcode</asp:label>
				<asp:label id="lblBSESSFinalResultValue" style="Z-INDEX: 114; LEFT: 184px; POSITION: absolute; TOP: 184px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblBarcodeValue" style="Z-INDEX: 115; LEFT: 184px; POSITION: absolute; TOP: 208px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblFormADate" style="Z-INDEX: 116; LEFT: 416px; POSITION: absolute; TOP: 88px" runat="server" Width="152px">Form A Date</asp:label>
				<asp:label id="lblFormADateValue" style="Z-INDEX: 117; LEFT: 584px; POSITION: absolute; TOP: 88px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblBSEEartag" style="Z-INDEX: 118; LEFT: 416px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">Eartag</asp:label>
				<asp:label id="lblBSEEartagValue" style="Z-INDEX: 119; LEFT: 584px; POSITION: absolute; TOP: 112px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblBSEBirthDate" style="Z-INDEX: 120; LEFT: 416px; POSITION: absolute; TOP: 136px" runat="server" Width="152px">Birth Date</asp:label>
				<asp:label id="lblBSEBirthDateValue" style="Z-INDEX: 121; LEFT: 584px; POSITION: absolute; TOP: 136px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblSurvey" style="Z-INDEX: 122; LEFT: 416px; POSITION: absolute; TOP: 160px" runat="server" Width="152px">Survey</asp:label>
				<asp:label id="lblSurveyValue" style="Z-INDEX: 123; LEFT: 584px; POSITION: absolute; TOP: 160px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblBSEFinalResult" style="Z-INDEX: 124; LEFT: 416px; POSITION: absolute; TOP: 184px" runat="server" Width="152px">Final Result</asp:label>
				<asp:label id="lblBSEFinalResultValue" style="Z-INDEX: 125; LEFT: 584px; POSITION: absolute; TOP: 184px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblBSESS" style="Z-INDEX: 126; LEFT: 184px; POSITION: absolute; TOP: 56px" runat="server" Width="184px" Font-Bold="True">BSESS</asp:label>
				<asp:label id="lblBSE" style="Z-INDEX: 127; LEFT: 584px; POSITION: absolute; TOP: 56px" runat="server" Width="184px" Font-Bold="True">BSE</asp:label>
				<INPUT style="Z-INDEX: 128; LEFT: 648px; POSITION: absolute; TOP: 16px" onclick="location.href='BSESSMenu.aspx'" type="button" value="BSESS Menu">
                <asp:Label ID="lblAHFReference" style="Z-INDEX: 129; LEFT: 16px; POSITION: absolute; TOP: 232px" runat="server" Text="AHF Reference"></asp:Label>
                <asp:Label ID="lblAHFReferenceValue" style="Z-INDEX: 130; LEFT: 184px; POSITION: absolute; TOP: 232px" runat="server"></asp:Label></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server" DESIGNTIMEDRAGDROP="40"></uc1:vlafooter></form>
		<P></P>
	</body>
</HTML>
