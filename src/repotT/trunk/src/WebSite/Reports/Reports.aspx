<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Reports.aspx.cs" Inherits="Reports_Reports" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" %>

<asp:Content runat="server" ID="ReportGeneralReportsContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align="center">
        <strong style="font-size:small;">Настройка отчетов</strong><br/>
        <asp:GridView ID="dgvReports" runat="server" AutoGenerateColumns="False">
            <Columns>
                <asp:TemplateField HeaderText="Отчет">
                    <ItemTemplate>
                        <asp:DropDownList ID="ddlReports" runat="server">
                        </asp:DropDownList>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Наименование">
                    <ItemTemplate>
                        <asp:TextBox ID="tbCaption" runat="server"></asp:TextBox><asp:RequiredFieldValidator
                            ID="rfvCaption" runat="server" ControlToValidate="tbCaption" ErrorMessage='Поле "Наименование" должно быть заполнено'>*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:HyperLinkField HeaderText="Параметры" Text="..." />
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>