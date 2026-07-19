<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="MoveCase.aspx.vb" Inherits="BSESystem.MoveCase"%>
<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CPHH" Src="CPHH.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Move Case</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 416px" ms_positioning="GridLayout">
				<DIV style="Z-INDEX: 101; LEFT: 184px; WIDTH: 184px; POSITION: absolute; TOP: 16px; HEIGHT: 27px" ms_positioning="GridLayout">
					<uc1:rbse id="ctlRBSE" runat="server"></uc1:rbse>
				</DIV>
				<asp:button id="btnLookUp" style="Z-INDEX: 122; LEFT: 376px; POSITION: absolute; TOP: 16px" runat="server" Width="104px" Text="Look Up"></asp:button>
				<asp:label id="lblRBSE" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 24px" runat="server" Width="152px">RBSE </asp:label>
				<HR style="Z-INDEX: 103; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 184px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:label id="lblNewCPHHControl" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 200px" runat="server" Width="152px">New CPHH</asp:label>
				<DIV style="Z-INDEX: 105; LEFT: 184px; WIDTH: 192px; POSITION: absolute; TOP: 200px; HEIGHT: 27px" ms_positioning="GridLayout">
					<uc1:cphh id="ctlCPHH" runat="server"></uc1:cphh>
				</DIV>
				<asp:button id="btnCheck" style="Z-INDEX: 106; LEFT: 384px; POSITION: absolute; TOP: 200px" runat="server" Text=" Check " Enabled="False"></asp:button>
				<asp:label id="lblNewName" style="Z-INDEX: 107; LEFT: 16px; POSITION: absolute; TOP: 240px" runat="server" Width="152px">New Owners Name</asp:label>
				<asp:label id="lblNewNameValue" style="Z-INDEX: 108; LEFT: 184px; POSITION: absolute; TOP: 240px" runat="server" Width="152px"></asp:label>
				<HR style="Z-INDEX: 109; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 368px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:button id="btnOK" style="Z-INDEX: 128; LEFT: 608px; POSITION: absolute; TOP: 384px" runat="server" Text="  OK  " Enabled="False"></asp:button>
				<asp:button id="btnCancel" style="Z-INDEX: 110; LEFT: 672px; POSITION: absolute; TOP: 384px" runat="server" Text=" Cancel " CausesValidation="False"></asp:button>
				<asp:label id="lblOwnerName" style="Z-INDEX: 111; LEFT: 16px; POSITION: absolute; TOP: 56px" runat="server" Width="152px">Owner Name</asp:label>
				<asp:label id="lblAddress" style="Z-INDEX: 112; LEFT: 16px; POSITION: absolute; TOP: 88px" runat="server" Width="152px">Address</asp:label>
				<asp:label id="lblOwnerNameValue" style="Z-INDEX: 113; LEFT: 184px; POSITION: absolute; TOP: 56px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblAddress1Value" style="Z-INDEX: 114; LEFT: 184px; POSITION: absolute; TOP: 88px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblAddress2Value" style="Z-INDEX: 115; LEFT: 184px; POSITION: absolute; TOP: 112px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblAddress3Value" style="Z-INDEX: 116; LEFT: 184px; POSITION: absolute; TOP: 136px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblPostcodeValue" style="Z-INDEX: 117; LEFT: 184px; POSITION: absolute; TOP: 160px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblEartag" style="Z-INDEX: 118; LEFT: 416px; POSITION: absolute; TOP: 56px" runat="server" Width="152px">Eartag</asp:label>
				<asp:label id="lblCPHH" style="Z-INDEX: 119; LEFT: 416px; POSITION: absolute; TOP: 80px" runat="server" Width="152px">CPHH</asp:label>
				<asp:label id="lblEartagValue" style="Z-INDEX: 120; LEFT: 584px; POSITION: absolute; TOP: 56px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblCPHHValue" style="Z-INDEX: 121; LEFT: 584px; POSITION: absolute; TOP: 80px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblNewAddress" style="Z-INDEX: 123; LEFT: 16px; POSITION: absolute; TOP: 272px" runat="server" Width="152px">Address</asp:label>
				<asp:label id="lblNewAddress1Value" style="Z-INDEX: 124; LEFT: 184px; POSITION: absolute; TOP: 272px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblNewAddress2Value" style="Z-INDEX: 125; LEFT: 184px; POSITION: absolute; TOP: 296px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblNewAddress3Value" style="Z-INDEX: 126; LEFT: 184px; POSITION: absolute; TOP: 320px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblNewPostcodeValue" style="Z-INDEX: 127; LEFT: 184px; POSITION: absolute; TOP: 344px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblFarmMessage" style="Z-INDEX: 129; LEFT: 418px; POSITION: absolute; TOP: 112px" runat="server" Width="325px" Height="40px" Visible="False" Font-Bold="True">This is the only case on this farm so the farm record will be removed</asp:label>
				<asp:Button id="btnCreateNewFarm" style="Z-INDEX: 130; LEFT: 456px; POSITION: absolute; TOP: 200px" runat="server" Text="Create New Farm" Visible="False"></asp:Button></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server" DESIGNTIMEDRAGDROP="40"></uc1:vlafooter></form>
		<P></P>
	</body>
</HTML>
