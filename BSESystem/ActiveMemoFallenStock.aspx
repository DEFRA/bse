<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="ActiveMemoFallenStock.aspx.vb"
    Inherits="BSESystem.ActiveMemoFallenStock" EnableViewState="false" %>

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Active Memo</title>
    <style type="text/css">
        body
        {
            font-size: medium;
        }
        li
        {
            margin: 1em 10% 1em 0;
        }
        h1
        {
            font-size: medium;
        }
        table.ScheduleTable
        {
            font-size: medium;
            width: 100%;
            border: 2px black solid;
            border-collapse: collapse;
        }
        tr.ScheduleTableHeader
        {
            font-weight: bold;
        }
        td.ScheduleTableCell
        {
            border: 1px black solid;
        }
        #textAddress
        {  
            margin-left:46px;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <p align="left">
            Appendix 1</p>
        <h1 align="left">
            BOVINE SPONGIFORM ENCEPHALOPATHY:<br />
            <asp:Label ID="lblHeadingTestResult" runat="server" /> RESULT TO AN INITIAL SCREENING TEST -<br />
            ANIMAL TAKEN IN A SURVEY
        </h1>
        <p align="right">
            <strong>
                <asp:Label ID="lblRBSE" runat="server" Text="RBSE Ref: xx/xx/xxxxx"></asp:Label>
            </strong>
        </p>
        <p align="right">
            <strong>
                <asp:Label ID="lblCPHH" runat="server" Text="CPH:xx/xxx/xxxxx/xx"></asp:Label>
            </strong>
        </p>
        <p align="right">
            <strong>
                <asp:Label ID="lblDate" runat="server" Text="Date of Report: xx/xx/xxxx"></asp:Label>
            </strong>
        </p>
        <div id="textAddressLine1">
            <strong>To&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;SSC Worcester</strong> (AHspecialistservicecentreworcester@apha.gov.uk)<br />
        </div>
        <div id="textAddress">
            <asp:Label ID="lblFarmAHOName" runat="server"></asp:Label><br />
            VeterinaryandTechnical.ServiceTeam@apha.gov.uk<br />
            Victor Andres VSM<br />
            Mike Dawson AHVLA</div>
    <ol>
    </ol>
    <ol>
        <li value="1">Sample taken in the <strong>
            <asp:Label ID="lblCaseSurvey" runat="server"></asp:Label>
            SURVEY.</strong></li>
        <li value="2">Tissue from the animal identified below has given <asp:Label ID="lblBodyTestResult" runat="server" /> result to
            a <strong>screening</strong> test for BSE.</li>
        <li value="3">The brain tissue has been identified as coming from the following animal:</li>
    </ol>
    <h1>
        Schedule of Case</h1>
    <table class="ScheduleTable" align="center">
        <tr class="ScheduleTableHeader">
            <td class="ScheduleTableCell">
                Owner
            </td>
            <td class="ScheduleTableCell">
                Ear tag no
            </td>
            <td class="ScheduleTableCell">
                Date of birth/age
            </td>
            <td class="ScheduleTableCell">
                Date of Slaughter/Death
            </td>
        </tr>
        <tr>
            <td class="ScheduleTableCell">
                <asp:Label ID="lblFarmOwner" runat="server"></asp:Label>
            </td>
            <td class="ScheduleTableCell">
                <asp:Label ID="lblEartag" runat="server"></asp:Label>
            </td>
            <td class="ScheduleTableCell">
                <asp:Label ID="lblBirthDate" runat="server"></asp:Label>
            </td>
            <td class="ScheduleTableCell">
                <asp:Label ID="lblCaseSlaughterDate" runat="server"></asp:Label>
            </td>
        </tr>
    </table>
    <%--   <h1>
            Potential offspring</h1>--%>
    <%-- <p align="left">
            <strong><u>Please check carefully at on-farm visit</u></strong></p>--%>
    <ol>
        <li value="4">This is not yet a confirmed result. Further definitive tests will now
            be carried out and a final result will follow.</li>
        <li value="5">The brain tissue has been identified to come from the following TSE Testing
            site:</li>
        <%--<li value="5">Further definitive tests will now be carried out and a final result will
                follow.</li>--%>
        <%--<li value="5"></li>
            <li value="6"></li>
            <li value="7"></li>--%>
    </ol>
    <table class="ScheduleTable" align="center">
        <tr class="ScheduleTableHeader">
            <td class="ScheduleTableCell">
                TSE Testing site name
            </td>
            <td class="ScheduleTableCell">
                Address
            </td>
            <td class="ScheduleTableCell">
                Approval Number
            </td>
            <td class="ScheduleTableCell">
                Date of sampling
            </td>
        </tr>
        <tr>
            <td class="ScheduleTableCell">
                <asp:Label ID="lblTseTestingSiteName" runat="server"></asp:Label>
            </td>
            <td class="ScheduleTableCell">
                <asp:Label ID="lblTseTestingSiteAddress" runat="server"></asp:Label>
            </td>
            <td class="ScheduleTableCell">
                <asp:Label ID="lblApprovalNumber" runat="server"></asp:Label>
            </td>
            <td class="ScheduleTableCell">
                <asp:Label ID="lblSamplingDate" runat="server"></asp:Label>
            </td>
        </tr>
    </table>
    <ol>
        <li value="6">SSC at Worcester -
            <br />
            MUST FOLLOW INSTRUCTIONS 'ACTION FOLLOWING PRELIMINARY POSITIVE RESULT'
            <br />
            WHICH CAN BE FOUND AT
            <br />
            <a id="lnkInstructions" href="http://intranet/v1p3r/opsman/content/chp25/sample/results.html#6"
                visible="true">http://intranet/v1p3r/opsman/content/chp25/sample/results.html#6
            </a></li>
        <li value="7">ANIMAL HEALTH OFFICE RESPONSIBLE FOR FALLEN STOCK TSE TESTING SITE -
            <br />
            MUST FOLLOW 'ACTION AT TESTING PREMISES ON RECEIPT OF NOTIFICATION OF POSITIVE SAMPLE
            RESULT'
            <br />
            INSTRUCTIONS WHICH CAN BE FOUND AT
            <br />
            <a id="lnkInstructionsTSETesting" href="http://intranet/v1p3r/workareas/Animal_By-Products/TSE_Sampling_Sites/Action_on_Receipt_of_Positive_Result.html"
                visible="true">http://intranet/v1p3r/workareas/Animal_By-Products/TSE_Sampling_Sites/Action_on_Receipt_of_Positive_Result.html</a>
        </li>
    </ol>
    </div>
    </form>
</body>

<script language="javascript">
    if (window.print)
        window.print();
</script>

</html>
