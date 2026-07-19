<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>

<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="PickListMaintenanceAHO.aspx.vb"
    Inherits="BSESystem.PickListMaintenanceAHO" %>

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html>
<head>
    <title>BSE System : Pick List Maintenance</title>
    <meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
    <meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
    <meta content="JavaScript" name="vs_defaultClientScript">
    <meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
    <link href="Style/vla-ie.css" type="text/css" rel="stylesheet">
</head>
<body>
    <form id="Form1" method="post" runat="server">
    <p>
        <uc1:VLAHeader ID="VLAHeader1" runat="server"></uc1:VLAHeader>
    </p>
    <div style="width: 750px; position: relative; height: 96px" ms_positioning="GridLayout">
        <asp:DropDownList ID="ddlEditableLookups" Style="z-index: 100; left: 120px; position: absolute;
            top: 16px" runat="server" AutoPostBack="True" Width="160px">
        </asp:DropDownList>
        <asp:Label ID="lblSelectATable" Style="z-index: 101; left: 16px; position: absolute;
            top: 16px" runat="server" Width="88px">Select a Table</asp:Label>
        <asp:Label ID="lblTruncated" Style="z-index: 102; left: 304px; position: absolute;
            top: 16px" runat="server" Width="430px">If you enter a string longer than the database allows then it will be truncated.  Check the help to see how many characters each field allows.</asp:Label>
        <asp:Label ID="lblSystem" Style="z-index: 103; left: 304px; position: absolute; top: 56px"
            runat="server" Width="430px">View the help file before adding any new codes to find out whether it will affect the functionality of other parts of the system.</asp:Label>
    </div>
    <p>
        <asp:DataGrid ID="grdLookup" runat="server" AutoGenerateColumns="False" AllowSorting="True"
            AllowPaging="True">
            <SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
            <EditItemStyle CssClass="GridEditItem"></EditItemStyle>
            <ItemStyle CssClass="GridItem"></ItemStyle>
            <HeaderStyle CssClass="GridHeader"></HeaderStyle>
            <Columns>
                <asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
                </asp:ButtonColumn>
                <asp:BoundColumn DataField="Code" SortExpression="Code" HeaderText="Code">
                    <HeaderStyle Width="150px"></HeaderStyle>
                </asp:BoundColumn>
                <asp:BoundColumn DataField="Name" SortExpression="Name" HeaderText="Name">
                    <HeaderStyle Width="200px"></HeaderStyle>
                </asp:BoundColumn>
                <asp:TemplateColumn SortExpression="BSERegionID" HeaderText="BSE Region">
                    <HeaderStyle Width="200px"></HeaderStyle>
                    <ItemTemplate>
                        <asp:Label ID="lblBSERegionDisplay" runat="server"></asp:Label>
                    </ItemTemplate>
                    <EditItemTemplate>
                        <asp:DropDownList ID="ddlBSERegionEdit" runat="server" Width="162px" DataSource="<%# GetBSERegionList() %>"
                            DataTextField="Name" DataValueField="ID">
                        </asp:DropDownList>
                    </EditItemTemplate>
                </asp:TemplateColumn>
            </Columns>
            <PagerStyle Visible="False"></PagerStyle>
        </asp:DataGrid></p>
    <div style="width: 750px; position: relative; height: 28px" ms_positioning="GridLayout">
        <p>
            <uc1:DataGridPager ID="Pager" runat="server"></uc1:DataGridPager>
        </p>
    </div>
    <p>
        <uc1:VLAFooter ID="VLAFooter1" runat="server"></uc1:VLAFooter>
    </p>
    </form>
</body>
</html>
