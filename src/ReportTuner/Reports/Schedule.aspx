<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_schedule" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="Schedule.aspx.cs" %>

<asp:Content runat="server" ID="ScheduleValuesContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align="center"><strong><font size ="2">        
Задание для отчета "<asp:Label ID="lblReportComment" runat="server" Text="Label"/>" для плательщика "<asp:Label ID="lblClient" runat="server" Text="Label"/>"<br /><br />
</font></strong></div>
    <div><font size ="2">
    <table width="100%">
        <tr bgcolor="#eef8ff"><td>
            <asp:Label ID="Label2" runat="server" Text="Выполнить:" SkinID="scheduleLabelSkin"></asp:Label>
            <asp:Label ID="lblWork" runat="server" Text="Label"></asp:Label>
        </td></tr>
        <tr bgcolor="#f6f6f6"><td>
            <asp:Label ID="Label1" runat="server" Text="Рабочая папка:" SkinID="scheduleLabelSkin"></asp:Label>
            <asp:Label ID="lblFolder" runat="server" Text="Label"></asp:Label>
        </td></tr>
        <tr bgcolor="#eef8ff"><td valign="top">
        <!-- SkinID="scheduleLabelSkin"-->
            <div style="float:left;width:200px;">Комментарий:</div>
<%--            <asp:Label ID="Label5" BackColor="red" runat="server" style="vertical-align:text-top;" Text="Комментарий:" ></asp:Label>
--%>            <asp:TextBox ID="tbComment" runat="server" TextMode="MultiLine" SkinID="passwordTexBoxSkin"></asp:TextBox>
        </td></tr>
        <tr bgcolor="#f6f6f6"><td>
            <asp:CheckBox ID="chbAllow" runat="server" Text="Разрешено" />
        </td></tr>
    </table>
    </font>
    <asp:Button ID="btnExecute" runat="server" Text="Выполнить задание" ValidationGroup="vgPassword" OnClick="btnExecute_Click" /><br /><br />
    </div>
    <div align="center">
        <asp:GridView ID="dgvSchedule" runat="server" AutoGenerateColumns="False" Caption="Расписание" OnRowCommand="dgvSchedule_RowCommand" OnRowDeleting="dgvSchedule_RowDeleting" OnRowDataBound="dgvSchedule_RowDataBound">
            <Columns>
                <asp:TemplateField HeaderText="Время начала">
                    <ItemTemplate>
                        <asp:TextBox ID="tbStart" runat="server" ></asp:TextBox>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Пн">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbMonday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SMonday"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Вт">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbTuesday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.STuesday"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Ср">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbWednesday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SWednesday"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Чт">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbThursday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SThursday"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Пт">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbFriday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SFriday"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Сб">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbSaturday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SSaturday"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Вс">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbSunday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SSunday"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField>
				<HeaderTemplate>
					<asp:Button ID="btnAdd" runat="server" Text="Добавить" CommandName="Add" />
				</HeaderTemplate>
				<ItemTemplate>
					<asp:Button ID="btnDelete" runat="server" Text="Удалить" CommandName="Delete" />
				</ItemTemplate>
                </asp:TemplateField>
            </Columns>
   			<EmptyDataTemplate>
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="Добавить расписание" />
			</EmptyDataTemplate>
        </asp:GridView>
        <br/>
        <asp:GridView ID="gvOtherTriggers" runat="server" 
            Caption="Дополнительное расписание" AutoGenerateColumns="False" >
            <Columns>
                <asp:BoundField DataField="!" HeaderText="Описание" />
            </Columns>
        </asp:GridView>
        <br/>
        <div align="center" style="width:70%;">
        <asp:GridView ID="gvLogs" runat="server" 
            Caption="Статистика выполнения отчета" AutoGenerateColumns="False"  EmptyDataText="Нет данных">
            <Columns>
                <asp:BoundField DataField="LogTime" HeaderText="Дата" />
                <asp:BoundField DataField="EMail" HeaderText="EMail" />
                <asp:BoundField DataField="SMTPID" HeaderText="SMTPID" />
            </Columns>
        </asp:GridView>
        </div>
        <br/>
        <asp:Button ID="btnApply" runat="server" Text="Применить" OnClick="btnApply_Click" ValidationGroup="vgPassword" />
    </div>
</asp:Content>
