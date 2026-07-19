<%@ Page Language="vb" AutoEventWireup="false" Codebehind="NewFarm.aspx.vb" Inherits="BSESystem.NewFarm"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : New Farm</title>
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
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 40px" ms_positioning="GridLayout">
				<asp:label id="lblCPHH" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="364px" Font-Bold="True">CPHH: 123</asp:label>
			</DIV>
			<asp:Panel id="VetnetPanel" runat="server" Width="750px" Height="96px">
				<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 118px" ms_positioning="GridLayout">
					<asp:label id="lblVetnet" style="Z-INDEX: 115; LEFT: 16px; POSITION: absolute; TOP: 24px" runat="server" Width="152px">Found on Vetnet</asp:label>
					<asp:label id="lblHerdmark" style="Z-INDEX: 115; LEFT: 16px; POSITION: absolute; TOP: 56px" runat="server" Width="152px">Herdmark</asp:label>
					<asp:label id="lblHerdmarkValue" style="Z-INDEX: 115; LEFT: 192px; POSITION: absolute; TOP: 56px" runat="server" Width="152px"></asp:label>
					<asp:label id="lblAcceptVetnetDetails" style="Z-INDEX: 115; LEFT: 16px; POSITION: absolute; TOP: 88px" runat="server" Width="160px">Accept Vetnet Details?</asp:label>
					<asp:CheckBox id="chkAcceptVetnetDetails" style="Z-INDEX: 115; LEFT: 192px; POSITION: absolute; TOP: 88px" tabIndex="20" runat="server" Width="112px" Text=" "></asp:CheckBox>
					<asp:label id="lblNumericHerdmark" style="Z-INDEX: 115; LEFT: 408px; POSITION: absolute; TOP: 56px" runat="server" Width="152px">Numeric Herdmark</asp:label>
					<asp:label id="lblNumericHerdmarkValue" style="Z-INDEX: 115; LEFT: 584px; POSITION: absolute; TOP: 56px" runat="server" Width="152px"></asp:label>
					<HR style="Z-INDEX: 101; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 8px; HEIGHT: 1px" width="96.14%" SIZE="1">
				</DIV>
			</asp:Panel>
			<asp:Panel id="NotVetnetPanel" runat="server" Width="750px" Height="48px">
				<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 48px" ms_positioning="GridLayout">
					<asp:label id="lblNotVetnet" style="Z-INDEX: 115; LEFT: 16px; POSITION: absolute; TOP: 24px" runat="server" Width="400px">The CPHH you entered was not Found on Vetnet</asp:label>
					<HR style="Z-INDEX: 101; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 8px; HEIGHT: 1px" width="96.14%" SIZE="1">
				</DIV>
			</asp:Panel>
			<asp:Panel id="LocalAuthorityPanel" runat="server">
				<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 44px" ms_positioning="GridLayout">
					<asp:label id="lblBrussels" style="Z-INDEX: 112; LEFT: 16px; POSITION: absolute; TOP: 24px" runat="server" Font-Bold="True" Width="718px">Local authority not on file – case won’t be notifiable to Brussels.  Please investigate.</asp:label>
					<HR style="Z-INDEX: 110; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 8px; HEIGHT: 1px" width="96.14%" SIZE="1">
				</DIV>
			</asp:Panel>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 63px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 8px; WIDTH: 96.14%; POSITION: absolute; TOP: 8px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:Button id="btnCreateFarm" style="Z-INDEX: 102; LEFT: 536px; POSITION: absolute; TOP: 24px" tabIndex="30" runat="server" Text="Create Farm"></asp:Button>
				<asp:Button id="btnCancel" style="Z-INDEX: 100; LEFT: 656px; POSITION: absolute; TOP: 24px" tabIndex="31" runat="server" Text=" Cancel "></asp:Button>
				<asp:TextBox id="txtParishName" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Visible="False"></asp:TextBox>
				<asp:TextBox id="txtCounty" style="Z-INDEX: 104; LEFT: 160px; POSITION: absolute; TOP: 16px" runat="server" Visible="False"></asp:TextBox>
				<asp:TextBox id="txtADNSRegionID" style="Z-INDEX: 105; LEFT: 304px; POSITION: absolute; TOP: 16px" runat="server" Visible="False"></asp:TextBox>
				<asp:TextBox id="txtAuthorityID" style="Z-INDEX: 107; LEFT: 440px; POSITION: absolute; TOP: 16px" runat="server" Width="32px" Visible="False"></asp:TextBox>
				<asp:TextBox id="txtAuthorityCountyID" style="Z-INDEX: 108; LEFT: 480px; POSITION: absolute; TOP: 16px" runat="server" Width="32px" Visible="False"></asp:TextBox></DIV>
			<P>
				<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
		</form>
	</body>
</HTML>
