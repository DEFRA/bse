<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ShowCase.aspx.vb" Inherits="BSESystem.ShowCase"%>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ShowCase</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<asp:Panel id="Panel2" runat="server" Width="288px" Height="24px">
				<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 32px" ms_positioning="GridLayout">
					<asp:Label id="lblMessage" style="Z-INDEX: 101; LEFT: 8px; POSITION: absolute; TOP: 8px" runat="server" Width="728px"></asp:Label>
					<P></P>
				</DIV>
			</asp:Panel>
			<p>
				<uc1:vlafooter id="VLAFooter1" runat="server" DESIGNTIMEDRAGDROP="868"></uc1:vlafooter>
			</p>
		</form>
	</body>
</HTML>
