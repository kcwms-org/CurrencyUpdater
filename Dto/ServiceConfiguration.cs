using System;
using System.Collections.Generic;
using System.Text;

namespace Dto
{

    public class Serviceconfiguration
    {
        public int MinutesToWaitBetweenExecutions { get; set; }
        public string databaseConnectionString { get; set; }
        public int sqlCommandTimeout { get; set; }
        public EmailSettings ErrorEmailConfiguration { get; set; }
        public SMTPSettings SMTPSettings { get; set; }
    }

    public class EmailSettings
    {
        public IEnumerable<string> To { get; set; }
        public IEnumerable<string> CC { get; set; }
        public string Subject { get; set; }
        public string FromAddress { get; set; }
    }

    public class SMTPSettings
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public bool UseSSL { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
