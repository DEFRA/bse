<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="AnnexA.aspx.vb" Inherits="BSESystem.AnnexA" %>

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Annex A</title>
    <style>
        body
        {
            font-size: medium;
        }
        h1
        {
            font-size: medium;
        }
        table.HeaderLayout
        {
            font-size: medium;
            width: 100%;
            border-collapse: collapse;
        }
        td.HeaderLeft
        {
            width: 50%;
            height: 21px;
            vertical-align: top;
            text-align: left;
        }
        td.HeaderRight
        {
            width: 50%;
            height: 21px;
            vertical-align: top;
            text-align: right;
        }
        table.InformationTable
        {
            font-size: medium;
            width: 100%;
            border: 2px black solid;
            border-collapse: collapse;
        }
        td.InformationTableLeft
        {
            width: 30%;
            border: 1px black solid;
            text-align: left;
        }
        td.InformationTableRight
        {
            width: 70%;
            border: 1px black solid;
            text-align: center;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <p align="left">
            ANNEX A</p>
        <h1 align="left">
            FATE OF CASE - <em>FIRST REMINDER</em></h1>
        <table class="HeaderLayout">
            <tr>
                <td class="HeaderLeft">
                    From:
                    <br />
                    Keith Russell
                    <br />
                    CERA 3, Area E
                    <br />
                    33 Weybourne Building
                    <br />
                    VLA Weybridge
                    <br />
                    New Haw
                    <br />
                                        Addlestone
                    <br />
                    KT15 3NB
                    <br />
                    <br />
                    Tel: 01932-359428
                </td>
                <td class="HeaderRight">
                    To: DVM
                    <br />
                    At:
                    <asp:Label ID="lblAHOName" runat="server"></asp:Label>
                </td>
            </tr>
        </table>
        <p align="left">
            Date:
            <asp:Label ID="lblDate" runat="server"></asp:Label></p>
        <p align="left">
            BSE: COULD YOU PLEASE ARRANGE FOR COMPLETION OF THE FOLLOWING INFORMATION.</p>
        <br />
        <br />
        <table class="InformationTable">
            <tr>
                <td class="InformationTableLeft">
                    RBSE No.
                </td>
                <td class="InformationTableRight">
                    <asp:Label ID="lblRBSE" runat="server"></asp:Label>
                </td>
            </tr>
            <tr>
                <td class="InformationTableLeft">
                    Animal Under Restriction
                </td>
                <td class="InformationTableRight">
                    Yes/No&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;(Delete
                    one)
                </td>
            </tr>
            <tr>
                <td class="InformationTableLeft">
                    &nbsp;&nbsp;&nbsp;&nbsp;If NOT Under Restriction
                </td>
                <td class="InformationTableRight">
                    Slaughtered/Recovered&nbsp;&nbsp;&nbsp;&nbsp;(Delete one)
                </td>
            </tr>
            <tr>
                <td class="InformationTableLeft">
                    Date of Slaughter
                </td>
                <td class="InformationTableRight">
                    &nbsp;
                </td>
            </tr>
            <tr>
                <td class="InformationTableLeft">
                    &nbsp;&nbsp;&nbsp;&nbsp;OR Date of Form B
                </td>
                <td class="InformationTableRight">
                    &nbsp;
                </td>
            </tr>
        </table>
        <br />
        <br />
        <br />
        <br />
        <p align="left">
            <strong><u>Urgent - Please fax information by return to 01932 359429</u></strong></p>
        <p align="left">
            Thank you</p>
    </div>
    </form>
</body>

<script language="javascript">
	if (window.print)
		window.print();
</script>

</html>
