<%@ Control Language="vb" AutoEventWireup="false" Codebehind="CalendarDate.ascx.vb" Inherits="BSESystem.CalendarDate" TargetSchema="http://schemas.microsoft.com/intellisense/ie5" %>
<div style="Z-INDEX: 100; LEFT: 0px; POSITION: absolute; TOP: 0px"><asp:textbox id="txtDate" runat="server" Width="132px" MaxLength="17"></asp:textbox></div>
<div style="Z-INDEX: 101; LEFT: 131px; POSITION: absolute; TOP: 0px"><asp:imagebutton id="btnCalendar" CausesValidation="False" Runat="server" ImageUrl="images/calendarOpenDown.gif"></asp:imagebutton></div>
<div style="Z-INDEX: 102; LEFT: 168px; WIDTH: 200px; POSITION: absolute; TOP: 2px"><asp:requiredfieldvalidator id="rfvDate" runat="server" CssClass="ValidatorText" ErrorMessage="*" ToolTip="Please enter a date" ControlToValidate="txtDate" Enabled="False"></asp:requiredfieldvalidator></div>
<div style="Z-INDEX: 103; LEFT: 168px; WIDTH: 200px; POSITION: absolute; TOP: 2px"><asp:customvalidator id="cvDate" runat="server" CssClass="ValidatorText" ErrorMessage="*" ToolTip="Invalid Date" ControlToValidate="txtDate" Enabled="True" Height="8px"></asp:customvalidator></div>
<div style="Z-INDEX: 104; LEFT: 168px; WIDTH: 200px; POSITION: absolute; TOP: 2px"><asp:label id="lblError" runat="server" CssClass="ValidatorText" ToolTip="Please enter a valid date" ForeColor="Red" Visible="False">*</asp:label></div>
<asp:literal id="litCalendar" runat="server"></asp:literal>
<asp:textbox id="txtClosedCalendar" runat="server" Width="0px" Height="0px"></asp:textbox>
