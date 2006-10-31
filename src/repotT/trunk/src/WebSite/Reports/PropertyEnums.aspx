<%@ Page Language="C#" AutoEventWireup="true" CodeFile="PropertyEnums.aspx.cs" Inherits="Reports_PropertyEnums" Theme="Main" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>��������� �������</title>
</head>
<body>
    <form id="form1" runat="server">
    <div align=center>
            <strong style="font-size:small;">������ �����</strong><br/>
        <asp:GridView ID="dgvEnums" runat="server" AutoGenerateColumns="False" OnRowCommand="dgvEnums_RowCommand" OnRowDeleting="dgvEnums_RowDeleting">
            <Columns>
                <asp:BoundField DataField="eID" HeaderText="���" Visible="False" />
                <asp:TemplateField HeaderText="������������">
                    <ItemTemplate>
                        <asp:TextBox ID="tbEnumName" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.eName")%>'></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvEnumName" runat="server" ControlToValidate="tbEnumName"
                            ErrorMessage='���� "������������" ������ ���� ���������'>*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:HyperLinkField HeaderText="������ ��������" Text="..." DataNavigateUrlFields="eID" DataNavigateUrlFormatString="EnumValues.aspx?e={0}" />
                <asp:TemplateField>
				<HeaderTemplate>
					<asp:Button ID="btnAdd" runat="server" Text="��������" CommandName="Add" />
				</HeaderTemplate>
				<ItemTemplate>
					<asp:Button ID="btnDelete" runat="server" Text="�������" CommandName="Delete" />
				</ItemTemplate>
                </asp:TemplateField>
            </Columns>
  			<EmptyDataTemplate>
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="�������� ���" />
			</EmptyDataTemplate>
        </asp:GridView>
        <asp:Button ID="btnApply" runat="server" Text="���������" OnClick="btnApply_Click" />
    </div>
    </form>
</body>
</html>
