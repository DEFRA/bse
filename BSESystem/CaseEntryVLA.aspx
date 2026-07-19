<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="BatchNumberDisplay" Src="BatchNumberDisplay.ascx" %>
<%@ Register TagPrefix="uc1" TagName="ExitConfirmation" Src="ExitConfirmation.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CaseEntryVLA.aspx.vb" Inherits="BSESystem.CaseEntryVLA" smartNavigation="true"%>
<%@ Register TagPrefix="uc1" TagName="PartialDate" Src="PartialDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CPHH" Src="CPHH.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Case Details</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="Z-INDEX: 400; WIDTH: 750px; POSITION: relative; HEIGHT: 40px" ms_positioning="GridLayout"><asp:label id="lblRBSEHeader" style="Z-INDEX: 100; LEFT: 8px; POSITION: absolute; TOP: 2px" runat="server" Width="302px" Font-Bold="True">RBSE Number:  12/12/12345</asp:label>
				<DIV style="Z-INDEX: 101; LEFT: 320px; WIDTH: 242px; POSITION: absolute; TOP: 0px; HEIGHT: 27px" ms_positioning="GridLayout"><uc1:batchnumberdisplay id="BatchNumberDisplay1" runat="server"></uc1:batchnumberdisplay></DIV>
				<asp:button id="btnSave2" style="Z-INDEX: 102; LEFT: 592px; POSITION: absolute; TOP: 0px" runat="server" Text="  Save  "></asp:button>
				<asp:button id="btnCancel2" style="Z-INDEX: 103; LEFT: 664px; POSITION: absolute; TOP: 0px" runat="server" Text=" Cancel " CausesValidation="False"></asp:button>
				<DIV style="Z-INDEX: 104; LEFT: 496px; WIDTH: 88px; POSITION: absolute; TOP: 0px; HEIGHT: 32px" ms_positioning="GridLayout">
					<asp:Literal id="litViewDocs2" runat="server"></asp:Literal></DIV>
			</DIV>
			<div style="WIDTH: 754px; POSITION: relative; HEIGHT: 32px" ms_positioning="GridLayout">&nbsp;
				<div class="TabEnd" style="Z-INDEX: 100; LEFT: 0px; WIDTH: 8px; POSITION: absolute; TOP: 0px; HEIGHT: 27px"></div>
				<div class="UnselectedTabTitle" style="Z-INDEX: 105; LEFT: 8px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbFarm" runat="server" CssClass="tablink">Farm</asp:linkbutton></div>
				<div class="UnselectedTabTitle" style="Z-INDEX: 105; LEFT: 108px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbCaseDEFRA" runat="server" CssClass="TABLINK">Case&nbsp;(DEFRA)</asp:linkbutton></div>
				<div class="UnselectedTabTitle" style="Z-INDEX: 106; LEFT: 208px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbBAB" runat="server" CssClass="tablink">BAB</asp:linkbutton></div>
				<div class="SelectedTabTitle" style="Z-INDEX: 102; LEFT: 308px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px">Case&nbsp;(VLA)</div>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 104; LEFT: 408px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbClinical" runat="server" CssClass="tablink">Clinical</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 103; LEFT: 508px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbFeeds" runat="server" CssClass="tablink">Feeds</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 109; LEFT: 608px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbRelations" runat="server" CssClass="tablink">Relations</asp:linkbutton></DIV>
			</div>
			<DIV style="Z-INDEX: 200; WIDTH: 750px; POSITION: relative; HEIGHT: 376px" ms_positioning="GridLayout">
				<DIV id="exitConfirmationDIV" style="Z-INDEX: 155; LEFT: 296px; WIDTH: 316px; POSITION: absolute; TOP: 8px; HEIGHT: 8px" ms_positioning="GridLayout">
					<uc1:ExitConfirmation id="ctlExitConfirmation" runat="server"></uc1:ExitConfirmation></DIV>
				<asp:label id="lblDateOfBirth" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Date Of Birth</asp:label>
				<asp:label id="lblSex" style="Z-INDEX: 102; LEFT: 400px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Sex</asp:label>
				<asp:label id="lblBreed" style="Z-INDEX: 103; LEFT: 400px; POSITION: absolute; TOP: 48px" runat="server" Width="152px">Breed</asp:label>
				<asp:label id="lblOrigin" style="Z-INDEX: 106; LEFT: 400px; POSITION: absolute; TOP: 80px" runat="server" Width="152px">Origin</asp:label>
				<asp:label id="lblDatePurchased" style="Z-INDEX: 109; LEFT: 16px; POSITION: absolute; TOP: 128px" runat="server" Width="152px">Date Purchased</asp:label>
				<asp:label id="lblAgePurchased" style="Z-INDEX: 110; LEFT: 400px; POSITION: absolute; TOP: 128px" runat="server" Width="150px">Age Purchased</asp:label>
				<asp:label id="lblPurchasedCounty" style="Z-INDEX: 111; LEFT: 16px; POSITION: absolute; TOP: 160px" runat="server" Width="152px">Purchased County</asp:label>
				<asp:label id="lblHerdEntryDate" style="Z-INDEX: 114; LEFT: 16px; POSITION: absolute; TOP: 208px" runat="server" Width="152px">Herd Entry Date</asp:label>
				<asp:label id="lblOnsetDate" style="Z-INDEX: 115; LEFT: 400px; POSITION: absolute; TOP: 208px" runat="server" Width="152px">Onset Date</asp:label>
				<asp:label id="lblMonthsPregnant" style="Z-INDEX: 116; LEFT: 400px; POSITION: absolute; TOP: 240px" runat="server" Width="152px">Months Pregnant</asp:label>
				<asp:label id="lblMonthsPostCalving" style="Z-INDEX: 117; LEFT: 16px; POSITION: absolute; TOP: 272px" runat="server" Width="152px">Months Post Calving</asp:label>
				<asp:label id="lblOnsetAge" style="Z-INDEX: 118; LEFT: 400px; POSITION: absolute; TOP: 272px" runat="server" Width="152px">Onset Age</asp:label>
				<asp:label id="lblSlaughterDate" style="Z-INDEX: 119; LEFT: 16px; POSITION: absolute; TOP: 304px" runat="server" Width="152px">Slaughter Date</asp:label>
				<asp:label id="lblDateOfBirthEstimated" style="Z-INDEX: 123; LEFT: 16px; POSITION: absolute; TOP: 80px" runat="server" Width="152px">Date Of Birth Estimated?</asp:label>
				<asp:label id="lblOnsetDateEstimated" style="Z-INDEX: 124; LEFT: 16px; POSITION: absolute; TOP: 240px" runat="server" Width="152px">Onset Date Estimated?</asp:label>
				<asp:label id="lblAgePurchasedYears" style="Z-INDEX: 128; LEFT: 400px; POSITION: absolute; TOP: 160px" runat="server" Width="40px">Years:</asp:label>
				<asp:label id="lblAgePurchasedMonths" style="Z-INDEX: 129; LEFT: 528px; POSITION: absolute; TOP: 160px" runat="server" Width="40px">Months:</asp:label>
				<asp:label id="lblOnsetAgeYears" style="Z-INDEX: 135; LEFT: 400px; POSITION: absolute; TOP: 304px" runat="server" Width="40px">Years:</asp:label>
				<asp:label id="lblOnsetAgeMonths" style="Z-INDEX: 138; LEFT: 528px; POSITION: absolute; TOP: 304px" runat="server" Width="40px">Months:</asp:label>
				<asp:Label id="lblOnsetAgeIncorrect" style="Z-INDEX: 142; LEFT: 648px; POSITION: absolute; TOP: 304px" runat="server" CssClass="validatortext" ForeColor="#9CCE00" ToolTip="Are you sure that the onset age is less than 18 months?" Visible="False">*</asp:Label>
				<asp:Label id="lblPregnantError" style="Z-INDEX: 143; LEFT: 368px; POSITION: absolute; TOP: 272px" runat="server" CssClass="validatortext" ToolTip="You cannot enter a value for Month's Post Calving and Months Pregnant" ForeColor="Red" Visible="False">*</asp:Label>
				<asp:Label id="lblOnsetDateWarning" style="Z-INDEX: 146; LEFT: 544px; POSITION: absolute; TOP: 200px" runat="server" CssClass="validatortext" Visible="False" ToolTip="Are you sure that the onset date was more than 3 months before the Form A Date?" ForeColor="#9CCE00">*</asp:Label>
				<asp:Label id="lblPreviousOwners" style="Z-INDEX: 147; LEFT: 16px; POSITION: absolute; TOP: 352px" runat="server" Font-Bold="True" Width="328px">Previous Owners</asp:Label>
				<asp:label id="lblDateOfBirthSource" style="Z-INDEX: 148; LEFT: 16px; POSITION: absolute; TOP: 48px" runat="server" Width="152px">Date Of Birth Source</asp:label>
				<HR style="Z-INDEX: 108; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 112px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<HR style="Z-INDEX: 113; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 192px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<HR style="Z-INDEX: 125; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 336px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:regularexpressionvalidator id="revMonthsPostCalving" style="Z-INDEX: 126; LEFT: 352px; POSITION: absolute; TOP: 272px" runat="server" CssClass="ValidatorText" ErrorMessage="*" ToolTip="This must be a value between 1 and 3" ControlToValidate="txtMonthsPostCalving" ValidationExpression="[1-3]?"></asp:regularexpressionvalidator>
				<asp:regularexpressionvalidator id="revMonthsPregnant" style="Z-INDEX: 127; LEFT: 728px; POSITION: absolute; TOP: 240px" runat="server" CssClass="ValidatorText" ErrorMessage="*" ToolTip="This must be a value between 1 and 9" ControlToValidate="txtMonthsPregnant" ValidationExpression="[1-9]?"></asp:regularexpressionvalidator>
				<asp:regularexpressionvalidator id="revAgePurchasedYears" style="Z-INDEX: 132; LEFT: 512px; POSITION: absolute; TOP: 160px" runat="server" CssClass="ValidatorText" ErrorMessage="*" ToolTip="This must be an integer" ControlToValidate="txtAgePurchasedYears" ValidationExpression="[0-9]?[0-9]?"></asp:regularexpressionvalidator>
				<asp:regularexpressionvalidator id="revAgePurchasedMonths" style="Z-INDEX: 133; LEFT: 640px; POSITION: absolute; TOP: 160px" runat="server" CssClass="ValidatorText" ErrorMessage="*" ToolTip="This must be an integer" ControlToValidate="txtAgePurchasedMonths" ValidationExpression="[0-9]?[0-9]?[0-9]?[0-9]?"></asp:regularexpressionvalidator>
				<asp:regularexpressionvalidator id="revOnsetAgeYears" style="Z-INDEX: 137; LEFT: 512px; POSITION: absolute; TOP: 304px" runat="server" CssClass="ValidatorText" ErrorMessage="*" ToolTip="This must be an integer" ControlToValidate="txtOnsetAgeYears" ValidationExpression="[0-9]?[0-9]?"></asp:regularexpressionvalidator>
				<asp:regularexpressionvalidator id="revOnsetAgeMonths" style="Z-INDEX: 140; LEFT: 632px; POSITION: absolute; TOP: 304px" runat="server" CssClass="ValidatorText" ErrorMessage="*" ToolTip="This must be an integer" ControlToValidate="txtOnsetAgeMonths" ValidationExpression="[0-9]?[0-9]?[0-9]?[0-9]?"></asp:regularexpressionvalidator>
				<DIV style="Z-INDEX: 154; LEFT: 184px; WIDTH: 216px; POSITION: absolute; TOP: 8px; HEIGHT: 32px" ms_positioning="GridLayout">
					<uc1:calendardate id="ctlDateOfBirth" runat="server"></uc1:calendardate>
				</DIV>
				<asp:DropDownList id="ddlBirthDateSource" style="Z-INDEX: 149; LEFT: 184px; POSITION: absolute; TOP: 48px" runat="server" Width="168px" Font-Size="90%"></asp:DropDownList>
				<asp:checkbox id="chkDateOfBirthEstimated" style="Z-INDEX: 101; LEFT: 184px; POSITION: absolute; TOP: 80px" runat="server" Width="66px" Height="8px" Text=" "></asp:checkbox>
				<asp:dropdownlist id="ddlSex" style="Z-INDEX: 104; LEFT: 560px; POSITION: absolute; TOP: 16px" runat="server" Width="168px" Font-Size="90%" AutoPostBack="True"></asp:dropdownlist>
				<asp:dropdownlist id="ddlBreed" style="Z-INDEX: 105; LEFT: 560px; POSITION: absolute; TOP: 48px" runat="server" Width="168px" Font-Size="90%"></asp:dropdownlist>
				<asp:dropdownlist id="ddlOrigin" style="Z-INDEX: 107; LEFT: 560px; POSITION: absolute; TOP: 80px" runat="server" Width="168px" Font-Size="90%" AutoPostBack="True"></asp:dropdownlist>
				<DIV style="Z-INDEX: 153; LEFT: 184px; WIDTH: 216px; POSITION: absolute; TOP: 128px; HEIGHT: 32px" ms_positioning="GridLayout">
					<uc1:calendardate id="ctlDatePurchased" runat="server"></uc1:calendardate>
				</DIV>
				<asp:button id="btnCalculateAgePurchased" style="Z-INDEX: 131; LEFT: 560px; POSITION: absolute; TOP: 128px" runat="server" Text="Calculate" ToolTip="Click to calculate the Age Purchased in months"></asp:button>
				<asp:dropdownlist id="ddlPurchasedCounty" style="Z-INDEX: 112; LEFT: 184px; POSITION: absolute; TOP: 160px" runat="server" Width="168px" Font-Size="90%"></asp:dropdownlist>
				<asp:TextBox id="txtAgePurchasedYears" style="Z-INDEX: 144; LEFT: 448px; POSITION: absolute; TOP: 160px" runat="server" Width="54px" MaxLength="2"></asp:TextBox>
				<asp:textbox id="txtAgePurchasedMonths" style="Z-INDEX: 130; LEFT: 584px; POSITION: absolute; TOP: 160px" runat="server" Width="54px" MaxLength="4"></asp:textbox>
				<DIV style="Z-INDEX: 152; LEFT: 184px; WIDTH: 216px; POSITION: absolute; TOP: 208px; HEIGHT: 45px" ms_positioning="GridLayout">
					<uc1:calendardate id="ctlHerdEntryDate" runat="server"></uc1:calendardate>
					<asp:Label id="lblHerdDateWarning" style="Z-INDEX: 156; LEFT: -16px; POSITION: absolute; TOP: 0px" runat="server" CssClass="validatortext" Visible="False" ToolTip="Are you usre the Herd Entry is less than 18 months after the Date of Birth" ForeColor="#9CCE00">*</asp:Label>
				</DIV>
				<DIV style="Z-INDEX: 141; LEFT: 560px; WIDTH: 176px; POSITION: absolute; TOP: 200px; HEIGHT: 32px" ms_positioning="GridLayout">
					<uc1:calendardate id="ctlOnsetDate" runat="server"></uc1:calendardate>
				</DIV>
				<asp:checkbox id="chkOnsetDateEstimated" style="Z-INDEX: 122; LEFT: 184px; POSITION: absolute; TOP: 240px" runat="server" Width="88px" Text=" "></asp:checkbox>
				<asp:textbox id="txtMonthsPregnant" style="Z-INDEX: 120; LEFT: 560px; POSITION: absolute; TOP: 240px" runat="server" Width="168px" MaxLength="1" ToolTip="This must be a value between 1 and 9"></asp:textbox>
				<asp:textbox id="txtMonthsPostCalving" style="Z-INDEX: 121; LEFT: 184px; POSITION: absolute; TOP: 272px" runat="server" Width="168px" MaxLength="1" ToolTip="This must be a value between 1 and 3"></asp:textbox>
				<asp:button id="btnCalculateOnsetAge" style="Z-INDEX: 134; LEFT: 560px; POSITION: absolute; TOP: 272px" runat="server" Text="Calculate" ToolTip="Click to calculate the Onset Age in months"></asp:button>
				<DIV style="Z-INDEX: 151; LEFT: 184px; WIDTH: 216px; POSITION: absolute; TOP: 304px; HEIGHT: 32px" ms_positioning="GridLayout">
					<uc1:calendardate id="ctlSlaughterDate" runat="server"></uc1:calendardate>
				</DIV>
				<asp:textbox id="txtOnsetAgeYears" style="Z-INDEX: 136; LEFT: 456px; POSITION: absolute; TOP: 304px" runat="server" Width="54px" MaxLength="2"></asp:textbox>
				<asp:textbox id="txtOnsetAgeMonths" style="Z-INDEX: 139; LEFT: 576px; POSITION: absolute; TOP: 304px" runat="server" Width="54px" MaxLength="4" AutoPostBack="True"></asp:textbox>
				<asp:Label id="lblInformDEFRA" style="Z-INDEX: 150; LEFT: 368px; POSITION: absolute; TOP: 8px" runat="server" CssClass="validatortext" Visible="False" ToolTip="DEFRA may need informing if you change the Date Of Birth" ForeColor="#9CCE00">*</asp:Label>
			</DIV>
			<P></P>
			<asp:datagrid id="grdOtherOwners" runat="server" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True">
				<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
				<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
				<ItemStyle CssClass="GridItem"></ItemStyle>
				<HeaderStyle CssClass="GridHeader"></HeaderStyle>
				<Columns>
					<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
						<HeaderStyle Width="25px"></HeaderStyle>
					</asp:ButtonColumn>
					<asp:TemplateColumn SortExpression="Type" HeaderText="Owner Type">
						<HeaderStyle Width="110px"></HeaderStyle>
						<ItemTemplate>
							<asp:Label id="lblOwnerType" runat="server"></asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 100px; POSITION: relative" ms_positioning="GridLayout">
								<asp:DropDownList id=ddlOwnerType runat="server" DataValueField="Code" DataTextField="Description" DataSource="<%# GetOwnerTypeList() %>">
								</asp:DropDownList>
								<DIV style="LEFT: 92px; POSITION: absolute; TOP: -4px">
									<asp:Label id="lblPreviousError" runat="server" CssClass="validatortext" ToolTip="You can only have one Owner of type Previous" ForeColor="Red" Height="23px" Visible="False">*</asp:Label>
								</DIV>
							</DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:TemplateColumn HeaderText="Name">
						<HeaderStyle Width="380px"></HeaderStyle>
						<ItemTemplate>
							<asp:Label id=lblOtherOwnerNameDisplay runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.Name") %>'>
							</asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 370px; POSITION: relative" ms_positioning="GridLayout">
								<asp:TextBox id=txtOtherOwnerNameEdit runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.Name") %>' Width="355">
								</asp:TextBox>
								<DIV style="LEFT: 360px; POSITION: absolute; TOP: -4px">
									<asp:Label id="lblOwnerError" runat="server" CssClass="validatortext" ToolTip="You must enter either an Owner Name or a CPHH" ForeColor="Red" Height="23px" Visible="False">*</asp:Label>
								</DIV>
							</DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:TemplateColumn SortExpression="CPHH" HeaderText="CPHH">
						<HeaderStyle Width="205px"></HeaderStyle>
						<ItemTemplate>
							<asp:Label id="lblCPHH" runat="server"></asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 202px; POSITION: relative; HEIGHT: 31px" ms_positioning="GridLayout">
								<uc1:CPHH id="ctlCPHH" runat="server"></uc1:CPHH></DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
				</Columns>
				<PagerStyle Visible="False"></PagerStyle>
			</asp:datagrid>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 28px" ms_positioning="GridLayout">
				<P><uc1:datagridpager id="OtherOwnersPager" runat="server"></uc1:datagridpager></P>
			</DIV>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 48px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 16px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<asp:button id="btnCaseAuditLog" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 24px" runat="server" Text="Case Audit log"></asp:button>
				<asp:button id="btnSave" style="Z-INDEX: 102; LEFT: 592px; POSITION: absolute; TOP: 24px" runat="server" Text="  Save  "></asp:button>
				<asp:button id="btnCancel" style="Z-INDEX: 103; LEFT: 664px; POSITION: absolute; TOP: 24px" runat="server" Text=" Cancel " CausesValidation="False"></asp:button>
				<DIV style="Z-INDEX: 104; LEFT: 496px; WIDTH: 88px; POSITION: absolute; TOP: 24px; HEIGHT: 32px" ms_positioning="GridLayout">
					<asp:Literal id="litViewDocs" runat="server"></asp:Literal></DIV>
			</DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</form>
	</body>
</HTML>
