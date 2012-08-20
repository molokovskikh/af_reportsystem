<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_PropertyEnums" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="PropertyEnums.aspx.cs" %>

<asp:Content runat="server" ID="ReportPropertyEnumsContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align=center>
            <strong style="font-size:small;">������ �����</strong><br/>
        <asp:GridView ID="dgvEnums" runat="server" AutoGenerateColumns="False" OnRowCommand="dgvEnums_RowCommand" OnRowDeleting="dgvEnums_RowDeleting" OnRowDataBound="dgvEnums_RowDataBound">
            <Columns>
                <asp:BoundField DataField="eID" HeaderText="���" Visible="False" />
                <asp:TemplateField HeaderText="������������">
                    <ItemTemplate>
                        <asp:TextBox ID="tbEnumName" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.eName") %>'></asp:TextBox><asp:RequiredFieldValidator ID="rfvEnumName" runat="server" ControlToValidate="tbEnumName"
                            ErrorMessage='���� "������������" ������ ���� ���������' ValidationGroup="vgTypes">*</asp:RequiredFieldValidator>
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
        <asp:Button ID="btnApply" runat="server" Text="���������" OnClick="btnApply_Click" ValidationGroup="vgTypes" />
    </div>
</asp:Content>