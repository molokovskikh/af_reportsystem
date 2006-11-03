<%@ Page Language="C#" AutoEventWireup="true" CodeFile="GeneralReports.aspx.cs" Inherits="Reports_GeneralReports" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" %>

<asp:Content runat="server" ID="ReportGeneralReportsContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align="center">
        <strong style="font-size:small;">��������� �������</strong><br/>
        <asp:GridView ID="dgvReports" runat="server" AutoGenerateColumns="False" OnRowCommand="dgvReports_RowCommand" OnRowDeleting="dgvReports_RowDeleting" OnRowDataBound="dgvReports_RowDataBound">
            <Columns>
                <asp:TemplateField HeaderText="������">
                    <ItemTemplate>
                        <asp:TextBox ID="tbSearch" runat="server" Width="79px"></asp:TextBox>
                        <asp:Button ID="btnSearch" runat="server" Text="�����" OnClick="btnSearch_Click" />
                        <asp:DropDownList ID="ddlNames" runat="server">
                        </asp:DropDownList>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="�������">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbAllow" runat="server" Checked=<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.GRAllow"))%> />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="E-mail">
                    <ItemTemplate>
                        <asp:TextBox ID="tbEMail" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.GRAddress")%>'></asp:TextBox><asp:RequiredFieldValidator
                            ID="rfvEMail" runat="server" ControlToValidate="tbEMail" ErrorMessage='���� "E-mail" ������ ���� ���������'>*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="���� ������">
                    <ItemTemplate>
                        <asp:TextBox ID="tbSubject" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.GRSubject")%>'></asp:TextBox><asp:RequiredFieldValidator
                            ID="rfvSubject" runat="server" ControlToValidate="tbSubject" ErrorMessage='���� "���� ������" ������ ���� ���������'>*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="��� ����� ������">
                    <ItemTemplate>
                        <asp:TextBox ID="tbFile" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.GRFileName")%>'></asp:TextBox><asp:RequiredFieldValidator
                            ID="rfvFile" runat="server" ControlToValidate="tbFile" ErrorMessage='���� "��� ����� ������" ������ ���� ���������'>*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="��� ������ ������">
                    <ItemTemplate>
                        <asp:TextBox ID="tbArch" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.GRArchName")%>'></asp:TextBox><asp:RequiredFieldValidator
                            ID="rfvArch" runat="server" ControlToValidate="tbArch" ErrorMessage='���� "��� ������ ������" ������ ���� ���������'>*</asp:RequiredFieldValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:HyperLinkField HeaderText="������" Text="..." DataNavigateUrlFields="GRRTCode" DataNavigateUrlFormatString="Reports.aspx?r={0}" />
                <asp:TemplateField>
				<HeaderTemplate>
					<asp:Button ID="btnAdd" runat="server" Text="��������" CommandName="Add" />
				</HeaderTemplate>
				<ItemTemplate>
					<asp:Button ID="btnDelete" runat="server" Text="�������" CommandName="Delete" />
				</ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="GRRTCode" Visible="False" />
            </Columns>
   			<EmptyDataTemplate>
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="�������� �����" />
			</EmptyDataTemplate>
        </asp:GridView>
        <asp:Button ID="btnApply" runat="server" Text="���������" OnClick="btnApply_Click" />
    </div>
</asp:Content>