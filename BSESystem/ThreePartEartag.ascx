<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="ThreePartEartag.ascx.vb" Inherits="BSESystem.ThreePartEartag" %>

<div style="Z-INDEX:100; LEFT:0px; POSITION:absolute; TOP:0px">
	<asp:TextBox id="txtEartagCountry" runat="server" MaxLength="4" Width="35px"></asp:TextBox>
</div>
<div style="Z-INDEX:100; LEFT:40px; POSITION:absolute; TOP:0px">
    <asp:TextBox id="txtEartagHerdmark" Width="60px" MaxLength="8" runat="server"></asp:TextBox>
</div>
<div style="Z-INDEX:100; LEFT:105px; POSITION:absolute; TOP:0px">
	<asp:TextBox id="txtEartagAnimal" Width="80px" MaxLength="25" runat="server"></asp:TextBox>
</div>
<asp:Label ID="lblError" runat="server" style="Z-INDEX:100; LEFT:185px; POSITION:absolute; TOP:2px" CssClass="ValidatorText" EnableViewState="False"
    ForeColor="Red" ToolTip="Please enter an eartag" Visible="False">*</asp:Label>
<asp:Label id="lblType" style="LEFT: 195px; POSITION: absolute; TOP: 2px" CssClass="ValidatorText" runat="server" ForeColor="#9CCE00" ToolTip="Unknown format" Visible="False">*</asp:Label>
