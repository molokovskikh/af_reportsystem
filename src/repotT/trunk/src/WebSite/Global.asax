<%@ Application Language="C#" %>

<script runat="server">

    void Application_Start(object sender, EventArgs e) 
    {
        // Code that runs on application startup

    }
    
    void Application_End(object sender, EventArgs e) 
    {
        //  Code that runs on application shutdown

    }
        
    void Application_Error(object sender, EventArgs e) 
    { 
        // Code that runs when an unhandled error occurs
        bool sessionExists = false;
        try
        {
            sessionExists = this.Session != null;
        }
        catch
        {
            sessionExists = false;
        }

        //При создании сессии происходит редирект на logon.aspx и в этом случае возникает ошибка
        //Если сессия не существует, то это именно та ошибка и мы это проверям
        if (sessionExists)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("----Url-------");
            builder.AppendLine(Request.Url.ToString());
            builder.AppendLine("--------------");
            builder.AppendLine("----Params----");
            foreach (string name in Request.QueryString)
                builder.AppendLine(String.Format("{0}: {1}", name, Request.QueryString[name]));
            builder.AppendLine("--------------");

            builder.AppendLine("----Error-----");
            Exception exception = Server.GetLastError();
            do
            {
                builder.AppendLine("Message:");
                builder.AppendLine(exception.Message);
                builder.AppendLine("Stack Trace:");
                builder.AppendLine(exception.StackTrace);
                builder.AppendLine("--------------");
                exception = exception.InnerException;
            } while (exception != null);
            builder.AppendLine("--------------");

            builder.AppendLine("----Session---");
            foreach (string key in Session.Keys)
            {
                if (Session[key] == null)
                    builder.AppendLine(String.Format("{0} - null", key));
                else
                    builder.AppendLine(String.Format("{0} - {1}", key, Session[key].GetType().ToString()));
            }
            builder.AppendLine("--------------");

            //builder.AppendLine(String.Format("Version : {0}", Assembly.GetExecutingAssembly().GetName().Version));

            System.Net.Mail.MailMessage m = new System.Net.Mail.MailMessage("service@analit.net", "service@analit.net", "Ошибка в интерфейсе настройки отчета", builder.ToString());
            m.BodyEncoding = Encoding.UTF8;

            System.Net.Mail.SmtpClient c = new System.Net.Mail.SmtpClient("box.analit.net");
            c.Send(m);
        }
    }

    void Session_Start(object sender, EventArgs e) 
    {
        // Code that runs when a new session is started

    }

    void Session_End(object sender, EventArgs e) 
    {
        // Code that runs when a session ends. 
        // Note: The Session_End event is raised only when the sessionstate mode
        // is set to InProc in the Web.config file. If session mode is set to StateServer 
        // or SQLServer, the event is not raised.

    }
       
</script>
