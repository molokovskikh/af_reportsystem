<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_Reports" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="Reports.aspx.cs" %>

<asp:Content runat="server" ID="ReportGeneralReportsContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align="center">
        <strong style="font-size:small;">Настройка дополнительных параметров</strong> <br/>
    </div>
    <div align="center">
      <table >
        <tr bgcolor="#eef8ff">
          <td align="right">
            <asp:Label ID="lblEMailSubject" runat="server" Text="Тема письма:" SkinID="paramLabelSkin"/>
          </td>
          <td> 
            <asp:TextBox ID="tbEMailSubject" runat="server" SkinID="paramTextBoxSkin"/>
          </td>
        </tr>
        <tr bgcolor="#f6f6f6">
          <td align="right">
            <asp:Label ID="lblReportFileName" runat="server" Text="Имя файла отчета:" SkinID="paramLabelSkin"/>
          </td>
          <td> 
            <asp:TextBox ID="tbReportFileName" runat="server" SkinID="paramTextBoxSkin"/>
          </td>
        </tr>
        <tr bgcolor="#eef8ff">
          <td align="right">
            <asp:Label ID="lblReportArchName" runat="server" Text="Имя архива отчета:" SkinID="paramLabelSkin"/>
          </td>
          <td> 
            <asp:TextBox ID="tbReportArchName" runat="server" SkinID="paramTextBoxSkin"/>
          </td>
        </tr>
        <tr bgcolor="#f6f6f6">
          <td align="right">
            <asp:Label ID="lblReportRecipient" runat="server" Text="Получатель отчета:" SkinID="paramLabelSkin"/>
          </td>
          <td> 
            <asp:DropDownList ID="Recipients" runat="server" Width="100%"/>
          </td>
        </tr>
        <tr bgcolor="#eef8ff">
          <td align="right">
            <asp:Label ID="ReportFormatLbl" runat="server" Text="Формат отчета:" SkinID="paramLabelSkin"/>
          </td>
          <td> 
            <asp:DropDownList ID="ReportFormatDD" runat="server" Width="100%">
				<asp:ListItem Text="Excel" Value="Excel" />
				<asp:ListItem Text="DBF" Value="DBF" />
            </asp:DropDownList>
          </td>
        </tr>
		<tr bgcolor="#eef8ff">
			<td align="right">
		  		<asp:Label ID="noArchiveText" runat="server" Text="Не архивировать:" SkinID="paramLabelSkin"></asp:Label>
		  </td>
		  <td>
		  	<asp:CheckBox runat="server" ID="NoArchive"/>
		  </td>
		</tr>
    </table>
    </div>
	<div>
		<asp:HyperLink runat="server" ID="SheduleLink">Расписание</asp:HyperLink>
	</div>
    <div align="center">
        <strong style="font-size:small;">Настройка отчетов</strong>
        <asp:GridView ID="dgvReports" runat="server" AutoGenerateColumns="False" OnRowCommand="dgvReports_RowCommand" OnRowDataBound="dgvReports_RowDataBound" OnRowDeleting="dgvReports_RowDeleting">
            <Columns>
                <asp:TemplateField HeaderText="Тип отчета">
                    <ItemTemplate>
                        <asp:Label ID="lblReports" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.RReportTypeName")%>'></asp:Label><asp:DropDownList ID="ddlReports" runat="server" Visible="False">
                        </asp:DropDownList>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Имя листа">
                    <ItemTemplate>
                        <asp:TextBox ID="tbCaption" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.RReportCaption")%>'></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvCaption" runat="server" ControlToValidate="tbCaption" 
							ErrorMessage='Поле "Имя листа" должно быть заполнено' ValidationGroup="vgReps">*</asp:RequiredFieldValidator>
						<asp:CustomValidator ControlToValidate="tbCaption" 
							ErrorMessage="<br/>Листы имеют одинаковое имя, что недопустимо" runat="server" EnableClientScript="false"
							ID="ServerValidator" onservervalidate="ServerValidator_ServerValidate" Display="Dynamic"/>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Включен">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbEnable" runat="server" Checked=<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.REnabled"))%> />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:HyperLinkField HeaderText="Параметры" Text="..." DataNavigateUrlFields="RReportCode" DataNavigateUrlFormatString="ReportProperties.aspx?rp={0}" />
                <asp:TemplateField>
				<HeaderTemplate>
					<asp:Button ID="btnAdd" runat="server" Text="Добавить" CommandName="Add" />
				</HeaderTemplate>
				<ItemTemplate>
					<asp:Button ID="btnDelete" runat="server" Text="Удалить" CommandName="Delete" />
					<asp:Button ID="btnCopy" runat="server" Text="Копировать" CommandName="Copy" Visible=<%#(DataBinder.Eval(Container, "DataItem.RReportCode") != DBNull.Value)%>/>
				</ItemTemplate>
                </asp:TemplateField>
            </Columns>
            <EmptyDataTemplate>
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="Добавить отчет" />
			</EmptyDataTemplate>
        </asp:GridView>
        <asp:Button ID="btnApply" runat="server" Text="Применить" OnClick="btnApply_Click" ValidationGroup="vgReps" /></div>
</asp:Content>