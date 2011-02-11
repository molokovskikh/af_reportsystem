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
<asp:Label ID="ErrorMassage" runat="server" Text="Label"/>
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
            <asp:CheckBox ID="chbAllow" runat="server" Text="Разрешено" 
                oncheckedchanged="chbAllow_CheckedChanged" />
        </td></tr>
    </table>
    </font>
<br />
        <br />
        <br />
        <br />
        <center><b><font size ="2"><label id="HeadLabel" >Выполнить отчет за указанный период и отослать по выбранным адресам</label></font></b></center>
        <br />
    </div>
    <div>
        <table cellspacing=0px style="background-color: rgb(235, 235, 235);">
            <tr>
                <td>
                          <asp:Label ID="Label3" runat="server" Text="Начало периода" Style="margin-left:0px;" SkinID="scheduleLabelSkin"></asp:Label>
                          <asp:Calendar id="dtFrom" runat="server" Style="margin-left:0px;" >
                                <TitleStyle BackColor="white" ForeColor="black">
                          </TitleStyle>
                         </asp:Calendar> 
                </td>
                <td>
                         <asp:Label ID="Label4" runat="server" Text="Конец периода" Style="margin-left:5px;" SkinID="scheduleLabelSkin"></asp:Label>
                         <asp:Calendar id="dtTo" runat="server" Style="margin-left:5px;">
                                <TitleStyle BackColor="white" ForeColor="black">
                          </TitleStyle>
                       </asp:Calendar> 
                </td>
            </tr>
            <tr >
                <td valign="top" style="width:325px;" colspan=2>
                        <br />
                        <asp:RadioButton ID="RadioSelf" runat="server" GroupName="Mailing"
                            Text="Выполнить и отослать на : " Checked="True" />
                        <br />
                        <asp:RadioButton ID="RadioMails" runat="server" GroupName="Mailing"
                            Text="Выполнить и выстать на указанные адреса" />
                        <br />
                        <br />
                        <asp:Label ID="Label5" runat="server" Width=420px Text="Адресаты (писать через запятую): " ></asp:Label>
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
                         TextMode=MultiLine Columns="50" Rows="6" runat=server></asp:TextBox>

    <asp:Label ID="Label7" runat="server" Width=419px Text="Например: (adr1@dom.com, adr2@dom.com, ... )"></asp:Label>
                         <br />
                         <br />
                         <br />
                                             <asp:Button ID="btn_Mailing" runat="server" Text="Выполнить" 
                        ValidationGroup="vgPassword" OnClick="btnExecute_mailing" Width="240px" />
                        
                </td>
                <td style="width: 268435488px"> 
                </td>
            </tr>
        </table>
    </div>
    <br />
    <br />
            <center><b><font size ="2"><label id="Label6" >Задать расписание для отчета</label></font></b> <br /> <br />
                <asp:Button ID="btnExecute" runat="server" Text="Выполнить задание" 
            ValidationGroup="vgPassword" OnClick="btnExecute_Click" style="height: 26px" /></center>
    <br />
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