<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="AppError.aspx.vb" Inherits="BSESystem.AppError"%>
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
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<br>
			<br>
			<asp:label id="lblMessage" runat="server" CssClass="ErrorText" Width="100%"></asp:label><br>
			<br>
			<asp:label id="lblAdditionalCaption" runat="server" CssClass="smalltext" Width="100%" Visible="False">Additional error information:</asp:label><asp:label id="lblAdditional" runat="server" CssClass="smalltext" Width="100%"></asp:label><br>
			<br>
			<asp:label id="Label1" runat="server" Width="100%" CssClass="LargerText">If requested to do so by your system administrator, you may enter additional information about what you were doing when the error occurred here:</asp:label><asp:textbox id="txtUserInfo" runat="server" Width="100%" Height="96px" TextMode="MultiLine"></asp:textbox>
			<asp:button id="cmdSave" runat="server" Width="104px" Height="32px" Text="Save"></asp:button>
		</form>
	</body>
</HTML>
