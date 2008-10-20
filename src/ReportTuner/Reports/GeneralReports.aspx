<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_GeneralReports" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="GeneralReports.aspx.cs" %>

<asp:Content runat="server" ID="ReportGeneralReportsContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align="center">
        <strong style="font-size:small;">Настройка отчетов</strong><br/>
        <asp:GridView ID="dgvReports" runat="server" AutoGenerateColumns="False" OnRowCommand="dgvReports_RowCommand" OnRowDeleting="dgvReports_RowDeleting" OnRowDataBound="dgvReports_RowDataBound" style="table-layout:fixed;">
            <Columns>
                <asp:BoundField DataField="GRCode" HeaderText="Код" ItemStyle-Width="3%" HeaderStyle-Width="3%"/>
                <asp:TemplateField HeaderText="Клиент" ItemStyle-Width="10%" HeaderStyle-Width="10%" ItemStyle-Wrap="true">
                    <ItemTemplate>
                        <asp:Label ID="lblFirmName" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.GRFirmName") %>'></asp:Label><asp:TextBox ID="tbSearch" runat="server" Width="79px" Visible="False"></asp:TextBox><asp:Button ID="btnSearch" runat="server" Text="Найти" OnClick="btnSearch_Click" Visible="False" />
                        <asp:DropDownList ID="ddlNames" runat="server" Visible="False">
                        </asp:DropDownList>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Включен" ItemStyle-Width="5%" HeaderStyle-Width="5%">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbAllow" runat="server" Checked='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.GRAllow"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:HyperLinkField HeaderText="Рассылки" Text="..." DataNavigateUrlFields="GRCode" DataNavigateUrlFormatString="Contacts.aspx?GeneralReport={0}" ItemStyle-Width="5%" HeaderStyle-Width="5%"/>
<%--                <asp:TemplateField HeaderText="E-mail" ItemStyle-Width="10%" HeaderStyle-Width="10%">
                    <ItemTemplate>
                        <asp:TextBox ID="tbEMail" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.GRAddress")%>'></asp:TextBox>                        
                        <asp:RegularExpressionValidator ID="revEMail" runat="server" ControlToValidate="tbEMail" ErrorMessage="E-mail введен неверно" ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*(\s*\,\s*\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*)*">*</asp:RegularExpressionValidator>
                        <!-- ~/noticias.asp?id= DataBinder.Eval(Container.DataItem, "IdSiteNoticias") -->
                        <asp:LinkButton ID="lbContacts" runat="server" Text="..." PostBackUrl='<%#DataBinder.Eval(Container.DataItem, "GRCode", "Contacts.aspx?GeneralReport={0}")%> '></asp:LinkButton>
                    </ItemTemplate>
                </asp:TemplateField>
--%>                <asp:TemplateField HeaderText="Тема письма" ItemStyle-Width="35%" HeaderStyle-Width="35%">
                    <ItemTemplate>
                        <asp:TextBox ID="tbSubject" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.GRSubject")%>'></asp:TextBox>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Имя файла отчета" ItemStyle-Width="5%" HeaderStyle-Width="5%">
                    <ItemTemplate>
                        <asp:TextBox ID="tbFile" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.GRFileName")%>'></asp:TextBox>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Имя архива отчета" ItemStyle-Width="5%" HeaderStyle-Width="5%">
                    <ItemTemplate>
                        <asp:TextBox ID="tbArch" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.GRArchName")%>'></asp:TextBox>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:HyperLinkField HeaderText="Отчеты" Text="..." DataNavigateUrlFields="GRCode" DataNavigateUrlFormatString="Reports.aspx?r={0}" ItemStyle-Width="5%" HeaderStyle-Width="5%"/>
                <asp:HyperLinkField HeaderText="Расписание" Text="..." DataNavigateUrlFields="GRCode" DataNavigateUrlFormatString="Schedule.aspx?r={0}" ItemStyle-Width="5%" HeaderStyle-Width="6%"/>
                <asp:TemplateField ItemStyle-Width="6%" HeaderStyle-Width="6%">
				    <HeaderTemplate>
					    <asp:Button ID="btnAdd" runat="server" Text="Добавить" CommandName="Add" />
				    </HeaderTemplate>
				    <ItemTemplate>
					    <asp:Button ID="btnDelete" runat="server" Text="Удалить" CommandName="Delete" />
				    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
   			<EmptyDataTemplate>
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="Добавить отчет" />
			</EmptyDataTemplate>
        </asp:GridView>
        <asp:Button ID="btnApply" runat="server" Text="Применить" OnClick="btnApply_Click" />
    </div>
</asp:Content>