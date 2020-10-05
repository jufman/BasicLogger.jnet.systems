using System;
using System.Collections.Generic;

namespace BasicLogger.jnet.systems.Objects
{
    public class Settings
    {
        public bool SendEmailAlerts { get; set; }

        public int EmailAlertLogLevel { get; set; } = 2;

        public int EmailInterval { get; set; } = 180;

        public List<string> EmailAddresses { get; set; } = new List<string>();

        public string EmailServerAddress { get; set; }
        public string SenderAddress { get; set; }

        public bool RequiresAuth { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;

        public string Subject { get; set; } = $"Error Log From {AppDomain.CurrentDomain.FriendlyName}";
    }
}
