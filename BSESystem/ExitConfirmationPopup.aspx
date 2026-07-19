<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ExitConfirmationPopup.aspx.vb" Inherits="BSESystem.ExitConfirmationPopup"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ExitConfirmationPopup</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body bgColor="#003399" MS_POSITIONING="GridLayout">
		<form id="Form1" method="post" runat="server">
			<DIV style="Z-INDEX: 1; LEFT: 0px; WIDTH: 205px; POSITION: absolute; TOP: 0px; HEIGHT: 100px; BACKGROUND-COLOR: #003399" ms_positioning="GridLayout">
				<DIV style="Z-INDEX: 102; LEFT: 1px; WIDTH: 197px; POSITION: absolute; TOP: 1px; HEIGHT: 40px; BACKGROUND-COLOR: white" ms_positioning="GridLayout">
					<asp:label id="lblMessage" style="Z-INDEX: 105; LEFT: 8px; POSITION: absolute; TOP: 8px" runat="server" Width="180px" ForeColor="#002163" CssClass="topnavtext">If you exit now the changes you have made will be lost.</asp:label></DIV>
				<asp:Literal id="litText" runat="server"></asp:Literal>
				<asp:LinkButton id="btnNo" style="Z-INDEX: 103; LEFT: 112px; POSITION: absolute; TOP: 48px" runat="server" ForeColor="White" CssClass="topnavlinks">Cancel</asp:LinkButton>
				<asp:LinkButton id="btnYes" style="Z-INDEX: 104; LEFT: 40px; POSITION: absolute; TOP: 48px" runat="server" ForeColor="White" CssClass="topnavlinks"> Exit </asp:LinkButton>
		</form>
		</DIV>
	</body>
</HTML>
