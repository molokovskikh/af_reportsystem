<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_PropertyEnums" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="PropertyEnums.aspx.cs" %>

<asp:Content runat="server" ID="ReportPropertyEnumsContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align=center>
            <strong style="font-size:small;">Список типов</strong><br/>
        <asp:GridView ID="dgvEnums" runat="server" AutoGenerateColumns="False" OnRowCommand="dgvEnums_RowCommand" OnRowDeleting="dgvEnums_RowDeleting" OnRowDataBound="dgvEnums_RowDataBound">
            <Columns>
                <asp:BoundField DataField="eID" HeaderText="Код" Visible="False" />
                <asp:TemplateField HeaderText="Наименование">
                    <ItemTemplate>
                        <asp:TextBox ID="tbEnumName" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.eName") %>'></asp:TextBox><asp:RequiredFieldValidator ID="rfvEnumName" runat="server" ControlToValidate="tbEnumName"
                            ErrorMessage='Поле "Наименование" должно быть заполнено' ValidationGroup="vgTypes">*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:HyperLinkField HeaderText="Список значений" Text="..." DataNavigateUrlFields="eID" DataNavigateUrlFormatString="EnumValues.aspx?e={0}" />
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
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="Добавить тип" />
			</EmptyDataTemplate>
        </asp:GridView>
        <asp:Button ID="btnApply" runat="server" Text="Применить" OnClick="btnApply_Click" ValidationGroup="vgTypes" />
    </div>
</asp:Content>