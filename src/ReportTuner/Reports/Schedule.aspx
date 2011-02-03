<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_schedule" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="Schedule.aspx.cs" %>

<asp:Content runat="server" ID="ScheduleValuesContent" ContentPlaceHolderID="ReportContentPlaceHolder">


<script type="text/javascript">

$().ready(function() {
	$("#form1").validate(
    {
    errorLabelContainer: $("#form1 div.errorjava")
    });
});

    jQuery.validator.addMethod(
	"regexp",
	function (value, element, regexp) {
	    var re = new RegExp(regexp);
	    return this.optional(element) || re.test(value);
	},
	"Please check your input."
);


    $("#form1").validate({
	rules: {
		mail_Text: {
			required: true,
			minlength: 5,
			regexp: "^[a-zA-Z0-9_]+$"
		},
	messages: {
		mail_Text: {
			required: "Введите логин",
			minlength: "Минимальная длина логина 5 символов",
			regexp: "Логин может состоять только из латинских букв, цифр и знака подчеркивания"
		}
});


</script>
    <div align="center"><strong><font size ="2">        
Задание для отчета "<asp:Label ID="lblReportComment" runat="server" Text="Label"/>" для плательщика "<asp:Label ID="lblClient" runat="server" Text="Label"/>"<br /><br />
</font></strong></div>
    <div><font size ="2">
    <table width="100%">
        <tr bgcolor="#eef8ff"><td>
            <asp:Label ID="Label2" runat="server" Text="Выполнить:" SkinID="scheduleLabelSkin"></asp:Label>
            <asp:Label ID="lblWork" runat="server" Text="Label"></asp:Label>
        </td></tr>
        <tr bgcolor="#f6f6f6"><td>
            <asp:Label ID="Label1" runat="server" Text="Рабочая папка:" SkinID="scheduleLabelSkin"></asp:Label>
            <asp:Label ID="lblFolder" runat="server" Text="Label"></asp:Label>
        </td></tr>
        <tr bgcolor="#eef8ff"><td>
            <asp:CheckBox ID="chbAllow" runat="server" Text="Разрешено" />
        </td></tr>
    </table>
    </font>
    <asp:Button ID="btnExecute" runat="server" Text="Выполнить задание" ValidationGroup="vgPassword" OnClick="btnExecute_Click" /><br /><br />
    </div>
    <div>
        <table>
            <tr>
                <td>
                    <asp:Label ID="Label3" runat="server" Text="Начало периода" SkinID="scheduleLabelSkin"></asp:Label>
                          <asp:Calendar id="dtFrom" runat="server" style="margin-right: 0px">
                                <TitleStyle BackColor="white" ForeColor="black">
                          </TitleStyle>
                         </asp:Calendar> 

                </td>
                <td >
                    <asp:Label ID="Label4" runat="server" style="margin-left:5px;" Text="Конец периода" SkinID="scheduleLabelSkin"></asp:Label>
                         <asp:Calendar id="dtTo" style="margin-left:5px;" runat="server">
                                <TitleStyle BackColor="white" ForeColor="black">
                          </TitleStyle>
                         </asp:Calendar> 
                </td>
            </tr>
            <tr style="background-color: rgb(235, 235, 235);">
                <td >
                    <asp:Button ID="Button1" runat="server" Text="Выполнить и выслать в рассылку" 
                        ValidationGroup="vgPassword" OnClick="btnExecute_Click_mailing" Width="232px" />
                </td>
                <td >
                <center>
                        <asp:Button ID="Button2" runat="server" Text="Выполнить для себя" ValidationGroup="vgPassword" OnClick="btnExecute_Click_self" />
               </center>
                </td>
                <td style="width:300px;">
                        <asp:Button ID="Button3" runat="server" ValidationGroup="mail_Text_Group" Text="Выполнить для указанных" OnClick="btnExecute_Click_Email" /><br />
                        <asp:Label ID="Label5" runat="server" Width=74px Text="Адресаты: " 
                            SkinID="scheduleLabelSkin"></asp:Label>
            <br />
          <asp:RegularExpressionValidator ID="RegularExpressionValidator1" 
   runat="server" ErrorMessage="Введите список EMail" 
   Display="Dynamic"
    ControlToValidate="mail_Text" ValidationExpression="^\s*\w[\w\.\-]*[@]\w[\w\.\-]*([.]([\w]{1,})){1,3}\s*(\,\s*\w[\w\.\-]*[@]\w[\w\.\-]*([.]([\w]{1,})){1,3}\s*)*$" 
    ValidationGroup="mail_Text_Group"></asp:RegularExpressionValidator>

  <asp:RequiredFieldValidator ID="RequiredFieldValidator2" 
  Display="Dynamic"
   runat="server" ErrorMessage="Не задан список" ControlToValidate="mail_Text"
   ValidationGroup="mail_Text_Group"></asp:RequiredFieldValidator>


                        <asp:TextBox ID="mail_Text" runat="server" style="background-color: white;
                             border-color:black; border-width:1px; color: black;"
                         TextMode=MultiLine Columns="50" Rows="6" runat=server> </asp:TextBox>
                </td>
            </tr>
        </table>
    </div>
    <div align="center">
        <asp:GridView ID="dgvSchedule" runat="server" AutoGenerateColumns="False" Caption="Расписание" OnRowCommand="dgvSchedule_RowCommand" OnRowDeleting="dgvSchedule_RowDeleting" OnRowDataBound="dgvSchedule_RowDataBound">
            <Columns>
                <asp:TemplateField HeaderText="Время начала">
                    <ItemTemplate>
                        <asp:TextBox ID="tbStart" runat="server" ></asp:TextBox>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Пн">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbMonday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SMonday"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Вт">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbTuesday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.STuesday"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Ср">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbWednesday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SWednesday"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Чт">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbThursday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SThursday"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Пт">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbFriday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SFriday"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Сб">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbSaturday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SSaturday"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Вс">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbSunday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SSunday"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField>
				<HeaderTemplate>
					<asp:Button ID="btnAdd" runat="server" Text="Добавить" CommandName="Add" />
				</HeaderTemplate>
				<ItemTemplate>
					<asp:Button ID="btnDelete" runat="server" Text="Удалить" CommandName="Delete" />
				</ItemTemplate>
                </asp:TemplateField>
            </Columns>
   			<EmptyDataTemplate>
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="Добавить расписание" />
			</EmptyDataTemplate>
        </asp:GridView>
        <br/>
        <asp:GridView ID="gvOtherTriggers" runat="server" 
            Caption="Дополнительное расписание" AutoGenerateColumns="False" >
            <Columns>
                <asp:BoundField DataField="!" HeaderText="Описание" />
            </Columns>
        </asp:GridView>
        <br/>
        <div align="center" style="width:70%;">
        <asp:GridView ID="gvLogs" runat="server" 
            Caption="Статистика выполнения отчета" AutoGenerateColumns="False"  EmptyDataText="Нет данных">
            <Columns>
                <asp:BoundField DataField="LogTime" HeaderText="Дата" />
                <asp:BoundField DataField="EMail" HeaderText="EMail" />
                <asp:BoundField DataField="SMTPID" HeaderText="SMTPID" />
            </Columns>
        </asp:GridView>
        </div>
        <br/>
        <asp:Button ID="btnApply" runat="server" Text="Применить" OnClick="btnApply_Click" ValidationGroup="vgPassword" />
    </div>
</asp:Content>