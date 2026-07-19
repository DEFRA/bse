<%@ Page Language="vb" AutoEventWireup="false" Codebehind="OSSExportMenu.aspx.vb" Inherits="BSESystem.OSSExportMenu"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : OSS Export Menu</title>
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
				<asp:HyperLink id="hlBSE1" runat="server" NavigateUrl="OSSExportBSE1.aspx">BSE1</asp:HyperLink></P>
			<P align="center">
				<asp:HyperLink id="hlBSE1b" runat="server" NavigateUrl="OSSExportBSE1b.aspx">BSE1b</asp:HyperLink></P>
			<P>
				<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
		</form>
	</body>
</HTML>
