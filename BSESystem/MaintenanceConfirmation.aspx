<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="MaintenanceConfirmation.aspx.vb" Inherits="BSESystem.MaintenanceConfirmation"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : <% = Request.QueryString("title")%></title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<link href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 96px" ms_positioning="GridLayout">
				<asp:label id="lblMessage" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 24px" runat="server" Font-Bold="True" Width="710px"></asp:label>
				<asp:Button id="btnHome" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 56px" runat="server" Text="Home"></asp:Button></DIV>
			<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter>
		</form>
	</body>
</HTML>
