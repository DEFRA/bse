<%@ Page Language="vb" AutoEventWireup="false" Codebehind="AuditLogMenu.aspx.vb" Inherits="BSESystem.AuditLogMenu"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Audit Log Menu</title>
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
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 160px" ms_positioning="GridLayout">
				<P align="center">
					<asp:HyperLink id="hlDailyAuditLog" runat="server" NavigateUrl="AuditLogByDate.aspx" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 16px" Width="722px">Daily Audit Log</asp:HyperLink></P>
				<P align="center">
					<asp:HyperLink id="hlNewFarms" runat="server" NavigateUrl="AuditLogNewFarms.aspx" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 40px" Width="721px">New Farms</asp:HyperLink></P>
				<P align="center">
					<asp:HyperLink id="hlCPHHChanges" runat="server" NavigateUrl="AuditLogCPHHChanges.aspx" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 64px" Width="723px">CPHH Changes</asp:HyperLink></P>
				<P align="center">
					<asp:HyperLink id="hlRBSEChanges" runat="server" NavigateUrl="AuditLogRBSEChanges.aspx" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 88px" Width="720px">RBSE Changes</asp:HyperLink></P>
				<P align="center">
					<asp:HyperLink id="hlCaseMovesList" runat="server" NavigateUrl="AuditLogCaseMoves.aspx" style="Z-INDEX: 105; LEFT: 16px; POSITION: absolute; TOP: 112px" Width="723px">Case Moves List</asp:HyperLink></P>
				<P align="center">
					<asp:HyperLink id="hlAuditLogByUser" runat="server" NavigateUrl="AuditLogByUser.aspx" style="Z-INDEX: 106; LEFT: 16px; POSITION: absolute; TOP: 136px" Width="723px">Audit Log By User</asp:HyperLink></P>
			</DIV>
			<P><uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
		</form>
	</body>
</HTML>
