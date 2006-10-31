<%@ Page Language="C#" AutoEventWireup="true" CodeFile="EnumValues.aspx.cs" Inherits="Reports_EnumValues" Theme="Main" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>��������� �������</title>
</head>
<body>
    <form id="form1" runat="server">
    <div align="right"><a href="base.aspx">�����</a></div>
    <div align=center>
        <strong style="font-size:small;">�������� ������������� ����&nbsp;"</strong>
        <asp:Label ID="lblEnumName" runat="server" Font-Bold="True"></asp:Label>
        <strong style="font-size:small;">"</strong><br />
        <asp:GridView ID="dgvEnumValues" runat="server" AutoGenerateColumns="False" OnRowCommand="dgvEnumValues_RowCommand" OnRowDeleting="dgvEnumValues_RowDeleting" OnRowDataBound="dgvEnumValues_RowDataBound">
            <Columns>
                <asp:BoundField DataField="evID" HeaderText="���" Visible="False" />
                <asp:TemplateField HeaderText="��������">
                    <ItemTemplate>
                        <asp:TextBox ID="tbValue" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.evValue")%>'></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvValue" runat="server" ControlToValidate="tbValue"
                            ErrorMessage='���� "��������" ������ ���� ���������'>*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="������������ ��������">
                    <ItemTemplate>
                        <asp:TextBox ID="tbDisplayValue" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.evDisplayValue")%>'></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvDisplayValue" runat="server" ControlToValidate="tbDisplayValue"
                            ErrorMessage='���� "������������ ��������" ������ ���� ���������'>*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
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
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="�������� ��������" />
			</EmptyDataTemplate>
        </asp:GridView>
        <asp:Button ID="btnApply" runat="server" OnClick="btnApply_Click" Text="���������" />
    </div>
    </form>
</body>
</html>
