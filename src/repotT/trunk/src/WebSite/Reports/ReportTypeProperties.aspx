<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ReportTypeProperties.aspx.cs" Inherits="Reports_ReportTypeProperties" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" %>

<asp:Content runat="server" ID="ReportTypePropertiesContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align=center>
            <strong style="font-size:small;">��������� ���������� ������&nbsp;"<asp:Label ID="lblReportName" runat="server" Font-Bold="True"></asp:Label>"</strong><br />
            <asp:GridView ID="dgvProperties" runat="server" AutoGenerateColumns="False" OnRowCommand="dgvProperties_RowCommand" OnRowDataBound="dgvProperties_RowDataBound" OnRowDeleting="dgvProperties_RowDeleting">
                <Columns>
                    <asp:BoundField DataField="PID" Visible="False" />
                    <asp:BoundField DataField="PRTCode" Visible="False" />
                    <asp:TemplateField HeaderText="������������">
                        <ItemTemplate>
                            <asp:TextBox ID="tbName" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.PName")%>'></asp:TextBox>
                            <asp:RequiredFieldValidator ID="rtbName" runat="server" ControlToValidate="tbName"
                                ErrorMessage='���� "������������" ������ ���� ���������'>*</asp:RequiredFieldValidator>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="������������ ������������">
                        <ItemTemplate>
                            <asp:TextBox ID="tbDisplayName" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.PDisplayName")%>'></asp:TextBox>
                            <asp:RequiredFieldValidator ID="rtbDisplayName" runat="server" ControlToValidate="tbDisplayName"
                                ErrorMessage='���� "������������ ������������" ������ ���� ���������'>*</asp:RequiredFieldValidator>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="���">
                        <ItemTemplate>
                            <asp:DropDownList ID="ddlType" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlType_SelectedIndexChanged">
                            </asp:DropDownList>
                            <asp:DropDownList ID="ddlEnum" runat="server">
                            </asp:DropDownList>
                            <asp:Button ID="btnEditType" runat="server" Font-Bold="True" Height="22px" OnClick="btnEditType_Click"
                                Text=">>" Width="22px" />
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="�������� �� ���������">
                        <ItemTemplate>
                            <asp:TextBox ID="tbDefault" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.PDefaultValue")%>'></asp:TextBox><asp:RequiredFieldValidator
                                ID="rfvDefault" runat="server" ControlToValidate="tbDefault" ErrorMessage='���� "�������� �� ���������" ������ ���� ���������'>*</asp:RequiredFieldValidator>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="PEnumID" Visible="False" />
                    <asp:TemplateField HeaderText="������������">
                        <ItemTemplate>
                            <asp:CheckBox ID="chbOptional" runat="server" Checked=<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.POptional"))%> />
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="�������� ���������">
                        <ItemTemplate>
                            <asp:TextBox ID="tbProc" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.PStoredProc")%>'></asp:TextBox>
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
            <asp:Button ID="btnApply" runat="server" Text="���������" OnClick="btnApply_Click" />
    </div>
</asp:Content>