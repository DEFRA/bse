<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="BatchNumberDisplay" Src="BatchNumberDisplay.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CaseEntryDEFRA.aspx.vb" Inherits="BSESystem.CaseEntryDEFRA" smartNavigation="true"%>
<%@ Register TagPrefix="uc1" TagName="ExitConfirmation" Src="ExitConfirmation.ascx" %>
<%@ Register TagPrefix="uc1" TagName="Eartag" Src="Eartag.ascx" %>
<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<%@ Register TagPrefix="uc1" TagName="ThreePartEartag" Src="ThreePartEartag.ascx" %>

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Case Details</title>
		<META content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<META content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<META content="JavaScript" name="vs_defaultClientScript">
		<META content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
        <link href="Style/vla-ie.css" rel="stylesheet" type="text/css" />
        <link href="Style/vla-ie.css" rel="stylesheet" type="text/css" />
        <link href="Style/vla-ie.css" rel="stylesheet" type="text/css" />
        <link href="Style/vla-ie.css" rel="stylesheet" type="text/css" />

	</HEAD>
	<BODY>
		<FORM id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="Z-INDEX: 400; WIDTH: 750px; POSITION: relative; HEIGHT: 40px" ms_positioning="GridLayout"><asp:label id="lblRBSEHeader" style="Z-INDEX: 100; LEFT: 8px; POSITION: absolute; TOP: 2px" runat="server" Font-Bold="True" Width="302px">RBSE Number:  12/12/12345</asp:label>
				<DIV style="Z-INDEX: 101; LEFT: 320px; WIDTH: 242px; POSITION: absolute; TOP: 0px; HEIGHT: 27px" ms_positioning="GridLayout"><uc1:batchnumberdisplay id="BatchNumberDisplay1" runat="server"></uc1:batchnumberdisplay></DIV>
				<asp:button id="btnSave2" style="Z-INDEX: 102; LEFT: 592px; POSITION: absolute; TOP: 0px" runat="server" Text="  Save  "></asp:button><asp:button id="btnCancel2" style="Z-INDEX: 103; LEFT: 664px; POSITION: absolute; TOP: 0px" runat="server" Text=" Cancel " CausesValidation="False"></asp:button>
				<DIV style="Z-INDEX: 104; LEFT: 496px; WIDTH: 88px; POSITION: absolute; TOP: 0px; HEIGHT: 32px" ms_positioning="GridLayout"><asp:literal id="litViewDocs2" runat="server"></asp:literal></DIV>
			</DIV>
			<DIV style="WIDTH: 754px; POSITION: relative; HEIGHT: 32px" ms_positioning="GridLayout">&nbsp;
				<DIV class="TabEnd" style="Z-INDEX: 101; LEFT: 0px; WIDTH: 8px; POSITION: absolute; TOP: 0px; HEIGHT: 27px"></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 105; LEFT: 8px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbFarm" runat="server" CssClass="tablink">Farm</asp:linkbutton></DIV>
				<DIV class="SelectedTabTitle" style="Z-INDEX: 102; LEFT: 108px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px">Case&nbsp;(DEFRA)</DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 106; LEFT: 208px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbBAB" runat="server" CssClass="tablink">BAB</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 107; LEFT: 308px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbCaseVLA" runat="server" CssClass="tablink">Case (VLA)</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 104; LEFT: 408px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbClinical" runat="server" CssClass="tablink">Clinical</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 103; LEFT: 508px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbFeeds" runat="server" CssClass="tablink">Feeds</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 108; LEFT: 608px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbRelations" runat="server" CssClass="tablink">Relations</asp:linkbutton></DIV>
				<DIV class="TabEnd" style="Z-INDEX: 109; LEFT: 708px; WIDTH: 42px; POSITION: absolute; TOP: 0px; HEIGHT: 27px"></DIV>
			</DIV>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 189px" ms_positioning="GridLayout"><asp:label id="lblEartag" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Eartag</asp:label><asp:label id="lblPreviousEartag" style="Z-INDEX: 101; LEFT: 400px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Previous Eartag</asp:label><asp:label id="lblBSE1DateReceived" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 48px" runat="server" Width="152px">BSE1 Date Received</asp:label><asp:label id="lblFormADate" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 80px" runat="server" Width="152px">Form A Date</asp:label><asp:label id="lblFormAResubmittedDate" style="Z-INDEX: 104; LEFT: 400px; POSITION: absolute; TOP: 80px" runat="server" Width="152px">Form A Resubmitted Date</asp:label><asp:label id="lblFormBDate" style="Z-INDEX: 105; LEFT: 16px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">Form B Date</asp:label><asp:label id="lblFate" style="Z-INDEX: 106; LEFT: 400px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">Fate (Form B Reason)</asp:label><asp:label id="lblFormCDate" style="Z-INDEX: 107; LEFT: 16px; POSITION: absolute; TOP: 144px" runat="server" Width="152px">Form C Date</asp:label><asp:label id="lblrfvFate" style="Z-INDEX: 159; LEFT: 728px; POSITION: absolute; TOP: 112px" runat="server" CssClass="ValidatorText" ToolTip="You must enter a Form B Reason">*</asp:label>
				<HR style="Z-INDEX: 108; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 176px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<DIV style="Z-INDEX: 160; LEFT: 184px; WIDTH: 296px; POSITION: absolute; TOP: 16px; HEIGHT: 16px" ms_positioning="GridLayout"><uc1:ThreePartEartag id="ctlEartag" runat="server"></uc1:ThreePartEartag></DIV>
				<DIV style="Z-INDEX: 161; LEFT: 560px; WIDTH: 182px; POSITION: absolute; TOP: 16px; HEIGHT: 27px" ms_positioning="GridLayout"><asp:textbox id="txtPreviousEartag" runat="server"></asp:textbox></DIV>
				<DIV style="Z-INDEX: 157; LEFT: 184px; WIDTH: 296px; POSITION: absolute; TOP: 48px; HEIGHT: 16px" ms_positioning="GridLayout"><uc1:calendardate id="ctlBSE1DateReceived" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 154; LEFT: 184px; WIDTH: 296px; POSITION: absolute; TOP: 80px; HEIGHT: 16px" ms_positioning="GridLayout"><uc1:calendardate id="ctlFormADate" runat="server" AutoPostBack="True"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 156; LEFT: 560px; WIDTH: 182px; POSITION: absolute; TOP: 80px; HEIGHT: 32px" ms_positioning="GridLayout"><uc1:calendardate id="ctlFormAResubmittedDate" runat="server" AutoPostBack="True"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 152; LEFT: 184px; WIDTH: 296px; POSITION: absolute; TOP: 112px; HEIGHT: 16px" ms_positioning="GridLayout"><uc1:calendardate id="ctlFormBDate" runat="server" AutoPostBack="True"></uc1:calendardate></DIV>
				<asp:dropdownlist id="ddlFate" style="Z-INDEX: 138; LEFT: 560px; POSITION: absolute; TOP: 112px" runat="server" Width="168px" Font-Size="90%"></asp:dropdownlist>
				<DIV style="Z-INDEX: 151; LEFT: 184px; WIDTH: 296px; POSITION: absolute; TOP: 144px; HEIGHT: 45px" ms_positioning="GridLayout"><uc1:calendardate id="ctlFormCDate" runat="server" AutoPostBack="True"></uc1:calendardate><asp:label id="lblFormCDateWarning" style="Z-INDEX: 167; LEFT: 176px; POSITION: absolute; TOP: 0px" runat="server" CssClass="validatortext" ToolTip="Are you sure the Form C Date isn't the same as the Form B Date?" ForeColor="#9CCE00" Visible="False">*</asp:label></DIV>
				<asp:checkbox id="chkNonGBCase" style="Z-INDEX: 164; LEFT: 520px; POSITION: absolute; TOP: 144px" runat="server" Visible="False"></asp:checkbox>
				<DIV id="exitConfirmationDIV" style="Z-INDEX: 166; LEFT: 296px; WIDTH: 316px; POSITION: absolute; TOP: 8px; HEIGHT: 8px" ms_positioning="GridLayout"><uc1:exitconfirmation id="ctlExitConfirmation" runat="server"></uc1:exitconfirmation></DIV>
			</DIV>
			<asp:datagrid id="grdTests" runat="server" Width="740px" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True">
				<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
				<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
				<ItemStyle CssClass="GridItem"></ItemStyle>
				<HeaderStyle CssClass="GridHeader"></HeaderStyle>
				<Columns>
					<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
						<HeaderStyle Width="25px"></HeaderStyle>
					</asp:ButtonColumn>
					<asp:TemplateColumn SortExpression="TestTypeDescription" HeaderText="Test Type">
						<ItemTemplate>
							<asp:Label id="lblTestType" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.TestTypeDescription") %>'>
							</asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 100px; POSITION: relative" ms_positioning="GridLayout">
								<asp:DropDownList id="ddlTestType" runat="server" DataValueField="Code" DataTextField="Description" DataSource="<%# GetTestTypeList() %>">
								</asp:DropDownList>
							</DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:TemplateColumn SortExpression="TestResultDescription" HeaderText="Test Result">
						<ItemTemplate>
							<asp:Label id="lblTestResult" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.TestResultDescription") %>'>
							</asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 100px; POSITION: relative" ms_positioning="GridLayout">
								<asp:DropDownList id="ddlTestResult" runat="server" DataValueField="Code" DataTextField="Description" DataSource="<%# GetTestResultList() %>">
								</asp:DropDownList>
							</DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
				</Columns>
				<PagerStyle Visible="False"></PagerStyle>
			</asp:datagrid>
            
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 28px" ms_positioning="GridLayout">
				<P><uc1:datagridpager id="TestsPager" runat="server"></uc1:datagridpager></P>
			</DIV>
			<DIV style="WIDTH: 802px; POSITION: relative; HEIGHT: 475px; left: 1px; top: 0px;" ms_positioning="GridLayout"><asp:label id="lblDBSE" style="Z-INDEX: 167; LEFT: 399px; POSITION: absolute; TOP: 145px" runat="server" Width="152px">DBSE</asp:label><asp:label id="lblDBSEValue" style="Z-INDEX: 167; LEFT: 560px; POSITION: absolute; TOP: 145px" runat="server" Width="152px">DBSE Value</asp:label><asp:label id="lblReportedLocation" style="Z-INDEX: 167; LEFT: 399px; POSITION: absolute; TOP: 113px" runat="server" Width="152px">Reported Location</asp:label><asp:label id="lblNotes" style="Z-INDEX: 167; LEFT: 14px; POSITION: absolute; TOP: 272px" runat="server" Width="152px">Notes:</asp:label><asp:label id="lblSurvey" style="Z-INDEX: 167; LEFT: 399px; POSITION: absolute; TOP: 181px" runat="server" Width="152px">Survey</asp:label><asp:label id="lblDateOfBirth" style="Z-INDEX: 167; LEFT: 16px; POSITION: absolute; TOP: 113px" runat="server" Width="152px">Date of Birth</asp:label><asp:label id="lblValuationAge" style="Z-INDEX: 167; LEFT: 15px; POSITION: absolute; TOP: 214px" runat="server" Width="152px">Valuation Age</asp:label><asp:label id="lblDateOfBirthEstimated" style="Z-INDEX: 167; LEFT: 15px; POSITION: absolute; TOP: 181px" runat="server" Width="160px">Is Date Of Birth Estimated?</asp:label><asp:label id="lblPurchaserBSE1Received" style="Z-INDEX: 167; LEFT: 14px; POSITION: absolute; TOP: 376px" runat="server" Width="176px">Is Purchaser BSE1 Received?</asp:label><asp:label id="lblVendor1BSE1Received" style="Z-INDEX: 167; LEFT: 14px; POSITION: absolute; TOP: 408px" runat="server" Width="176px">Is Vendor 1 BSE1 Received?</asp:label><asp:label id="lblSummarySheetReceived" style="Z-INDEX: 167; LEFT: 14px; POSITION: absolute; TOP: 440px" runat="server" Width="176px">Is Summary Sheet Received?</asp:label><asp:label id="lblBreederBSE1Received" style="Z-INDEX: 167; LEFT: 398px; POSITION: absolute; TOP: 376px" runat="server" Width="176px">Is Breeder BSE1 Received?</asp:label><asp:label id="lblHomebredBSE1Received" style="Z-INDEX: 167; LEFT: 398px; POSITION: absolute; TOP: 408px" runat="server" Width="176px">Is Homebred BSE1 Received?</asp:label><asp:label id="lblPaperworkComplete" style="Z-INDEX: 167; LEFT: 398px; POSITION: absolute; TOP: 440px" runat="server" Width="176px">Is Paperwork Complete?</asp:label><asp:label id="lblDateOfBirthSource" style="Z-INDEX: 167; LEFT: 15px; POSITION: absolute; TOP: 149px" runat="server" Width="160px">Date Of Birth Source</asp:label>
				<HR style="Z-INDEX: 167; LEFT: 14px; WIDTH: 96.14%; POSITION: absolute; TOP: 360px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<DIV style="Z-INDEX: 167; LEFT: 184px; WIDTH: 216px; POSITION: absolute; TOP: 113px; HEIGHT: 45px" ms_positioning="GridLayout"><uc1:calendardate id="ctlDateOfBirth" runat="server"></uc1:calendardate><asp:label id="lblHerdEntryDateWarning" style="Z-INDEX: 167; LEFT: 176px; POSITION: absolute; TOP: 0px" runat="server" CssClass="validatortext" ToolTip="Are you sure the Herd Entry is less than 18 months after the Date of Birth" ForeColor="#9CCE00" Visible="False">*</asp:label></DIV>
				<asp:dropdownlist id="ddlBirthDateSource" style="Z-INDEX: 167; LEFT: 183px; POSITION: absolute; TOP: 149px" runat="server" Width="168px" Font-Size="90%"></asp:dropdownlist><asp:checkbox id="chkDateOfBirthEstimated" style="Z-INDEX: 167; LEFT: 183px; POSITION: absolute; TOP: 181px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:dropdownlist id="ddlReportedLocation" style="Z-INDEX: 167; LEFT: 560px; POSITION: absolute; TOP: 113px" runat="server" Width="168px" Font-Size="90%"></asp:dropdownlist><asp:dropdownlist id="ddlSurvey" style="Z-INDEX: 167; LEFT: 559px; POSITION: absolute; TOP: 181px" runat="server" Width="168px" Font-Size="90%"></asp:dropdownlist><asp:dropdownlist id="ddlValuationAge" style="Z-INDEX: 167; LEFT: 184px; POSITION: absolute; TOP: 211px" runat="server" Width="168px" Font-Size="90%"></asp:dropdownlist><asp:textbox id="txtNotes" style="Z-INDEX: 167; LEFT: 14px; POSITION: absolute; TOP: 296px" runat="server" Width="720px" MaxLength="500" Height="50px" TextMode="MultiLine"></asp:textbox><asp:checkbox id="chkPurchaserBSE1Received" style="Z-INDEX: 167; LEFT: 198px; POSITION: absolute; TOP: 376px" runat="server" Width="32px" Text=" "></asp:checkbox><asp:checkbox id="chkBreederBSE1Received" style="Z-INDEX: 167; LEFT: 582px; POSITION: absolute; TOP: 376px" runat="server" Width="40px" Text=" "></asp:checkbox><asp:checkbox id="chkVendor1BSE1Received" style="Z-INDEX: 167; LEFT: 198px; POSITION: absolute; TOP: 408px" runat="server" Width="24px" Text=" "></asp:checkbox><asp:checkbox id="chkHomebredBSE1Received" style="Z-INDEX: 167; LEFT: 582px; POSITION: absolute; TOP: 408px" runat="server" Width="32px" Text=" "></asp:checkbox><asp:checkbox id="chkSummarySheetReceived" style="Z-INDEX: 167; LEFT: 198px; POSITION: absolute; TOP: 440px" runat="server" Width="32px" Text=" "></asp:checkbox><asp:checkbox id="chkPaperworkComplete" style="Z-INDEX: 167; LEFT: 582px; POSITION: absolute; TOP: 440px" runat="server" Width="32px" Text=" "></asp:checkbox>
				
				<DIV style="Z-INDEX: 167; LEFT: 225px; WIDTH: 166px; POSITION: absolute; TOP: 374px; HEIGHT: 16px" ms_positioning="GridLayout"><uc1:calendardate id="ctlPurchaserBSE1ReceivedDate" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 167; LEFT: 611px; WIDTH: 166px; POSITION: absolute; TOP: 374px; HEIGHT: 16px" ms_positioning="GridLayout"><uc1:calendardate id="ctlBreederBSE1ReceivedDate" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 167; LEFT: 225px; WIDTH: 166px; POSITION: absolute; TOP: 407px; HEIGHT: 16px" ms_positioning="GridLayout"><uc1:calendardate id="ctlVendor1BSE1ReceivedDate" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 167; LEFT: 611px; WIDTH: 166px; POSITION: absolute; TOP: 407px; HEIGHT: 16px" ms_positioning="GridLayout"><uc1:calendardate id="ctlHomebredBSE1ReceivedDate" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 167; LEFT: 225px; WIDTH: 166px; POSITION: absolute; TOP: 440px; HEIGHT: 16px" ms_positioning="GridLayout"><uc1:calendardate id="ctlSummarySheetReceivedDate" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 167; LEFT: 611px; WIDTH: 166px; POSITION: absolute; TOP: 440px; HEIGHT: 16px" ms_positioning="GridLayout"><uc1:calendardate id="ctlPaperworkCompleteDate" runat="server"></uc1:calendardate></DIV>
				
				<HR style="Z-INDEX: 167; LEFT: 17px; WIDTH: 96.14%; POSITION: absolute; TOP: 93px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:label id="lblFinalResultDate" style="Z-INDEX: 167; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Final Result Date</asp:label><asp:label id="lblFinalResultDateValue" style="Z-INDEX: 167; LEFT: 184px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Final Result Date</asp:label><asp:label id="lblFinalResult" style="Z-INDEX: 167; LEFT: 400px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Final Result</asp:label><asp:label id="lblFinalResultValue" style="Z-INDEX: 167; LEFT: 560px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Final Result</asp:label>
                
                <asp:Label ID="lblCaseType" style="Z-INDEX: 167; LEFT: 16px; POSITION: absolute; TOP: 46px" runat="server" Text="Case Type"></asp:Label>
                <asp:Label ID="lblLabComment" style="Z-INDEX: 167; LEFT: 400px; POSITION: absolute; TOP: 46px" runat="server" Text="Lab Comment"></asp:Label>
                <asp:DropDownList ID="ddlCaseType" runat="server" style="Z-INDEX: 167; LEFT: 184px; POSITION: absolute; TOP: 47px" runat="server" Width="168px" Font-Size="90%"></asp:DropDownList>
                <asp:TextBox ID="txtLabComment" style="Z-INDEX: 167; LEFT: 559px; POSITION: absolute; TOP: 47px" runat="server" MaxLength="255" TextMode="MultiLine" Width="165px"></asp:TextBox>
                <asp:Label ID="lblBarcode" style="Z-INDEX: 167; LEFT: 400px; POSITION: absolute; TOP: 215px" runat="server" Text="Barcode"></asp:Label>
                <asp:Label ID="lblAHFReference" style="Z-INDEX: 167; LEFT: 400px; POSITION: absolute; TOP: 251px" runat="server" Text="AHFReference"></asp:Label>
                <asp:TextBox ID="txtBarcode" style="Z-INDEX: 167; LEFT: 560px; POSITION: absolute; TOP: 216px" runat="server" MaxLength="20"></asp:TextBox>
                <asp:TextBox ID="txtAHFReference" style="Z-INDEX: 167; LEFT: 560px; POSITION: absolute; TOP: 248px" runat="server" MaxLength="40"></asp:TextBox></DIV>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 54px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 16px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<asp:button id="btnCaseAuditLog" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 24px" runat="server" Text="Case Audit log"></asp:button><asp:button id="btnSave" style="Z-INDEX: 102; LEFT: 592px; POSITION: absolute; TOP: 24px" runat="server" Text="  Save  "></asp:button><asp:button id="btnCancel" style="Z-INDEX: 103; LEFT: 664px; POSITION: absolute; TOP: 24px" runat="server" Text=" Cancel " CausesValidation="False"></asp:button>
				<DIV style="Z-INDEX: 104; LEFT: 496px; WIDTH: 88px; POSITION: absolute; TOP: 24px; HEIGHT: 32px" ms_positioning="GridLayout"><asp:literal id="litViewDocs" runat="server"></asp:literal></DIV>
                <asp:Button ID="btnCaseWork" style="Z-INDEX: 104; LEFT: 148px; POSITION: absolute; TOP: 24px" runat="server" Text="Casework" /></DIV>
			<P><asp:HiddenField ID="hdnRBSEDate" runat="server" /></P>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</FORM>
	</BODY>
</HTML>
