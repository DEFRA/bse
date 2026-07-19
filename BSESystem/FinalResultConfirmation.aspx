<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="FinalResultConfirmation.aspx.vb" Inherits="BSESystem.FinalResultConfirmation"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Final Result Confirmation</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 118px" ms_positioning="GridLayout">
				<asp:label id="lblRBSE" style="Z-INDEX: 101; LEFT: 32px; POSITION: absolute; TOP: 16px" runat="server" Font-Bold="True" Width="416px">RBSE: </asp:label>
				<asp:label id="lblDBSE" style="Z-INDEX: 102; LEFT: 32px; POSITION: absolute; TOP: 48px" runat="server" Font-Bold="True" Width="416px">DBSE: </asp:label>
				<asp:hyperlink id="hlDownload" style="Z-INDEX: 103; LEFT: 40px; POSITION: absolute; TOP: 80px" runat="server" Target="_blank" NavigateUrl="ResultMemo.aspx" ImageUrl="images/btnDownloadDisabled.gif" Enabled="False">HyperLink</asp:hyperlink>
				<asp:Button id="btnHome" style="Z-INDEX: 104; LEFT: 664px; POSITION: absolute; TOP: 80px" runat="server" Text="Home"></asp:Button>
				<asp:Button id="btnFinalResult" style="Z-INDEX: 105; LEFT: 248px; POSITION: absolute; TOP: 80px" runat="server" Text="Add Another Result"></asp:Button>
				<asp:Button id="btnPrintMemo" style="Z-INDEX: 106; LEFT: 136px; POSITION: absolute; TOP: 80px" runat="server" Text="Print Memo"></asp:Button></DIV>
			<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter>
		</form>
	</body>
</HTML>
