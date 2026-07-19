<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="RBSEChange.aspx.vb" Inherits="BSESystem.RBSEChange"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : RBSE Change</title>
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
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 264px" ms_positioning="GridLayout">
				<asp:label id="lblOldRBSE" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Old RBSE</asp:label>
				<DIV style="Z-INDEX: 101; LEFT: 184px; WIDTH: 232px; POSITION: absolute; TOP: 16px; HEIGHT: 27px" ms_positioning="GridLayout">
					<uc1:RBSE id="ctlOldRBSE" runat="server"></uc1:RBSE></DIV>
				<asp:button id="btnLookUp" style="Z-INDEX: 102; LEFT: 368px; POSITION: absolute; TOP: 16px" runat="server" Width="104px" Text="Look Up"></asp:button>
				<HR style="Z-INDEX: 103; LEFT: 16px; WIDTH: 96.13%; POSITION: absolute; TOP: 56px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:label id="lblPostcodeValue" style="Z-INDEX: 105; LEFT: 184px; POSITION: absolute; TOP: 192px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblAddress3Value" style="Z-INDEX: 106; LEFT: 184px; POSITION: absolute; TOP: 168px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblAddress2Value" style="Z-INDEX: 107; LEFT: 184px; POSITION: absolute; TOP: 144px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblAddress1Value" style="Z-INDEX: 108; LEFT: 184px; POSITION: absolute; TOP: 120px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblOwnerNameValue" style="Z-INDEX: 109; LEFT: 184px; POSITION: absolute; TOP: 88px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblOwnerName" style="Z-INDEX: 110; LEFT: 16px; POSITION: absolute; TOP: 88px" runat="server" Width="152px">Owner Name</asp:label>
				<asp:label id="lblAddress" style="Z-INDEX: 111; LEFT: 16px; POSITION: absolute; TOP: 120px" runat="server" Width="152px">Address</asp:label>
				<asp:label id="lblEartag" style="Z-INDEX: 112; LEFT: 416px; POSITION: absolute; TOP: 88px" runat="server" Width="152px">Eartag</asp:label>
				<asp:label id="lblEartagValue" style="Z-INDEX: 113; LEFT: 584px; POSITION: absolute; TOP: 88px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblCPHH" style="Z-INDEX: 114; LEFT: 416px; POSITION: absolute; TOP: 120px" runat="server" Width="152px">CPHH</asp:label>
				<asp:label id="lblCPHHValue" style="Z-INDEX: 115; LEFT: 584px; POSITION: absolute; TOP: 120px" runat="server" Width="152px"></asp:label>
				<P>&nbsp;</P>
				<HR style="Z-INDEX: 116; LEFT: 16px; WIDTH: 96.13%; POSITION: absolute; TOP: 216px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<DIV style="Z-INDEX: 118; LEFT: 184px; WIDTH: 232px; POSITION: absolute; TOP: 224px; HEIGHT: 27px" ms_positioning="GridLayout">
					<uc1:RBSE id="ctlNewRBSE" runat="server"></uc1:RBSE></DIV>
				<asp:label id="lblNewRBSE" style="Z-INDEX: 119; LEFT: 16px; POSITION: absolute; TOP: 232px" runat="server" Width="152px">New RBSE</asp:label>
				<asp:Button id="btnOK" style="Z-INDEX: 117; LEFT: 632px; POSITION: absolute; TOP: 232px" runat="server" Text="  OK  " Enabled="False"></asp:Button>
				<asp:Button id="btnCancel" style="Z-INDEX: 121; LEFT: 688px; POSITION: absolute; TOP: 232px" runat="server" Text="Cancel" CausesValidation="False"></asp:Button></DIV>
			<P>
				<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
		</form>
	</body>
</HTML>
