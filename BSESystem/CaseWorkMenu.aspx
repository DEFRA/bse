<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="CaseWorkMenu.aspx.vb" Inherits="BSESystem.CaseWorkMenu" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>BSE System : Casework Menu</title>
    <meta name="vs_defaultClientScript" content="JavaScript">
	<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
	<link href="Style/vla-ie.css" type="text/css" rel="stylesheet">
</head>
<body>
    <form id="form1" method="post" runat="server">
    <P>
	    <uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader></P>
	<P align="center">
		<asp:HyperLink id="hlViewAllOpenCases" runat="server" NavigateUrl="CaseWorkOpenReport.aspx">View all open cases</asp:HyperLink></P>
	<P align="center">
		<asp:HyperLink id="hlViewAllClosedCases" runat="server" NavigateUrl="CaseWorkClosedReport.aspx">View all closed cases</asp:HyperLink></P>
	<P align="left">
		<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
    </form>
</body>
</html>

