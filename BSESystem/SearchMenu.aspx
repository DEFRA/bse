<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SearchMenu.aspx.vb" Inherits="BSESystem.SearchMenu"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Search Menu</title>
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
					<asp:HyperLink id="hlFarmSearch" runat="server" NavigateUrl="SearchFarm.aspx" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 16px" Width="720px">Farm Search</asp:HyperLink></P>
				<P align="center">
					<asp:HyperLink id="hlCaseSearch" runat="server" NavigateUrl="SearchCase.aspx" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 40px" Width="720px">Case Search</asp:HyperLink></P>
				<P align="center">
					<asp:HyperLink id="hlCaseSearchByHerdmark" runat="server" NavigateUrl="SearchCaseByHerdmark.aspx" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 64px" Width="722px">List Of Cases With A Given Herdmark (includes animals sold off)</asp:HyperLink></P>
				<P align="center">
					<asp:HyperLink id="hlCasesByCPHH" runat="server" NavigateUrl="SearchCPHH.aspx" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 88px" Width="722px">List Of Cases For A Given Holding/Herdmark (for organic farmer checks)</asp:HyperLink></P>
				<P align="center">
					<asp:HyperLink id="hlRelatedAnimalsSearch" runat="server" NavigateUrl="SearchRelatedAnimal.aspx" style="Z-INDEX: 105; LEFT: 16px; POSITION: absolute; TOP: 112px" Width="721px">Related Animals Search</asp:HyperLink></P>
				<P align="center">
					<asp:HyperLink id="hlOutstandingDataList" runat="server" NavigateUrl="SearchOutstandingData.aspx" style="Z-INDEX: 106; LEFT: 16px; POSITION: absolute; TOP: 136px" Width="722px">Outstanding Data List</asp:HyperLink></P>
			</DIV>
			<P><uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
		</form>
	</body>
</HTML>
