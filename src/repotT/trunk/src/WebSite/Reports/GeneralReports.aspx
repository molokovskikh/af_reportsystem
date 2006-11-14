<%@ Page Language="C#" AutoEventWireup="true" CodeFile="GeneralReports.aspx.cs" Inherits="Reports_GeneralReports" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" %>

<asp:Content runat="server" ID="ReportGeneralReportsContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align="center">
        <strong style="font-size:small;">Настройка отчетов</strong><br/>
        <asp:GridView ID="dgvReports" runat="server" AutoGenerateColumns="False" OnRowCommand="dgvReports_RowCommand" OnRowDeleting="dgvReports_RowDeleting" OnRowDataBound="dgvReports_RowDataBound">
            <Columns>
                <asp:BoundField DataField="GRCode" HeaderText="Код" />
                <asp:TemplateField HeaderText="Клиент">
                    <ItemTemplate>
                        <asp:Label ID="lblFirmName" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.GRFirmName") %>'></asp:Label><asp:TextBox ID="tbSearch" runat="server" Width="79px" Visible="False"></asp:TextBox><asp:Button ID="btnSearch" runat="server" Text="Найти" OnClick="btnSearch_Click" Visible="False" />
                        <asp:DropDownList ID="ddlNames" runat="server" Visible="False">
                        </asp:DropDownList>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Включен">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbAllow" runat="server" Checked=<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.GRAllow"))%> />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="E-mail">
                    <ItemTemplate>
                        <asp:TextBox ID="tbEMail" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.GRAddress")%>'></asp:TextBox><asp:RegularExpressionValidator
                            ID="revEMail" runat="server" ControlToValidate="tbEMail" ErrorMessage="E-mail введен неверно"
                            ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*">*</asp:RegularExpressionValidator>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Тема письма">
                    <ItemTemplate>
                        <asp:TextBox ID="tbSubject" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.GRSubject")%>'></asp:TextBox>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Имя файла отчета">
                    <ItemTemplate>
                        <asp:TextBox ID="tbFile" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.GRFileName")%>'></asp:TextBox>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Имя архива отчета">
                    <ItemTemplate>
                        <asp:TextBox ID="tbArch" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.GRArchName")%>'></asp:TextBox>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:HyperLinkField HeaderText="Отчеты" Text="..." DataNavigateUrlFields="GRCode" DataNavigateUrlFormatString="Reports.aspx?r={0}" />
                <asp:TemplateField>
				<HeaderTemplate>
					<asp:Button ID="btnAdd" runat="server" Text="Добавить" CommandName="Add" />
				</HeaderTemplate>
				<ItemTemplate>
					<asp:Button ID="btnDelete" runat="server" Text="Удалить" CommandName="Delete" />
				</ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="GRCode" Visible="False" />
            </Columns>
   			<EmptyDataTemplate>
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="Добавить отчет" />
			</EmptyDataTemplate>
        </asp:GridView>
        <asp:Button ID="btnApply" runat="server" Text="Применить" OnClick="btnApply_Click" />
    </div>
</asp:Content>