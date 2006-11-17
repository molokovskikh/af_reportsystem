<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ReportPropertyValues.aspx.cs" Inherits="Reports_ReportPropertyValues" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" %>

<asp:Content runat="server" ID="ReportPropertyValuesContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align="center">
            <strong style="font-size:small;">Редактирование значений списка "<asp:Label ID="lblListName" runat="server"
            Text="Label"></asp:Label>"</strong><br/>
        <asp:TextBox ID="tbSearch" runat="server"></asp:TextBox>
        <asp:Button ID="btnSearch" runat="server" OnClick="btnSearch_Click" Text="Найти" /><br />
        <asp:CheckBox ID="chbShowEnabled" runat="server" AutoPostBack="True" Text="Только включенные" /><br />
        <asp:GridView ID="dgvListValues" runat="server" AutoGenerateColumns="False" OnRowDataBound="dgvListValues_RowDataBound">
            <Columns>
                <asp:TemplateField HeaderText="Включено">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbEnabled" runat="server" />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField HeaderText="Значение" DataField="DisplayValue" />
            </Columns>
        </asp:GridView>    
        <asp:Button ID="btnApply" runat="server" Text="Применить" OnClick="btnApply_Click" />&nbsp;
    </div>
</asp:Content>