<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_ReportTypes" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="ReportTypes.aspx.cs" %>

<asp:Content runat="server" ID="ReportTypesContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align=center>
        <strong style="font-size:small;">���� �������&nbsp;</strong><br/>
        <asp:GridView ID="dgvReportTypes" runat="server" AutoGenerateColumns="False" OnRowCommand="dgvReportTypes_RowCommand" OnRowDeleting="dgvReportTypes_RowDeleting" OnRowDataBound="dgvReportTypes_RowDataBound">
            <Columns>
                <asp:BoundField DataField="RTCode" HeaderText="���" Visible="False" />
                <asp:TemplateField HeaderText="������������ ������">
                    <ItemTemplate>
                        <asp:TextBox ID="tbName" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.RTName") %>'></asp:TextBox><asp:RequiredFieldValidator ID="rfvName" runat="server" ControlToValidate="tbName"
                            ErrorMessage='���� "������������ ������" ������ ���� ���������' ValidationGroup="vgRepType">*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="������� �����">
                    <ItemTemplate>
                        <asp:TextBox ID="tbPrefix" runat="server" Width="79px" Text='<%# DataBinder.Eval(Container, "DataItem.RTPrefix") %>'></asp:TextBox><asp:RequiredFieldValidator ID="rfvPrefix" runat="server" ControlToValidate="tbPrefix"
                            ErrorMessage='���� "������� �����" ������ ���� ���������' ValidationGroup="vgRepType">*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="�������������� ���� ������">
                    <ItemTemplate>
                        <asp:TextBox ID="tbSubject" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.RTSubject") %>'></asp:TextBox><asp:RequiredFieldValidator ID="rfvSubject" runat="server" ControlToValidate="tbSubject"
                            ErrorMessage='���� "�������������� ���� ������" ������ ���� ���������' ValidationGroup="vgRepType">*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="C# �����">
                    <ItemTemplate>
                        <asp:TextBox ID="tbClass" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.RTClass") %>'></asp:TextBox><asp:RequiredFieldValidator ID="rfvClass" runat="server" ControlToValidate="tbClass"
                            ErrorMessage='���� "C# �����" ������ ���� ���������' ValidationGroup="vgRepType">*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:HyperLinkField DataNavigateUrlFields="RTCode" DataNavigateUrlFormatString="ReportTypeProperties.aspx?rtc={0}"
                    HeaderText="���������" Text="..." />
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
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="�������� ��� ������" />
			</EmptyDataTemplate>
        </asp:GridView>
        <asp:Button ID="btnApply" runat="server" OnClick="btnApply_Click" Text="���������" ValidationGroup="vgRepType" />
    </div>
</asp:Content>