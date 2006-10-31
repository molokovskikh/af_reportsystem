<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ReportTypes.aspx.cs" Inherits="Reports_ReportTypes" Theme="Main" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>��������� �������</title>
</head>
<body>
    <form id="form1" method="post" runat="server">
    <div align="right"><a href="base.aspx">�����</a></div>
    <div align=center>
        <strong style="font-size:small;">���� �������&nbsp;</strong><br/>
        <asp:GridView ID="dgvReportTypes" runat="server" AutoGenerateColumns="False" OnRowCommand="dgvReportTypes_RowCommand" OnRowDeleting="dgvReportTypes_RowDeleting" OnRowDataBound="dgvReportTypes_RowDataBound">
            <Columns>
                <asp:BoundField DataField="RTCode" HeaderText="���" Visible="False" />
                <asp:TemplateField HeaderText="������������ ������">
                    <ItemTemplate>
                        <asp:TextBox ID="tbName" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.RTName")%>'></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvName" runat="server" ControlToValidate="tbName"
                            ErrorMessage='���� "������������ ������" ������ ���� ���������'>*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="������� �����">
                    <ItemTemplate>
                        <asp:TextBox ID="tbPrefix" runat="server" Width="79px" Text='<%#DataBinder.Eval(Container, "DataItem.RTPrefix")%>'></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvPrefix" runat="server" ControlToValidate="tbPrefix"
                            ErrorMessage='���� "������� �����" ������ ���� ���������'>*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="�������������� ���� ������">
                    <ItemTemplate>
                        <asp:TextBox ID="tbSubject" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.RTSubject")%>'></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvSubject" runat="server" ControlToValidate="tbSubject"
                            ErrorMessage='���� "�������������� ���� ������" ������ ���� ���������'>*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="C# �����">
                    <ItemTemplate>
                        <asp:TextBox ID="tbClass" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.RTClass")%>'></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvClass" runat="server" ControlToValidate="tbClass"
                            ErrorMessage='���� "C# �����" ������ ���� ���������'>*</asp:RequiredFieldValidator>
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
        <asp:Button ID="btnApply" runat="server" OnClick="btnApply_Click" Text="���������" />
    </div>
    </form>
</body>
</html>
