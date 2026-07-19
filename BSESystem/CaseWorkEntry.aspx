<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="CaseWorkEntry.aspx.vb"
    Inherits="BSESystem.CaseWorkEntry" %>

<%@ Register TagPrefix="asp" Namespace="AjaxControlToolkit" Assembly="AjaxControlToolkit" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="asp" Namespace="AjaxControlToolkit" Assembly="AjaxControlToolkit" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>BSE System : Casework Entry</title>
    <link href="Style/vla-ie.css" type="text/css" rel="stylesheet" />
</head>
<body>
    <form id="Form1" runat="server">
    <p>
        <uc1:VLAHeader ID="VLAHeader1" runat="server"></uc1:VLAHeader>
    </p>
    <div style="width: 978px; position: relative; height: 30px; left: 0px; top: 0px;">
        <asp:Button ID="btnSaveTop" runat="server" Text="Save" Style="z-index: 100; left: 833px;
            position: absolute; top: 0px" Width="58px" />
        <asp:Button ID="btnCancelTop" runat="server" Text="Cancel" Style="z-index: 100; left: 901px;
            position: absolute; top: 0px" Width="58px" />
        <hr size="1" style="z-index: 101; left: 16px; width: 96.03%; position: absolute;
            top: 28px; height: 1px" width="96.03%" />
    </div>
    <div style="width: 978px; position: relative; height: 769px; left: 0px; top: 0px;">
        <asp:ScriptManager ID="Scriptmanager1" runat="server" EnablePageMethods="true">
        </asp:ScriptManager>
        <!--Column 1-->
        <asp:Label ID="lblRBSE" runat="server" Text="RBSE" Style="z-index: 100; left: 16px;
            position: absolute; top: 55px" Width="135px"></asp:Label>
        <asp:Label ID="lblStatus" runat="server" Style="z-index: 100; left: 16px; position: absolute;
            top: 27px" Text="Status" Width="135px"></asp:Label>
        <asp:Label ID="lblStatusValue" runat="server" Style="z-index: 100; left: 159px; position: absolute;
            top: 27px" Width="135px"></asp:Label>
        <asp:LinkButton ID="lblRBSEValue" runat="server" Style="z-index: 100; left: 159px;
            position: absolute; top: 55px" Width="152px"></asp:LinkButton>
        <asp:Label ID="lblRBSEDate" runat="server" Text="RBSE Date" Style="z-index: 100;
            left: 16px; position: absolute; top: 80px" Width="135px"></asp:Label>
        <asp:Label ID="lblRBSEDateValue" runat="server" Style="z-index: 100; left: 159px;
            position: absolute; top: 80px" Width="152px"></asp:Label>
        <asp:Label ID="lblFormADate" runat="server" Text="Form A Date" Style="z-index: 100;
            left: 16px; position: absolute; top: 105px" Width="135px"></asp:Label>
        <asp:Label ID="lblFormADateValue" runat="server" Style="z-index: 100; left: 159px;
            position: absolute; top: 105px" Width="152px"></asp:Label>
        <asp:Label ID="lblFormBDate" runat="server" Text="Form B Date" Style="z-index: 100;
            left: 16px; position: absolute; top: 130px" Width="135px"></asp:Label>
        <asp:Label ID="lblFormBDateValue" runat="server" Style="z-index: 100; left: 159px;
            position: absolute; top: 130px" Width="152px"></asp:Label>
        <asp:Label ID="lblSlaughterDate" runat="server" Text="Slaughter Date" Style="z-index: 100;
            left: 16px; position: absolute; top: 155px" Width="135px"></asp:Label>
        <asp:Label ID="lblSlaughterDateValue" runat="server" Style="z-index: 100; left: 159px;
            position: absolute; top: 155px" Width="152px"></asp:Label>
        <asp:Label ID="lblFate" runat="server" Text="Fate" Style="z-index: 100; left: 16px;
            position: absolute; top: 180px" Width="135px"></asp:Label>
        <asp:Label ID="lblFateValue" runat="server" Style="z-index: 100; left: 159px; position: absolute;
            top: 180px" Width="152px"></asp:Label>
        <asp:Label ID="lblOrigin" runat="server" Text="Origin" Style="z-index: 100; left: 16px;
            position: absolute; top: 205px" Width="135px"></asp:Label>
        <asp:Label ID="lblOriginValue" runat="server" Style="z-index: 100; left: 159px; position: absolute;
            top: 205px" Width="152px"></asp:Label>
        <asp:Label ID="lblSurvey" runat="server" Text="Survey" Style="z-index: 100; left: 16px;
            position: absolute; top: 230px" Width="135px"></asp:Label>
        <asp:Label ID="lblSurveyValue" runat="server" Style="z-index: 100; left: 159px; position: absolute;
            top: 230px" Width="152px" Height="47px"></asp:Label>
        <asp:Label ID="lblBarcode" runat="server" Text="Barcode" Style="z-index: 100; left: 16px;
            position: absolute; top: 288px" Width="135px"></asp:Label>
        <asp:TextBox ID="txtBarcodeValue" runat="server" Style="z-index: 100; left: 159px;
            position: absolute; top: 286px" Width="152px"></asp:TextBox>
        <asp:Label ID="lblAHFReference" runat="server" Text="AHFReference" Style="z-index: 100;
            left: 16px; position: absolute; top: 313px" Width="135px"></asp:Label>
        <asp:TextBox ID="txtAHFReferenceValue" runat="server" Style="z-index: 100; left: 159px;
            position: absolute; top: 311px" Width="152px"></asp:TextBox>
        <asp:Label ID="lblRegionalLab" runat="server" Text="Regional Lab" Style="z-index: 100;
            left: 16px; position: absolute; top: 363px" Width="135px"></asp:Label>
        <asp:DropDownList ID="ddlRegionalLabValue" runat="server" Style="z-index: 100; left: 159px;
            position: absolute; top: 361px" Width="152px">
        </asp:DropDownList>
        <asp:Label ID="lblRegionalLabReceivedDate" runat="server" Text="Regional Lab Received"
            Style="z-index: 100; left: 16px; position: absolute; top: 388px" Width="135px"></asp:Label>
        <div style="z-index: 203; left: 159px; position: absolute; top: 384px">
            <uc1:CalendarDate ID="ctlRegionalLabReceivedDateValue" runat="server" AllowUnknown="false" ></uc1:CalendarDate>
        </div>
        <asp:Label ID="lblInitialReceivedDate" runat="server" Text="Initial Received" Style="z-index: 100;
            left: 16px; position: absolute; top: 413px" Width="135px"></asp:Label>
        <div style="z-index: 202; left: 159px; position: absolute; top: 409px">
            <uc1:CalendarDate ID="ctlInitialReceivedDateValue" runat="server" AutoPostBack="true">
            </uc1:CalendarDate>
        </div>
        <asp:Label ID="lblFinalReceivedDate" runat="server" Text="Final Received" Style="z-index: 100;
            left: 16px; position: absolute; top: 438px" Width="135px"></asp:Label>
        <div style="z-index: 201; left: 159px; position: absolute; top: 434px">
            <uc1:CalendarDate ID="ctlFinalReceivedDateValue" runat="server" AutoPostBack="true">
            </uc1:CalendarDate>
        </div>
        <asp:Label ID="lblFinalResult" runat="server" Text="Final Result" Style="z-index: 100;
            left: 16px; position: absolute; top: 488px" Width="135px"></asp:Label>
        <asp:Label ID="lblFinalResultValue" runat="server" Style="z-index: 100; left: 159px;
            position: absolute; top: 488px" Width="152px"></asp:Label>
        <asp:Label ID="lblFinalResultDate" runat="server" Text="Final Result Date" Style="z-index: 100;
            left: 16px; position: absolute; top: 513px" Width="135px"></asp:Label>
        <asp:Label ID="lblFinalResultDateValue" runat="server" Style="z-index: 100; left: 159px;
            position: absolute; top: 513px" Width="152px"></asp:Label>
        <asp:Label ID="lblDBSE" runat="server" Text="DBSE" Style="z-index: 100; left: 16px;
            position: absolute; top: 538px" Width="135px"></asp:Label>
        <asp:Label ID="lblDBSEValue" runat="server" Style="z-index: 100; left: 159px; position: absolute;
            top: 538px" Width="152px"></asp:Label>
        <asp:Label ID="lblAlternateDiagnosis" runat="server" Text="Alternate Diagnosis" Style="z-index: 100;
            left: 16px; position: absolute; top: 563px" Width="135px"></asp:Label>
        <asp:Label ID="lblAlternateDiagnosisValue" runat="server" Style="z-index: 100; left: 159px;
            position: absolute; top: 563px" Width="152px"></asp:Label>
        <asp:Label ID="lblFinalSentDate" runat="server" Text="Final Sent Date" Style="z-index: 100;
            left: 16px; position: absolute; top: 588px" Width="135px"></asp:Label>
        <div style="z-index: 200; left: 159px; position: absolute; top: 585px">
            <uc1:CalendarDate ID="ctlFinalSentDateValue" runat="server" AutoPostBack="true">
            </uc1:CalendarDate>
        </div>
        <asp:Label ID="lblBirthDate" runat="server" Text="Birth Date" Style="z-index: 100;
            left: 16px; position: absolute; top: 638px" Width="135px"></asp:Label>
        <asp:Label ID="lblBirthDateValue" runat="server" Style="z-index: 100; left: 159px;
            position: absolute; top: 638px" Width="152px"></asp:Label>
        <!--Column 2-->
        <asp:Label ID="lblPaperworkReceived" runat="server" Font-Bold="True" Text="Paperwork Received"
            Style="z-index: 100; left: 346px; position: absolute; top: 27px"></asp:Label>
        <asp:Label ID="lblPurchaserBSE1ReceivedDate" runat="server" Text="Purchaser BSE1"
            Style="z-index: 100; left: 346px; position: absolute; top: 55px" Width="162px"></asp:Label>
        <div style="z-index: 208; left: 488px; position: absolute; top: 52px">
            <uc1:CalendarDate ID="ctlPurchaserBSE1ReceivedDateValue" runat="server" AutoPostBack="true">
            </uc1:CalendarDate>
        </div>
        <asp:Label ID="lblBreederBSE1ReceivedDate" runat="server" Text="Breeder BSE1" Style="z-index: 100;
            left: 346px; position: absolute; top: 80px" Width="152px"></asp:Label>
        <div style="z-index: 207; left: 488px; position: absolute; top: 77px">
            <uc1:CalendarDate ID="ctlBreederBSE1ReceivedDateValue" runat="server" AutoPostBack="true">
            </uc1:CalendarDate>
        </div>
        <asp:Label ID="lblVendor1BSE1ReceivedDate" runat="server" Text="Vendor BSE1" Style="z-index: 100;
            left: 346px; position: absolute; top: 105px" Width="152px"></asp:Label>
        <div style="z-index: 206; left: 488px; position: absolute; top: 102px">
            <uc1:CalendarDate ID="ctlVendor1BSE1ReceivedDateValue" runat="server" AutoPostBack="true">
            </uc1:CalendarDate>
        </div>
        <asp:Label ID="lblHomeBredBSE1ReceivedDate" runat="server" Text="Homebred BSE1" Style="z-index: 100;
            left: 346px; position: absolute; top: 130px" Width="167px"></asp:Label>
        <div style="z-index: 205; left: 488px; position: absolute; top: 127px">
            <uc1:CalendarDate ID="ctlHomeBredBSE1ReceivedDateValue" runat="server" AutoPostBack="true">
            </uc1:CalendarDate>
        </div>
        <asp:Label ID="lblSummarySheetReceivedDate" runat="server" Text="Summary Sheet" Style="z-index: 100;
            left: 346px; position: absolute; top: 155px" Width="159px"></asp:Label>
        <div style="z-index: 204; left: 488px; position: absolute; top: 152px">
            <uc1:CalendarDate ID="ctlSummarySheetReceivedDateValue" runat="server" AutoPostBack="true">
            </uc1:CalendarDate>
        </div>
        <asp:Label ID="lblPaperworkCompleteDate" runat="server" Text="Paperwork Complete"
            Style="z-index: 100; left: 346px; position: absolute; top: 180px" Width="152px"></asp:Label>
        <div style="z-index: 203; left: 488px; position: absolute; top: 177px">
            <uc1:CalendarDate ID="ctlPaperworkCompleteDateValue" runat="server" AutoPostBack="true">
            </uc1:CalendarDate>
        </div>
        <asp:Label ID="lblDataCompleteDate" runat="server" Text="Data Complete" Style="z-index: 100;
            left: 346px; position: absolute; top: 230px" Width="152px"></asp:Label>
        <div style="z-index: 202; left: 488px; position: absolute; top: 227px">
            <uc1:CalendarDate ID="ctlDataCompleteDateValue" runat="server" AutoPostBack="true">
            </uc1:CalendarDate>
        </div>
        <asp:Label ID="lblTseTestingsite" runat="server" Text="TSE Testing Site" Style="z-index: 100;
            left: 346px; position: absolute; top: 260px" Width="135px"></asp:Label>
        <div style="left: 0px; top: 0px; position: absolute; z-index: 201; margin: 0px">
            <asp:TextBox ID="txtTseTestingsite" runat="server" Style="left: 488px; position: absolute;
                top: 260px" Width="165px" Enabled="false" MaxLength="50"></asp:TextBox>
            <div style="z-index: 100; left: 655px; width: 200px; position: absolute; top: 260px">
                <asp:Label ID="lblError" runat="server" CssClass="ValidatorText" ToolTip="Please enter a valid TSE Testing Site"
                    ForeColor="Red" Visible="False">*</asp:Label>
            </div>
            <asp:AutoCompleteExtender runat="server" ID="aceTseTestingSite" TargetControlID="txtTseTestingsite"
                ServiceMethod="GetAutoTestSiteEntries" EnableCaching="True" MinimumPrefixLength="1"
                FirstRowSelected="true" CompletionListCssClass="AutoExtender" CompletionListHighlightedItemCssClass="AutoExtenderHighlight"
                CompletionListItemCssClass="listItem">
            </asp:AutoCompleteExtender>
        </div>
        <asp:Label ID="lblSamplingDate" runat="server" Text="Sampling Date" Style="z-index: 100;
            left: 346px; position: absolute; top: 290px" Width="152px"></asp:Label>
        <div style="z-index: 200; left: 488px; position: absolute; top: 290px">
            <uc1:CalendarDate ID="ctlSamplingDateValue" runat="server" AutoPostBack="true" Enabled="false" AllowUnknown="true">
            </uc1:CalendarDate>
        </div>
         <asp:Label ID="lblAHRO" runat="server" Text="AHRO" Style="z-index: 100;
            left: 347px; position: absolute; top: 322px" Width="135px"></asp:Label>
        <asp:DropDownList ID="ddlAHRO" runat="server" Style="z-index: 100; left: 488px;
            position: absolute; top: 322px" Width="315px">
        </asp:DropDownList>
        <asp:Label ID="lblMinutesDue" runat="server" Text="Minutes Due" Font-Bold="True"
            Style="z-index: 100; left: 345px; position: absolute; top: 363px"></asp:Label>
        <asp:Label ID="lblMinuteSentOn" runat="server" Font-Bold="True" Style="z-index: 100;
            left: 641px; position: absolute; top: 363px" Text="Minute Sent On"></asp:Label>
        <asp:Label ID="lblActiveMemoDueDate" runat="server" Text="Active Memo" Style="z-index: 100;
            left: 345px; position: absolute; top: 388px" Width="156px"></asp:Label>
        <asp:Label ID="lblActiveMemoDueDateValue" runat="server" Style="z-index: 100; left: 453px;
            position: absolute; top: 388px" Width="152px"></asp:Label>
        <asp:Label ID="lblAnnexADueDate" runat="server" Text="Annex A" Style="z-index: 100;
            left: 345px; position: absolute; top: 413px" Width="156px"></asp:Label>
        <asp:Label ID="lblAnnexADueDateValue" runat="server" Style="z-index: 100; left: 453px;
            position: absolute; top: 413px" Width="152px"></asp:Label>
        <asp:Label ID="lblAnnexBDueDate" runat="server" Text="Annex B" Style="z-index: 100;
            left: 345px; position: absolute; top: 438px" Width="156px"></asp:Label>
        <asp:Label ID="lblAnnexBDueDateValue" runat="server" Style="z-index: 100; left: 453px;
            position: absolute; top: 438px" Width="152px"></asp:Label>
        <asp:Label ID="lblAnnexCDueDate" runat="server" Text="Annex C" Style="z-index: 100;
            left: 345px; position: absolute; top: 463px" Width="156px"></asp:Label>
        <asp:Label ID="lblAnnexCDueDateValue" runat="server" Style="z-index: 100; left: 453px;
            position: absolute; top: 463px" Width="152px"></asp:Label>
        <asp:Label ID="lblAnnexDDueDate" runat="server" Text="Annex D" Style="z-index: 100;
            left: 345px; position: absolute; top: 488px" Width="156px"></asp:Label>
        <asp:Label ID="lblAnnexDDueDateValue" runat="server" Style="z-index: 100; left: 453px;
            position: absolute; top: 488px" Width="152px"></asp:Label>
        <asp:Label ID="lblActiveMemoDateValue" runat="server" Style="z-index: 100; left: 640px;
            position: absolute; top: 388px" Width="152px"></asp:Label>
        <asp:Label ID="lblAnnexADateValue" runat="server" Style="z-index: 100; left: 640px;
            position: absolute; top: 413px" Width="152px"></asp:Label>
        <asp:Label ID="lblAnnexBDateValue" runat="server" Style="z-index: 100; left: 640px;
            position: absolute; top: 438px" Width="152px"></asp:Label>
        <asp:Label ID="lblAnnexCDateValue" runat="server" Style="z-index: 100; left: 640px;
            position: absolute; top: 463px" Width="152px"></asp:Label>
        <asp:Label ID="lblAnnexDDateValue" runat="server" Style="z-index: 100; left: 640px;
            position: absolute; top: 488px" Width="152px"></asp:Label>
        <asp:Label ID="lblLabChaseDueDate" runat="server" Text="Chase lab results" Style="z-index: 100;
            left: 347px; position: absolute; top: 538px" Width="156px"></asp:Label>
        <asp:Label ID="lblChasedOn" runat="server" Style="z-index: 100; left: 573px; position: absolute;
            top: 538px" Text="Chased on" Width="64px"></asp:Label>
        <asp:Label ID="lblLabChaseDueDateValue" runat="server" Style="z-index: 100; left: 454px;
            position: absolute; top: 538px" Width="152px"></asp:Label>
        <div style="z-index: 209; left: 640px; position: absolute; top: 535px">
            <uc1:CalendarDate ID="ctlLabChasedDateValue" runat="server" AutoPostBack="true">
            </uc1:CalendarDate>
        </div>
        <asp:Label ID="lblBarbMemoDue" runat="server" Text="BARB Minute" Style="z-index: 100;
            left: 349px; position: absolute; top: 593px" Width="156px"></asp:Label>
        <asp:Label ID="lblSentOn" runat="server" Style="z-index: 100; left: 590px; position: absolute;
            top: 593px" Text="Sent on" Width="50px"></asp:Label>
        <asp:Label ID="lblBarbMemoDueValue" runat="server" Style="z-index: 100; left: 454px;
            position: absolute; top: 593px" Width="152px"></asp:Label>
        <div style="z-index: 208; left: 640px; position: absolute; top: 589px">
            <uc1:CalendarDate ID="ctlBarbMemoDateValue" runat="server" AutoPostBack="true"></uc1:CalendarDate>
        </div>
        <!--Column 3-->
        <asp:CheckBox ID="chkOpenCloseCase" runat="server" Text="Open Case" TextAlign="Left"
            Style="z-index: 100; left: 885px; position: absolute; top: 0px" />
        <asp:Label ID="lblCaseworkComments" runat="server" Font-Bold="True" Text="Casework Comments"
            Style="z-index: 100; left: 685px; position: absolute; top: 27px"></asp:Label>
        <asp:TextBox ID="txtCaseworkComments" runat="server" Style="z-index: 100; left: 685px;
            position: absolute; top: 56px" Height="125px" TextMode="MultiLine" Width="284px"></asp:TextBox>
        <asp:Button ID="btnActiveMemoSend" runat="server" Text="Send" Style="z-index: 105;
            left: 590px; position: absolute; top: 386px" />
        <asp:Button ID="btnAnnexASend" runat="server" Text="Send" Style="z-index: 105; left: 590px;
            position: absolute; top: 411px" />
        <asp:Button ID="btnAnnexBSend" runat="server" Text="Send" Style="z-index: 105; left: 590px;
            position: absolute; top: 436px" />
        <asp:Button ID="btnAnnexCSend" runat="server" Text="Send" Style="z-index: 105; left: 590px;
            position: absolute; top: 461px" />
        <asp:Button ID="btnAnnexDSend" runat="server" Text="Send" Style="z-index: 105; left: 590px;
            position: absolute; top: 486px" />
        <!--Bottom stuff-->
        <asp:Label ID="lblBirthDateAfter2000" runat="server" Text="Born after December 2000 - Inform Defra if positive"
            Font-Bold="True" Font-Size="Medium" ForeColor="Red" Style="z-index: 105; left: 16px;
            position: absolute; top: 670px" Visible="False"></asp:Label>
        <asp:Label ID="lblDefraInformedOn" runat="server" Style="z-index: 100; left: 521px;
            position: absolute; top: 672px" Text="DEFRA informed on" Visible="False" Width="121px"></asp:Label>
        <div style="z-index: 200; left: 640px; position: absolute; top: 668px">
            <uc1:CalendarDate ID="ctlPost2000SentDateValue" runat="server" Visible="false" AutoPostBack="true">
            </uc1:CalendarDate>
        </div>
        <hr size="1" style="z-index: 101; left: 16px; width: 96.03%; position: absolute;
            top: 705px; height: 1px" width="96.03%" />
        <asp:Button ID="btnSave" runat="server" Text="Save" Style="z-index: 100; left: 833px;
            position: absolute; top: 716px" Width="58px" />
        <asp:Button ID="btnCancel" runat="server" Text="Cancel" Style="z-index: 100; left: 901px;
            position: absolute; top: 716px" Width="58px" />
    </div>
    <p>
        <uc1:VLAFooter ID="VLAFooter1" runat="server"></uc1:VLAFooter>
    </p>
    </form>
</body>
</html>
