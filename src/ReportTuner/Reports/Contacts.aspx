<%@ Page Language="C#" MasterPageFile="~/Reports/ReportMasterPage.master" AutoEventWireup="true" CodeBehind="Contacts.aspx.cs" Inherits="ReportTuner.Contacts" Theme="Main" %>
<asp:Content ID="GeneralReportContactsContent" ContentPlaceHolderID="ReportContentPlaceHolder" runat="server">
    <br/>
    <div align="center">
        <font style="font-weight:bold">Настройка рассылки для отчета: </font>
        <asp:Label ID="lReportName" runat="server"></asp:Label>
    </div>
    <br/>
    <asp:GridView ID="gvRelatedReports" runat="server" AutoGenerateColumns="false" Caption="Отчеты, использующие текущую рассылку">
        <Columns>
            <asp:BoundField DataField="Id" HeaderText="Код" HeaderStyle-Width="5%"/>
            <asp:HyperLinkField HeaderText="Настройка рассылки" Text="..." DataNavigateUrlFields="Id" DataNavigateUrlFormatString="Contacts.aspx?GeneralReport={0}" HeaderStyle-Width="15%"/>
            <asp:BoundField DataField="EMailSubject" HeaderText="Тема письма"/>
        </Columns>
        <EmptyDataTemplate>
        </EmptyDataTemplate>            
    </asp:GridView>           
    <br/>
    <table width="80%">
        <tr>
            <td width="20%">Выбор имеющейся рассылки:</td>
            <td >
                <asp:TextBox ID="tbContactFind" runat="server" AutoPostBack="true" OnTextChanged="btnFind_Click"></asp:TextBox>
                <asp:Button ID="btnFind" Text="Найти" runat="server" onclick="btnFind_Click" 
                    CausesValidation="False" />
                <asp:DropDownList ID="ContactGroups" runat="server"></asp:DropDownList>
                <asp:Button ID="btnSaveContactGropup" Text="Сохранить" runat="server" 
                    ToolTip="Устанавливаем выбранную рассылку для текущего отчета" 
                    CausesValidation="False" onclick="btnSaveContactGropup_Click" />
                <asp:Button ID="btnCancelContactGroup" Text="Отменить" runat="server" 
                    onclick="btnCancelContactGroup_Click" CausesValidation="False"/>
            </td>
        </tr>
        <tr>
            <td width="20%">Текущая рассылка:</td>
            <td >
                <asp:HyperLink ID="hlEditGroup" ToolTip="Редактирование списка контактов рассылки" 
                    Text="не установлена" runat="server" Target="_blank"></asp:HyperLink>            
                <asp:TextBox ID="tbContactGroupName" runat="server"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvContactGroupName" runat="server" 
                    ControlToValidate="tbContactGroupName" Display="Dynamic" 
                    ErrorMessage="Не установлено наименование рассылки" ToolTip="Не установлено наименование рассылки">*</asp:RequiredFieldValidator>
                <asp:CustomValidator ID="cvOnLikeName" runat="server" ControlToValidate="tbContactGroupName" 
                    Display="Dynamic" ToolTip="Название новой рассылки совпадает с названием существующей рассылки" 
                    
                    ErrorMessage="Название новой рассылки совпадает с названием существующей рассылки" 
                    onservervalidate="cvOnLikeName_ServerValidate">*</asp:CustomValidator>
                <asp:Button ID="btnSaveChangedGroupName" Text="Сохранить изменение" 
                    runat="server" onclick="btnSaveChangedGroupName_Click"/>
                <asp:Button ID="btnCancelChangeGroupName" Text="Отменить изменение" 
                    runat="server" CausesValidation="False" 
                    onclick="btnCancelChangeGroupName_Click"/>
                <asp:GridView ID="gvEmails" runat="server" AutoGenerateColumns="false">
                    <Columns>
                        <asp:BoundField DataField="ContactText" HeaderText="Email"/>
                        <asp:BoundField DataField="Comment" HeaderText="Комментарий"/>
                        <asp:BoundField DataField="Payer" HeaderText="Плательщик"/>
                    </Columns>
                    <EmptyDataTemplate>
                        Список адресов пуст.
                    </EmptyDataTemplate>            
                </asp:GridView>                    
            </td>
        </tr>
        <tr>
            <td width="20%">
            </td>
            <td >
                <asp:Button ID="btnChangeGroupName" Text="Изменить наименование рассылки" 
                    runat="server" CausesValidation="False" onclick="btnChangeGroupName_Click"/>
                <asp:Button ID="btnCreate" Text="Создать новую рассылку" 
                    runat="server" onclick="btnCreate_Click" CausesValidation="False" />
            </td>
        </tr>
        </table>
    <div align="center">
        <asp:ValidationSummary ID="ValidationSummary" runat="server"/>
    </div>
</asp:Content>
