<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ReportProperties.aspx.cs" Inherits="Reports_ReportProperties" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" %>

<asp:Content runat="server" ID="ReportPropertiesContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align="center">
        <strong style="font-size:small;">��������� ���������� ������ "<asp:Label ID="lblReport" runat="server"
            Text="Label"></asp:Label>"</strong><br/>
        <asp:GridView ID="dgvNonOptional" runat="server" AutoGenerateColumns="False" OnRowDataBound="dgvNonOptional_RowDataBound">
            <Columns>
                <asp:BoundField DataField="PParamName" HeaderText="������������ ���������" />
                <asp:TemplateField HeaderText="��������">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbValue" runat="server" Visible="False" />
                        <asp:TextBox ID="tbValue" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.PPropertyValue")%>'></asp:TextBox><asp:RequiredFieldValidator
                            ID="rfvValue" runat="server" ControlToValidate="tbValue" ErrorMessage='���� "��������" ������ ���� ���������'>*</asp:RequiredFieldValidator>
                        <asp:DropDownList ID="ddlValue" runat="server" Visible="False">
                        </asp:DropDownList>
                        <asp:Label ID="lblType" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.PPropertyType") %>'  Visible="False"></asp:Label>
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