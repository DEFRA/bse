<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CPHHChange.aspx.vb" Inherits="BSESystem.CPHHChange"%>
<%@ Register TagPrefix="uc1" TagName="CPHH" Src="CPHH.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : CPHH Change</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<link href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P>
				<uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 306px" ms_positioning="GridLayout">
				<asp:label id="lblOldCPHH" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Old CPHH</asp:label>
				<asp:label id="lblOwnerName" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 88px" runat="server" Width="152px">Owner Name</asp:label>
				<DIV style="Z-INDEX: 105; LEFT: 184px; WIDTH: 176px; POSITION: absolute; TOP: 16px; HEIGHT: 27px" ms_positioning="GridLayout">
					<uc1:CPHH id="ctlOldCPHH" runat="server"></uc1:CPHH>
				</DIV>
				<asp:button id="btnLookUp" style="Z-INDEX: 103; LEFT: 368px; POSITION: absolute; TOP: 16px" runat="server" Width="104px" Text="Look Up"></asp:button>
				<asp:label id="lblOwnerNameValue" style="Z-INDEX: 106; LEFT: 184px; POSITION: absolute; TOP: 88px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblAddress" style="Z-INDEX: 107; LEFT: 16px; POSITION: absolute; TOP: 120px" runat="server" Width="152px">Address</asp:label>
				<asp:label id="lblAddress1Value" style="Z-INDEX: 108; LEFT: 184px; POSITION: absolute; TOP: 120px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblAddress2Value" style="Z-INDEX: 109; LEFT: 184px; POSITION: absolute; TOP: 144px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblAddress3Value" style="Z-INDEX: 110; LEFT: 184px; POSITION: absolute; TOP: 168px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblPostcodeValue" style="Z-INDEX: 111; LEFT: 184px; POSITION: absolute; TOP: 192px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblConfirmedCases" style="Z-INDEX: 112; LEFT: 16px; POSITION: absolute; TOP: 224px" runat="server" Width="168px">Number of Confirmed Cases</asp:label>
				<asp:label id="lblConfirmedCasesValue" style="Z-INDEX: 113; LEFT: 184px; POSITION: absolute; TOP: 224px" runat="server" Width="152px"></asp:label>
				<HR style="Z-INDEX: 114; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 56px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<DIV style="Z-INDEX: 104; LEFT: 184px; WIDTH: 216px; POSITION: absolute; TOP: 264px; HEIGHT: 27px" ms_positioning="GridLayout">
					<uc1:CPHH id="ctlNewCPHH" runat="server"></uc1:CPHH></DIV>
				<P></P>
				<HR style="Z-INDEX: 115; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 248px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:label id="lblNewCPHH" style="Z-INDEX: 116; LEFT: 16px; POSITION: absolute; TOP: 264px" runat="server" Width="152px">New CPHH</asp:label>
				<asp:Button id="btnOK" style="Z-INDEX: 117; LEFT: 624px; POSITION: absolute; TOP: 272px" runat="server" Text="  OK  " Enabled="False"></asp:Button>
				<asp:Button id="btnCancel" style="Z-INDEX: 118; LEFT: 688px; POSITION: absolute; TOP: 272px" runat="server" Text="Cancel" CausesValidation="False"></asp:Button>
				<asp:label id="lblError" style="Z-INDEX: 119; LEFT: 16px; POSITION: absolute; TOP: 64px" runat="server" Width="416px" Font-Bold="True" Visible="False">A farm with this CPHH was not found</asp:label>
			</DIV>
			<P>
				<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
		</form>
	</body>
</HTML>
