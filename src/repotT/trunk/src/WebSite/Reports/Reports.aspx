<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Reports.aspx.cs" Inherits="Reports_Reports" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" %>

<asp:Content runat="server" ID="ReportGeneralReportsContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align="center">
        <strong style="font-size:small;">��������� �������</strong><asp:GridView ID="dgvReports" runat="server" AutoGenerateColumns="False" OnRowCommand="dgvReports_RowCommand" OnRowDataBound="dgvReports_RowDataBound" OnRowDeleting="dgvReports_RowDeleting">
            <Columns>
                <asp:TemplateField HeaderText="��� ������">
                    <ItemTemplate>
                        <asp:Label ID="lblReports" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.RReportTypeName")%>'></asp:Label><asp:DropDownList ID="ddlReports" runat="server" Visible="False">
                        </asp:DropDownList>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="��� �����">
                    <ItemTemplate>
                        <asp:TextBox ID="tbCaption" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.RReportCaption")%>'></asp:TextBox><asp:RequiredFieldValidator
                            ID="rfvCaption" runat="server" ControlToValidate="tbCaption" ErrorMessage='���� "��� �����" ������ ���� ���������' ValidationGroup="vgReps">*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="�������">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbEnable" runat="server" Checked=<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.REnabled"))%> />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:HyperLinkField HeaderText="���������" Text="..." DataNavigateUrlFields="RReportCode" DataNavigateUrlFormatString="ReportProperties.aspx?rp={0}" />
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
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="�������� �����" />
			</EmptyDataTemplate>
        </asp:GridView>
        <asp:Button ID="btnApply" runat="server" Text="���������" OnClick="btnApply_Click" ValidationGroup="vgReps" /></div>
</asp:Content>