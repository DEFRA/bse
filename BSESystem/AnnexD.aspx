<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="AnnexD.aspx.vb" Inherits="BSESystem.AnnexD" %>

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Annex D</title>
    <style>
        body 
        { 
            font-size:medium 
        }
        h1 
        { 
            font-size:medium 
        }
        table.HeaderLayout
        {
            font-size:medium;
            width:100%;
            border-collapse:collapse;
        }
        td.HeaderLeft
        {
            width:50%;
            height:21px;
            vertical-align:top;
            text-align:left
        }
        td.HeaderRight
        {
            width:50%;
            height:21px;
            vertical-align:top;
            text-align:right
        }
        table.InformationTable
        {
            font-size:medium;
            width:100%;
            border-collapse:collapse;
            border-right: 2px black solid;
            border-bottom: 2px black solid;
        }
        tr.InformationTableHeader
        {
            font-weight:Bold;
            vertical-align:top
        }
        td.InformationTableCell
        {
            border:1px black solid;
            text-align: left
        }
        hr
        {
            border: 1px black solid;
            height: 1px
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <p align="left">ANNEX D</p>
        <h1 align="left">OUTSTANDING PAPERWORK - <em>SECOND REMINDER</em></h1>
        
        <table class="HeaderLayout">
            <tr>
                <td class="HeaderLeft">
                    From:<br />
                    Brenda Rajanayagam [VLA  D (Sci)]<br />
                    CERA 3 - Area E - Weybourne Building<br />
                    VLA Weybridge, New Haw,<br />
                    Addlestone,<br />
                    KT15 3NB
                </td>
                <td class="HeaderRight">
                    To: DVM
                    <br />
                    At: <asp:Label id="lblAHOName1" runat="server"></asp:Label>
                    <br />
                    <br />
                    Cc: ADVFS
                    <br />
                    At: <asp:Label id="lblAHOName2" runat="server"></asp:Label>
                </td>
            </tr>
        </table>
        
        <p align="left">Date: <asp:Label id="lblDate" runat="server"></asp:Label></p>
        
        <p align="left"><strong>
            A fax requesting information was sent to you on <asp:label id="lblAnnexCDate" runat="server"></asp:label> by Keith Russell.<br />
            To date no reply has been received.
        </strong></p>
        
        <p align="left">
            BSE: FORMS OUTSTANDING</p>
        
        <table class="InformationTable">
            <tr>
                <td colspan="2"></td>
                <td colspan="4" style="border-color:black; border-style:solid; border-width: 2px 2px 0px 2px; text-align:center">To be completed by AHDO</td>
            </tr>
            <tr class="InformationTableHeader">
                <td rowspan="2" style="border-left:2px black solid; border-top:2px black solid;" class="InformationTableCell">RBSE No</td>
                <td rowspan="2" style="border-top:2px black solid;"class="InformationTableCell">Outstanding BSE Forms</td>
                <td colspan="2" class="InformationTableCell">Form will follow by fax or email</td>
                <td rowspan="2" class="InformationTableCell">Tracing required (Y/N)</td>
                <td rowspan="2" class="InformationTableCell">
                    Name of AHDO who will be carrying out the tracing</td>
            </tr>
            <tr class="InformationTableHeader">
               <td class="InformationTableCell">YES</td>
               <td class="InformationTableCell">NO</td> 
            </tr>
            <asp:Repeater id="rptRepeater" runat="server">
                <ItemTemplate>
                    <tr>
                        <td class="InformationTableCell" style="border-left:2px black solid"><asp:label id="lblRBSE" runat="server" Text=<%# DataBinder.Eval(Container.DataItem, "RBSE") %>></asp:label></td>
                        <td class="InformationTableCell"><asp:label id="lblFormType" runat="server" Text=<%# DataBinder.Eval(Container.DataItem, "FormType") %>></asp:label></td>
                        <td class="InformationTableCell"></td>
                        <td class="InformationTableCell"></td>
                        <td class="InformationTableCell"></td>
                        <td class="InformationTableCell"></td>
                     </tr>
                </ItemTemplate>
            </asp:Repeater>
        
        </table>
        <p>
           Paper work relating to the BSE cases listed has not been received as 
           indicated in column 1 & 2. Would you please complete the table as necessary
           and fax by return if possible and please send the requested paperwork by email 
           or post ASAP.
        </p>
        
        <p align="left"><strong><u>Urgent - Please fax information to 01932 359429 by return if possible</u></strong></p>
        
        <p align="left"><strong>Please fax as soon as possible</strong></p>
        
        <p align="left">Thank you</p>
    </div>
    </form>
</body>
</html>
