<%@ Page Language="vb" AutoEventWireup="false" Codebehind="BSESSMenu.aspx.vb" Inherits="BSESystem.BSESSMenu" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : BSESS Check Menu</title>
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
			<P align="center">
				<asp:HyperLink id="hlCheckByDate" runat="server" NavigateUrl="BSESSCheckByDate.aspx">Compare Cases By Date</asp:HyperLink></P>
			<P align="center">
				<asp:HyperLink id="hlCheckByRBSE" runat="server" NavigateUrl="BSESSCheckByRBSE.aspx">Compare Details By RBSE</asp:HyperLink></P>
			<P align="left">
				<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
		</form>
	</body>
</HTML>
