<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_error" Codebehind="error.aspx.cs" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Ошибка в интерфейсе настройки отчетов</title>
</head>
<body bgcolor="#ffffff">
    <form id="Form1" method="post" runat="server">
		<div style="width:100%; text-align:center;">
			<h3 style="color: red">
			Ошибка в интерфейсе настройки отчетов
			</h3>
			<p>
			Сообщение об ошибке было отправленно разработчикам. Повторите ваши действия.
			</p>
			<asp:Button ID="BackButton" runat="server" Text="Вернуться на страницу" OnClick="BackButton_Click" />
        </div>
    </form>
</body>
</html>


