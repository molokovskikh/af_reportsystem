<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_EnumValues" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="EnumValues.aspx.cs" %>

<asp:Content runat="server" ID="ReportEnumValuesContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align=center>
        <strong style="font-size:small;">Значения перечислимого типа&nbsp;"</strong>
        <asp:Label ID="lblEnumName" runat="server" Font-Bold="True"></asp:Label>
        <strong style="font-size:small;">"</strong><br />
        <asp:GridView ID="dgvEnumValues" runat="server" AutoGenerateColumns="False" OnRowCommand="dgvEnumValues_RowCommand" OnRowDeleting="dgvEnumValues_RowDeleting" OnRowDataBound="dgvEnumValues_RowDataBound">
            <Columns>
                <asp:BoundField DataField="evID" HeaderText="Код" Visible="False" />
                <asp:TemplateField HeaderText="Значение">
                    <ItemTemplate>
                        <asp:TextBox ID="tbValue" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.evValue") %>'></asp:TextBox><asp:RequiredFieldValidator ID="rfvValue" runat="server" ControlToValidate="tbValue"
                            ErrorMessage='Поле "Значение" должно быть заполнено' ValidationGroup="vgEnums">*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Отображаемое значение">
                    <ItemTemplate>
                        <asp:TextBox ID="tbDisplayValue" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.evDisplayValue") %>'></asp:TextBox><asp:RequiredFieldValidator ID="rfvDisplayValue" runat="server" ControlToValidate="tbDisplayValue"
                            ErrorMessage='Поле "Отображаемое значение" должно быть заполнено' ValidationGroup="vgEnums">*</asp:RequiredFieldValidator>
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
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="Добавить значение" />
			</EmptyDataTemplate>
        </asp:GridView>
        <asp:Button ID="btnApply" runat="server" OnClick="btnApply_Click" Text="Применить" ValidationGroup="vgEnums" />
    </div>
</asp:Content>