<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CaseEntryClinical.aspx.vb" Inherits="BSESystem.CaseEntryClinical" smartNavigation="true"%>
<%@ Register TagPrefix="uc1" TagName="ExitConfirmation" Src="ExitConfirmation.ascx" %>
<%@ Register TagPrefix="uc1" TagName="BatchNumberDisplay" Src="BatchNumberDisplay.ascx" %>
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
				<div class="TabEnd" style="Z-INDEX: 101; LEFT: 0px; WIDTH: 8px; POSITION: absolute; TOP: 0px; HEIGHT: 27px"></div>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 104; LEFT: 8px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbFarm" runat="server" CssClass="tablink">Farm</asp:linkbutton></DIV>
				<div class="UnselectedTabTitle" style="Z-INDEX: 105; LEFT: 108px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbCaseDEFRA" runat="server" CssClass="TABLINK">Case&nbsp;(DEFRA)</asp:linkbutton></div>
				<div class="UnselectedTabTitle" style="Z-INDEX: 106; LEFT: 208px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbBAB" runat="server" CssClass="tablink">BAB</asp:linkbutton></div>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 107; LEFT: 308px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbCaseVLA" runat="server" CssClass="tablink">Case (VLA)</asp:linkbutton></DIV>
				<div class="SelectedTabTitle" style="Z-INDEX: 102; LEFT: 408px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px">Clinical</div>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 103; LEFT: 508px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbFeeds" runat="server" CssClass="tablink">Feeds</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 108; LEFT: 608px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbRelations" runat="server" CssClass="tablink">Relations</asp:linkbutton></DIV>
				<DIV class="TabEnd" style="Z-INDEX: 109; LEFT: 708px; WIDTH: 42px; POSITION: absolute; TOP: 0px; HEIGHT: 27px"></DIV>
			</div>
			<DIV style="Z-INDEX: 200; WIDTH: 761px; POSITION: relative; HEIGHT: 554px" ms_positioning="GridLayout"><asp:label id="lblApprehension" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Apprehension</asp:label><asp:label id="lblAbnormalBehaviour" style="Z-INDEX: 103; LEFT: 400px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Abnormal Behaviour</asp:label><asp:label id="lblHypersensitiveTouch" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 48px" runat="server" Width="152px">Hypersensitive Touch</asp:label><asp:label id="lblHypersensitiveSound" style="Z-INDEX: 105; LEFT: 16px; POSITION: absolute; TOP: 80px" runat="server" Width="152px">Hypersensitive Sound</asp:label><asp:label id="lblManiacal" style="Z-INDEX: 106; LEFT: 16px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">Maniacal</asp:label><asp:label id="lblPanicStricken" style="Z-INDEX: 107; LEFT: 16px; POSITION: absolute; TOP: 144px" runat="server" Width="152px">Panic Stricken</asp:label><asp:label id="lblTemperamentChange" style="Z-INDEX: 108; LEFT: 16px; POSITION: absolute; TOP: 176px" runat="server" Width="152px">Temperament Change</asp:label><asp:label id="lblAbnormalHeadCarriage" style="Z-INDEX: 109; LEFT: 16px; POSITION: absolute; TOP: 208px" runat="server" Width="152px">Abnormal Head Carriage</asp:label><asp:label id="lblEarTwitching" style="Z-INDEX: 110; LEFT: 16px; POSITION: absolute; TOP: 240px" runat="server" Width="152px">Ear Twitching</asp:label><asp:label id="lblEarsOddAngle" style="Z-INDEX: 111; LEFT: 16px; POSITION: absolute; TOP: 272px" runat="server" Width="152px">Ears Odd Angle</asp:label><asp:label id="lblHeadShyness" style="Z-INDEX: 112; LEFT: 400px; POSITION: absolute; TOP: 48px" runat="server" Width="152px">Head Shyness</asp:label><asp:label id="lblLickingFlank" style="Z-INDEX: 113; LEFT: 400px; POSITION: absolute; TOP: 80px" runat="server" Width="152px">Licking Flank</asp:label><asp:label id="lblLickingNose" style="Z-INDEX: 114; LEFT: 400px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">Licking Nose</asp:label><asp:label id="lblKicking" style="Z-INDEX: 115; LEFT: 400px; POSITION: absolute; TOP: 144px" runat="server" Width="152px">Kicking</asp:label><asp:label id="lblReluctantToEnterDoorways" style="Z-INDEX: 116; LEFT: 400px; POSITION: absolute; TOP: 176px" runat="server" Width="169px">Reluctant to enter doorways</asp:label><asp:label id="lblHeadPressing" style="Z-INDEX: 117; LEFT: 400px; POSITION: absolute; TOP: 208px" runat="server" Width="152px">Head Pressing</asp:label><asp:label id="lblHeadRubbing" style="Z-INDEX: 118; LEFT: 400px; POSITION: absolute; TOP: 240px" runat="server" Width="152px">Head Rubbing</asp:label><asp:label id="lblTeethGrinding" style="Z-INDEX: 119; LEFT: 400px; POSITION: absolute; TOP: 272px" runat="server" Width="152px">Teeth Grinding</asp:label><asp:label id="lblBlindness" style="Z-INDEX: 138; LEFT: 16px; POSITION: absolute; TOP: 320px" runat="server" Width="152px"> Blindness</asp:label><asp:label id="lblFalling" style="Z-INDEX: 142; LEFT: 280px; POSITION: absolute; TOP: 320px" runat="server" Width="152px"> Falling</asp:label><asp:label id="lblRecumbent" style="Z-INDEX: 143; LEFT: 544px; POSITION: absolute; TOP: 320px" runat="server" Width="152px"> Recumbent</asp:label><asp:label id="lblCircling" style="Z-INDEX: 144; LEFT: 16px; POSITION: absolute; TOP: 352px" runat="server" Width="152px">Circling</asp:label><asp:label id="lblHindAtaxia" style="Z-INDEX: 145; LEFT: 16px; POSITION: absolute; TOP: 384px" runat="server" Width="152px">Hind Ataxia</asp:label><asp:label id="lblParesis" style="Z-INDEX: 146; LEFT: 280px; POSITION: absolute; TOP: 352px" runat="server" Width="152px">Paresis</asp:label><asp:label id="lblForeAtaxia" style="Z-INDEX: 147; LEFT: 280px; POSITION: absolute; TOP: 384px" runat="server" Width="152px">Fore Ataxia</asp:label><asp:label id="lblTremor" style="Z-INDEX: 148; LEFT: 544px; POSITION: absolute; TOP: 352px" runat="server" Width="152px">Tremor</asp:label><asp:label id="lblKnucklingFetlock" style="Z-INDEX: 149; LEFT: 544px; POSITION: absolute; TOP: 384px" runat="server" Width="152px">Knuckling Fetlock</asp:label><asp:label id="lblWeightLoss" style="Z-INDEX: 157; LEFT: 16px; POSITION: absolute; TOP: 424px" runat="server" Width="152px">Weight Loss</asp:label><asp:label id="lblConditionLoss" style="Z-INDEX: 159; LEFT: 16px; POSITION: absolute; TOP: 456px" runat="server" Width="152px">Condition Loss</asp:label><asp:label id="lblMilkYield" style="Z-INDEX: 160; LEFT: 16px; POSITION: absolute; TOP: 488px" runat="server" Width="152px">Milk Yield Loss</asp:label>
				<HR style="Z-INDEX: 163; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 512px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<HR style="Z-INDEX: 137; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 304px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<HR style="Z-INDEX: 156; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 408px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:checkbox id="chkApprehension" style="Z-INDEX: 102; LEFT: 184px; POSITION: absolute; TOP: 16px" runat="server" Width="112px" Text=" "></asp:checkbox>
				<asp:checkbox id="chkHypersensitiveTouch" style="Z-INDEX: 120; LEFT: 184px; POSITION: absolute; TOP: 48px" runat="server" Width="112px" Text=" "></asp:checkbox>
				<asp:checkbox id="chkHypersensitiveSound" style="Z-INDEX: 121; LEFT: 184px; POSITION: absolute; TOP: 80px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkManiacal" style="Z-INDEX: 122; LEFT: 184px; POSITION: absolute; TOP: 112px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkPanicStricken" style="Z-INDEX: 123; LEFT: 184px; POSITION: absolute; TOP: 144px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkTemperamentChange" style="Z-INDEX: 124; LEFT: 184px; POSITION: absolute; TOP: 176px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkAbnormalHeadCarriage" style="Z-INDEX: 125; LEFT: 184px; POSITION: absolute; TOP: 208px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkEarTwitching" style="Z-INDEX: 126; LEFT: 184px; POSITION: absolute; TOP: 240px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkEarsOddAngle" style="Z-INDEX: 127; LEFT: 184px; POSITION: absolute; TOP: 272px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkAbnormalBehaviour" style="Z-INDEX: 130; LEFT: 568px; POSITION: absolute; TOP: 16px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkHeadShyness" style="Z-INDEX: 128; LEFT: 568px; POSITION: absolute; TOP: 48px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkLickingFlank" style="Z-INDEX: 129; LEFT: 568px; POSITION: absolute; TOP: 80px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkLickingNose" style="Z-INDEX: 131; LEFT: 568px; POSITION: absolute; TOP: 112px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkKicking" style="Z-INDEX: 132; LEFT: 568px; POSITION: absolute; TOP: 144px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkReluctantToEnterDoorways" style="Z-INDEX: 133; LEFT: 568px; POSITION: absolute; TOP: 176px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkHeadPressing" style="Z-INDEX: 134; LEFT: 568px; POSITION: absolute; TOP: 208px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkHeadRubbing" style="Z-INDEX: 135; LEFT: 568px; POSITION: absolute; TOP: 240px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkTeethGrinding" style="Z-INDEX: 136; LEFT: 568px; POSITION: absolute; TOP: 272px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkBlindness" style="Z-INDEX: 139; LEFT: 184px; POSITION: absolute; TOP: 320px" runat="server" Width="32px" Text=" "></asp:checkbox><asp:checkbox id="chkCircling" style="Z-INDEX: 150; LEFT: 184px; POSITION: absolute; TOP: 352px" runat="server" Width="32px" Text=" "></asp:checkbox><asp:checkbox id="chkHindAtaxia" style="Z-INDEX: 151; LEFT: 184px; POSITION: absolute; TOP: 384px" runat="server" Width="32px" Text=" "></asp:checkbox><asp:checkbox id="chkFalling" style="Z-INDEX: 140; LEFT: 448px; POSITION: absolute; TOP: 320px" runat="server" Width="32px" Text=" "></asp:checkbox><asp:checkbox id="chkParesis" style="Z-INDEX: 152; LEFT: 448px; POSITION: absolute; TOP: 352px" runat="server" Width="32px" Text=" "></asp:checkbox><asp:checkbox id="chkForeAtaxia" style="Z-INDEX: 153; LEFT: 448px; POSITION: absolute; TOP: 384px" runat="server" Width="32px" Text=" "></asp:checkbox><asp:checkbox id="chkRecumbent" style="Z-INDEX: 141; LEFT: 712px; POSITION: absolute; TOP: 320px" runat="server" Width="32px" Text=" "></asp:checkbox><asp:checkbox id="chkTremor" style="Z-INDEX: 154; LEFT: 712px; POSITION: absolute; TOP: 352px" runat="server" Width="32px" Text=" "></asp:checkbox><asp:checkbox id="chkKnucklingFetlock" style="Z-INDEX: 155; LEFT: 712px; POSITION: absolute; TOP: 384px" runat="server" Width="32px" Text=" "></asp:checkbox><asp:checkbox id="chkWeightLoss" style="Z-INDEX: 158; LEFT: 184px; POSITION: absolute; TOP: 424px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkConditionLoss" style="Z-INDEX: 161; LEFT: 184px; POSITION: absolute; TOP: 456px" runat="server" Width="112px" Text=" "></asp:checkbox><asp:checkbox id="chkMilkYield" style="Z-INDEX: 162; LEFT: 184px; POSITION: absolute; TOP: 488px" runat="server" Width="112px" Text=" "></asp:checkbox>
				<asp:Label id="lblClinicalVisitDates" runat="server" style="Z-INDEX: 164; LEFT: 16px; POSITION: absolute; TOP: 528px" Font-Bold="True" Width="296px">Clinical Visit Dates</asp:Label>
				<DIV id="exitConfirmationDIV" style="Z-INDEX: 167; LEFT: 296px; WIDTH: 316px; POSITION: absolute; TOP: 8px; HEIGHT: 8px" ms_positioning="GridLayout">
					<uc1:ExitConfirmation id="ctlExitConfirmation" runat="server"></uc1:ExitConfirmation></DIV>
			</DIV>
			<asp:datagrid id="grdClinicalVisit" runat="server" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True">
				<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
				<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
				<ItemStyle CssClass="GridItem"></ItemStyle>
				<HeaderStyle CssClass="GridHeader"></HeaderStyle>
				<Columns>
					<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
						<HeaderStyle Width="20px"></HeaderStyle>
					</asp:ButtonColumn>
					<asp:TemplateColumn SortExpression="VisitDate" HeaderText="Visit Date">
						<ItemTemplate>
							<asp:Label id="lblVisitDateDisplay" runat="server"></asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="Z-INDEX: 700; WIDTH: 186px; POSITION: relative; HEIGHT: 26px" ms_positioning="GridLayout">
								<uc1:CalendarDate id="ctlVisitDateEdit" runat="server"></uc1:CalendarDate>
								<DIV style="LEFT: 170px; POSITION: absolute; TOP: -4px">
									<asp:Label id="lblVisitDateError" runat="server" CssClass="validatortext" Visible="False" ForeColor="Red" ToolTip="You have entered an invalid date or the same date twice.  The visit date must be after the birth date and before today.">*</asp:Label></DIV>
							</DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
				</Columns>
				<PagerStyle Visible="False"></PagerStyle>
			</asp:datagrid>
			<DIV style="WIDTH: 426px; POSITION: relative; HEIGHT: 27px" ms_positioning="GridLayout"><uc1:datagridpager id="ClinicalVisitPager" runat="server"></uc1:datagridpager></DIV>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 48px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 16px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<asp:button id="btnSave" style="Z-INDEX: 102; LEFT: 592px; POSITION: absolute; TOP: 24px" runat="server" Text="  Save  "></asp:button><asp:button id="btnCancel" style="Z-INDEX: 103; LEFT: 664px; POSITION: absolute; TOP: 24px" runat="server" Text=" Cancel " CausesValidation="False"></asp:button>
				<DIV style="Z-INDEX: 104; LEFT: 496px; WIDTH: 88px; POSITION: absolute; TOP: 24px; HEIGHT: 32px" ms_positioning="GridLayout">
					<asp:Literal id="litViewDocs" runat="server"></asp:Literal></DIV>
			</DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</form>
	</body>
</HTML>
