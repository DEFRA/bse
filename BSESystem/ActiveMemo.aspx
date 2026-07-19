<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="ActiveMemo.aspx.vb" Inherits="BSESystem.ActiveMemo" EnableViewState="false"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Active Memo</title>
    <style>
        body 
        { 
            font-size:medium 
        }
        li 
        { 
            margin:1em 10% 1em 0
        }
        h1 
        { 
            font-size:medium 
        }
        table.ScheduleTable
        {
            font-size:medium;
            width:100%;
            border:2px black solid;    
            border-collapse: collapse; 
        }
        tr.ScheduleTableHeader
        {
            font-weight:bold
        }
        td.ScheduleTableCell
        {
            border:1px black solid;
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
            POSITIVE RESULT TO AN INITIAL SCREENING TEST -<br />
            ANIMAL TAKEN IN A SURVEY
        </h1>
       
        <p align="right"><strong>
            <asp:Label id="lblRBSE" runat="server" Text="RBSE: xx/xx/xxxxx"></asp:Label>
        </strong></p>
        
        <p align="right"><strong>
            <asp:Label id="lblAHONumber" runat="server" Text="AHO: xx"></asp:Label>
        </strong></p>
        
        <p align="right"><strong>
            <asp:Label id="lblCPHH" runat="server" Text="CPHH:xx/xxx/xxxxx/xx"></asp:Label>
        </strong></p>
        
        <p align="right"><strong>
            <asp:Label id="lblDate" runat="server" Text="Date of Report: xx/xx/xxxx"></asp:Label>
        </strong></p>
        
        <p align="left">To<strong>
            SSC Worcester
        </strong>(AHspecialistservicecentreworcester@apha.gov.uk)</p>
        
        <ol>
            <li value="1">Sample taken in the <asp:Label id="lblCaseSurvey" runat="server"></asp:Label></li>
            <li value="2">Tissue from the animal identified below has given a positive result to a <strong>screening</strong> test for BSE.</li>
            <li value="3">The brain tissue has been identified as coming from the following animal:</li>
        </ol>
        
        <h1>Schedule of Case</h1>
        
        <table class="ScheduleTable" align="center">
            <tr class="ScheduleTableHeader">
                <td class="ScheduleTableCell">Owner</td>
                <td class="ScheduleTableCell">Ear tag no</td>
                <td class="ScheduleTableCell">Date of birth/age</td>
                <td class="ScheduleTableCell">Date of Slaughter/Death</td>
            </tr>
            <tr>
                <td class="ScheduleTableCell"><asp:Label id="lblFarmOwner" runat="server"></asp:Label></td>
                <td class="ScheduleTableCell"><asp:Label id="lblEartag" runat="server"></asp:Label></td>
                <td class="ScheduleTableCell"><asp:Label id="lblBirthDate" runat="server"></asp:Label></td>
                <td class="ScheduleTableCell"><asp:Label id="lblCaseSlaughterDate" runat="server"></asp:Label></td>
            </tr>
        </table>
        
        <h1>Potential offspring</h1>
        
        <p align="left"><strong><u>Please check carefully at on-farm visit</u></strong></p>
        
        <ol>
            <li value="4">This is not yet a confirmed result. Please initiate the necessary action in accordance with paragraphs K 16 - 23 of Chapter 25.</li>
            <li value="5">Further definitive tests will now be carried out and a final result will follow.</li>
        </ol>
    </div>
    </form>
</body>
<script language="javascript">
	if (window.print)
		window.print();
	</script>
</html>
