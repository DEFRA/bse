<%@ Control Language="vb" AutoEventWireup="false" Codebehind="BatchNumberDisplay.ascx.vb" Inherits="BSESystem.BatchNumberDisplay" TargetSchema="http://schemas.microsoft.com/intellisense/ie5" %>
<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
<div style="Z-INDEX: 101; LEFT: 0px; POSITION: absolute; TOP: 3px">
	<asp:Label id="lblBatchNumbers" runat="server" Font-Bold="True">Batch Numbers</asp:Label>
</div>
<div style="Z-INDEX: 101; LEFT: 120px; POSITION: absolute; TOP: 0px">
	<asp:ImageButton id="btnView" CausesValidation="False" Runat="server" ImageUrl="images/btnDown.gif"></asp:ImageButton>
</div>
<asp:Literal id="litTable" runat="server"></asp:Literal>
