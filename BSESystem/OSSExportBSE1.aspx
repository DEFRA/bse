<%@ Page Language="vb" AutoEventWireup="false" Codebehind="OSSExportBSE1.aspx.vb" Inherits="BSESystem.OSSExportBSE1"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="BatchNumber" Src="BatchNumber.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : OSS Export : BSE1</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 102px" ms_positioning="GridLayout">
				<DIV style="Z-INDEX: 101; LEFT: 128px; WIDTH: 296px; POSITION: absolute; TOP: 16px; HEIGHT: 24px" ms_positioning="GridLayout"><uc1:batchnumber id="ctlBatchNumber" runat="server"></uc1:batchnumber></DIV>
				<asp:label id="lblBatchNumber" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="96px">Batch Number</asp:label><asp:button id="btnDownload" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 56px" runat="server" Text="Download"></asp:button>
				<asp:Button id="btnHome" style="Z-INDEX: 104; LEFT: 120px; POSITION: absolute; TOP: 56px" runat="server" Text="OSS Menu"></asp:Button>
				<asp:Label id="lblTruncateError" style="Z-INDEX: 105; LEFT: 224px; POSITION: absolute; TOP: 56px" runat="server" Width="272px" ForeColor="Red" Visible="False">N.B. The Batch number you entered was longer than 3 digits so has been truncated.</asp:Label></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
