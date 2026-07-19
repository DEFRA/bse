<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ResultMemo.aspx.vb" Inherits="BSESystem.ResultMemo"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ResultMemo</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<font style="FONT-SIZE: 12pt; FONT-FAMILY: Arial">
				<P align="center"><b>MEMORANDUM</b></P>
				<P align="left">
					<asp:Label id="lblTo" runat="server">To: </asp:Label><br>
					<asp:Label id="lblTo2" runat="server" Visible="false">  Truro</asp:Label></P>
				<P align="right">
					<asp:Label id="lblCaseRef" runat="server">Case Ref: 03/00001</asp:Label></P>
				<P align="right">
					<asp:Label id="lblDBSERef" runat="server">DBSE Ref: 03/00001</asp:Label></P>
				<P align="right">
					<asp:Label id="lblAHO" runat="server">AHO: 45TRURO</asp:Label></P>
				<P align="right">
					<asp:Label id="lblCPHH" runat="server">CPHH: 01/001/0001/01</asp:Label></P>
				<P align="right">
					<asp:Label id="lblReportDate" runat="server">Date of report: 07/04/03</asp:Label></P>
				<P><b>BOVINE SPONGIFORM ENCEPHALOPATHY</b></P>
				<p>Immunohistochemical and / or Western Blot analysis of brain tissue from</p>
				<P>
					<asp:Label id="lblAnimalID" runat="server">Animal ID: </asp:Label>
					&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<asp:Label id="lblAnimalIDValue" runat="server" Width="480px"></asp:Label></P>
				<p>
					<asp:Label id="lblBelongingTo" runat="server">Belonging to: </asp:Label>
					&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<asp:Label id="lblName" runat="server" Width="364px"></asp:Label><br>
					&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<asp:Label id="lblAddress1Value" runat="server" Width="400px"></asp:Label><br>
					&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<asp:Label id="lblAddress2Value" runat="server" Width="480px"></asp:Label><br>
					&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<asp:Label id="lblAddress3Value" runat="server" Width="376px"></asp:Label><br>
					&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<asp:Label id="lblPostcodeValue" runat="server" Width="320px"></asp:Label>
				</p>
				<P>
					<asp:Label id="lblConfirm" runat="server">Disease is CONFIRMED on the premises</asp:Label></P>
				<P>
					<asp:Label id="lblParsh" runat="server">in the parish of: </asp:Label>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
					<asp:Label id="lblParishValue" runat="server" Width="472px"></asp:Label></P>
				<P>
					<asp:Label id="lblDistrict" runat="server">District: </asp:Label>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
					<asp:Label id="lblDistrictValue" runat="server" Width="472px"></asp:Label></P>
				<P>
					<asp:Label id="lblCounty" runat="server">County: </asp:Label>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
					<asp:Label id="lblCountyValue" runat="server" Width="472px"></asp:Label></P>
					
				<p>	
				    <asp:Label id="lblAlternateResult" runat="server">Histological examination of brain samples from this case revealed alternative pathology</asp:Label>
				
				</p>
                <p>
                    <asp:Table ID="tblAlternateResultDetails" runat="server" Width="606px" BorderWidth="1px" CellPadding="0" CellSpacing="0" GridLines="Both">
                        <asp:TableRow runat="server">
                            <asp:TableHeaderCell runat="server">Date of Birth</asp:TableHeaderCell>
                            <asp:TableHeaderCell runat="server">Pathology Report</asp:TableHeaderCell>
                        </asp:TableRow>
                        <asp:TableRow runat="server">
                            <asp:TableCell runat="server"><asp:Label id="lblDateOfBirth" runat="server">DOB</asp:Label></asp:TableCell>
                            <asp:TableCell runat="server"><asp:Label id="lblAlternateDiagnosis" runat="server">Alternate Diagnosis</asp:Label></asp:TableCell>
                        </asp:TableRow>
                    </asp:Table>
                    &nbsp;</p>
                <p>
                    Please copy to local SVIO/VS</p>
			</font>
		</form>
	</body>
	<script language="javascript">
	if (window.print)
		window.print();
	</script>
</HTML>
