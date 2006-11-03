<%@ Page Language="C#" AutoEventWireup="true" CodeFile="EnumValues.aspx.cs" Inherits="Reports_EnumValues" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" %>

<asp:Content runat="server" ID="ReportEnumValuesContent" ContentPlaceHolderID="ReportContentPlaceHolder">
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
</asp:Content>