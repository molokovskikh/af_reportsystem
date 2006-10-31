<%@ Page Language="C#" AutoEventWireup="true" CodeFile="PropertyEnums.aspx.cs" Inherits="Reports_PropertyEnums" Theme="Main" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Настройка отчетов</title>
</head>
<body>
    <form id="form1" runat="server">
    <div align=center>
            <strong style="font-size:small;">Список типов</strong><br/>
        <asp:GridView ID="dgvEnums" runat="server" AutoGenerateColumns="False" OnRowCommand="dgvEnums_RowCommand" OnRowDeleting="dgvEnums_RowDeleting">
            <Columns>
                <asp:BoundField DataField="eID" HeaderText="Код" Visible="False" />
                <asp:TemplateField HeaderText="Наименование">
                    <ItemTemplate>
                        <asp:TextBox ID="tbEnumName" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.eName")%>'></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvEnumName" runat="server" ControlToValidate="tbEnumName"
                            ErrorMessage='Поле "Наименование" должно быть заполнено'>*</asp:RequiredFieldValidator>
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
        <asp:Button ID="btnApply" runat="server" Text="Применить" OnClick="btnApply_Click" />
    </div>
    </form>
</body>
</html>
