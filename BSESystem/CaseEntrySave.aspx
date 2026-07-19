<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CaseEntrySave.aspx.vb" Inherits="BSESystem.CaseEntrySave"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Save Case Details</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 37px" ms_positioning="GridLayout"><asp:label id="lblRBSE" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Font-Bold="True" Width="328px">RBSE: 12/34/56789</asp:label>
				<P></P>
				<P></P>
			</DIV>
			<div id="ctlDIV" style="WIDTH: 750px; POSITION: relative" runat="server" ms_positioning="FlowLayout"></div>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 38px" ms_positioning="GridLayout">
				<P></P>
				<P></P>
				<asp:button id="btnOK" style="Z-INDEX: 106; LEFT: 16px; POSITION: absolute; TOP: 8px" runat="server" Width="80px" Text="OK"></asp:button></DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</form>
	</body>
</HTML>
