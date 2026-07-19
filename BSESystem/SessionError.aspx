<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SessionError.aspx.vb" Inherits="BSESystem.SessionError" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Application Error</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P>
				<uc1:vlaheader id="VLAHeader1" runat="server" PageTitle="Application Error"></uc1:vlaheader>
				<br>
				<br>
				<asp:label id="lblMessage" runat="server" CssClass="ErrorText" Width="100%">An error has occurred.</asp:label><br>
				<br>
				<asp:label id="Label1" runat="server" Width="100%" CssClass="LargerText">The application does not have data required to display the page you requested. This may have occurred for one of the following reasons:</asp:label></P>
			<P>
				<asp:label id="Label2" runat="server" Width="100%" CssClass="LargerText">* The user session has timed out.  This happens if you do not interact with the application for a long time.</asp:label></P>
			<P>
				<asp:label id="Label3" runat="server" Width="100%" CssClass="LargerText">* You attempted to access a page directly, eg. by typing in a URL or visiting a bookmarked page, without following through the steps required by the application to perform the task that the page normally carries out.</asp:label></P>
			<P>
				<asp:label id="Label4" runat="server" Width="100%" CssClass="LargerText">* You used the browser Back button to access pages in your browser cache that are no longer valid.</asp:label></P>
			<P>
				<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
		</form>
	</body>
</HTML>
