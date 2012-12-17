<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_Reports" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="Reports.aspx.cs" %>
<asp:Content runat="server" ID="ReportGeneralReportsContent" ContentPlaceHolderID="ReportContentPlaceHolder">
	<div align="center">
		<strong style="font-size:small;">��������� �������������� ����������</strong> <br/>
	</div>
	<div align="center">
	  <table >
		<tr bgcolor="#eef8ff">
		  <td align="right">
			<asp:Label ID="lblEMailSubject" runat="server" Text="���� ������:" SkinID="paramLabelSkin"/>
		  </td>
		  <td> 
			<asp:TextBox ID="tbEMailSubject" runat="server" SkinID="paramTextBoxSkin"/>
		  </td>
		</tr>
		<tr bgcolor="#f6f6f6">
		  <td align="right">
			<asp:Label ID="lblReportFileName" runat="server" Text="��� ����� ������:" SkinID="paramLabelSkin"/>
		  </td>
		  <td> 
			<asp:TextBox ID="tbReportFileName" runat="server" SkinID="paramTextBoxSkin"/>
		  </td>
		</tr>
		<tr bgcolor="#eef8ff">
		  <td align="right">
			<asp:Label ID="lblReportArchName" runat="server" Text="��� ������ ������:" SkinID="paramLabelSkin"/>
		  </td>
		  <td> 
			<asp:TextBox ID="tbReportArchName" runat="server" SkinID="paramTextBoxSkin"/>
		  </td>
		</tr>
		<tr bgcolor="#eef8ff">
		  <td align="right">
			<asp:Label ID="ReportFormatLbl" runat="server" Text="������ ������:" SkinID="paramLabelSkin"/>
		  </td>
		  <td> 
			<asp:DropDownList ID="ReportFormatDD" runat="server" Width="100%">
				<asp:ListItem Text="Excel" Value="Excel" />
				<asp:ListItem Text="DBF" Value="DBF" />
				<asp:ListItem Text="CSV" Value="CSV" />
			</asp:DropDownList>
		  </td>
		</tr>
		<tr bgcolor="#eef8ff">
			<td align="right">
				<asp:Label ID="noArchiveText" runat="server" Text="�� ������������:" SkinID="paramLabelSkin"></asp:Label>
		  </td>
		  <td>
			<asp:CheckBox runat="server" ID="NoArchive"/>
		  </td>
		</tr>
		<tr bgcolor="#eef8ff">
			<td align="right">
				<asp:Label ID="SendDescriptionFileLabel" runat="server" Text="������� � ������ ����� � ���������:" SkinID="paramLabelSkin"></asp:Label>
			</td>
		<td>
			<asp:CheckBox runat="server" ID="SendDescriptionFile" ClientIDMode="Static"/>
		</td>
		</tr>
		<tr>
			<td colspan="2" class="topPadding">������� � ������ � ������� �����:</td>
		</tr>
		<tr>
			<td colspan="2">
				<asp:GridView ID="fileGridView" runat="server" AutoGenerateColumns="False" OnRowCommand="filesDataGridView_RowCommand" OnRowDeleting="filesDataGridView_RowDeleting">
				<Columns>
					<asp:TemplateField HeaderText="������">
						<ItemTemplate>
							<asp:HyperLink runat="server" ID="FileName" Text='<%# DataBinder.Eval(Container, "DataItem.FileName") %>'
						 NavigateUrl='<%# DataBinder.Eval(Container, "DataItem.NavigateUrl") %>'></asp:HyperLink>
						</ItemTemplate>
					</asp:TemplateField>
					<asp:TemplateField HeaderText="����� �����">
					<ItemTemplate>
						<asp:HiddenField runat="server" ID="Id" Value='<%# DataBinder.Eval(Container, "DataItem.Id") %>'/>
						<asp:FileUpload runat="server" ID="UploadFile"/>
					</ItemTemplate>
					</asp:TemplateField>
					<asp:TemplateField>
						<HeaderTemplate>
							<asp:Button ID="btnAdd" runat="server" Text="��������" CommandName="Add" />
						</HeaderTemplate>
						<ItemTemplate>
							<asp:Button ID="btnDelete" CssClass="deleteFileButton" runat="server" Text="�������" CommandName="Delete" />
						</ItemTemplate>
						</asp:TemplateField>
					</Columns>
					<EmptyDataTemplate>
						<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="�������� ����" />
					</EmptyDataTemplate>
				</asp:GridView>
			</td>
		</tr>
	</table>
	</div>
	<div>
		<asp:HyperLink runat="server" ID="SheduleLink">����������</asp:HyperLink>
	</div>
	<div align="center">
		<strong style="font-size:small;">��������� �������</strong>
		<asp:Label ID="lblMessage" runat="server" Text="" /><br/><br/>
		<asp:Label ID="lblFilter" runat="server" Text="������:" />
		<asp:TextBox ID="tbFilter" runat="server" SkinID="paramTextBoxSkin" ClientIDMode="Static"
			ontextchanged="btnFilter_Click" ToolTip="������� ��� ����� ��� ����� ����� ��� ������ �� ���������"/>
		<asp:Button ID="btnFilter" runat="server" Text="�����������" ClientIDMode="Static"
			onclick="btnFilter_Click" />
			<br/><br/>

		<asp:GridView ID="dgvReports" runat="server" AutoGenerateColumns="False" OnRowCommand="dgvReports_RowCommand" OnRowDataBound="dgvReports_RowDataBound" OnRowDeleting="dgvReports_RowDeleting">
			<Columns>
				<asp:TemplateField HeaderText="��� ������">
					<ItemTemplate>
						<asp:Label ID="lblReports" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.RReportTypeName") %>'></asp:Label><asp:DropDownList ID="ddlReports" runat="server" Visible="False">
						</asp:DropDownList>
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="��� �����">
					<ItemTemplate>
						<asp:TextBox ID="tbCaption" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.RReportCaption") %>'></asp:TextBox>
						<asp:RequiredFieldValidator ID="rfvCaption" runat="server" ControlToValidate="tbCaption" 
							ErrorMessage='���� "��� �����" ������ ���� ���������' ValidationGroup="vgReps">*</asp:RequiredFieldValidator>
						<asp:CustomValidator ControlToValidate="tbCaption" 
							ErrorMessage="<br/>����� ����� ���������� ���, ��� �����������" runat="server" EnableClientScript="false"
							ID="ServerValidator" onservervalidate="ServerValidator_ServerValidate" Display="Dynamic"/>
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="�������">
					<ItemTemplate>
						<asp:CheckBox ID="chbEnable" runat="server" Checked=<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.REnabled")) %> />
					</ItemTemplate>
				</asp:TemplateField>
				<asp:HyperLinkField HeaderText="���������" Text="..." DataNavigateUrlFields="RReportCode" DataNavigateUrlFormatString="ReportProperties.aspx?rp={0}" />
				<asp:TemplateField>
				<HeaderTemplate>
					<asp:Button ID="btnAdd" runat="server" Text="��������" CommandName="Add" />
				</HeaderTemplate>
				<ItemTemplate>
					<asp:HiddenField runat="server" ID="Id" Value='<%# DataBinder.Eval(Container, "DataItem.RReportCode") %>'/>
					<asp:Button ID="btnDelete" runat="server" Text="�������" CommandName="Delete" />
					<asp:Button ID="btnCopy" runat="server" Text="����������" CommandName="Copy" Visible=<%# (DataBinder.Eval(Container, "DataItem.RReportCode") != DBNull.Value) %>/>
					<asp:Button ID="btnCopyToOther" runat="server" Text="����������� � ��.�����" CommandName="CopyTo" Visible=<%# (DataBinder.Eval(Container, "DataItem.RReportCode") != DBNull.Value) %>/>
				</ItemTemplate>
				</asp:TemplateField>
			</Columns>
			<EmptyDataTemplate>
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="�������� �����" />
			</EmptyDataTemplate>
		</asp:GridView>
		<asp:Button ID="btnApply" runat="server" Text="���������" OnClick="btnApply_Click" ValidationGroup="vgReps" /></div>
</asp:Content>