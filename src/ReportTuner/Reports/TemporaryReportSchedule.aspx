<%@ Page Title="" Language="C#" MasterPageFile="~/Reports/ReportMasterPage.master" AutoEventWireup="true" CodeBehind="TemporaryReportSchedule.aspx.cs" Inherits="ReportTuner.Reports.TemporaryReportSchedule" Theme="Main" %>
<asp:Content ID="TemporaryReportScheduleContent" ContentPlaceHolderID="ReportContentPlaceHolder" runat="server">
    <div align="center">
        <table >
            <tr align="left">
                <td >Выбор имеющейся рассылки:</td>
                <td >
                    <asp:TextBox ID="tbContactFind" SkinID="findTexBoxSkin" runat="server" 
                        AutoPostBack="true" ontextchanged="btnFind_Click"></asp:TextBox>
                    <asp:Button ID="btnFind" Text="Найти" runat="server" onclick="btnFind_Click" />
                    <asp:DropDownList ID="ContactGroups" runat="server" Visible ="false"></asp:DropDownList>
                    <asp:Button ID="btnSaveContactGropup" Text="Сохранить" runat="server" 
                        ToolTip="Устанавливаем выбранную рассылку для текущего отчета" 
                        onclick="btnSaveContactGropup_Click" />
                    <asp:Button ID="btnCancelContactGroup" Text="Отменить" runat="server" 
                        onclick="btnCancelContactGroup_Click" />
                </td>
            </tr>
            <tr align="left">
                <td >Текущая рассылка:</td>
                <td >
                    <asp:Label ID="lContactGroupName" runat="server" Text="не установлена" />
                </td>
            </tr>
            <tr align="left">
                <td ></td>
                <td >
                    <asp:Button ID="btnRun" Text="Выполнить" runat="server" 
                        onclick="btnRun_Click" />
                </td>
            </tr>
         </table>
    </div>
    <div>
        <asp:Button ID="btnBack" runat="server" Text="Назад" style="float:left" 
            onclick="btnBack_Click" />
        <asp:Button ID="btnFinish" runat="server" Text="Завершить" style="float:right" 
            onclick="btnFinish_Click" />
    </div>
</asp:Content>
