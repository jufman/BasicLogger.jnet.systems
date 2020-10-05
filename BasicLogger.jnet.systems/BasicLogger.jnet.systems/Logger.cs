using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BasicLogger.jnet.systems
{
    public class Logger
    {
        private static List<string> ErrorCache = new List<string>();

        private static Objects.Settings _settings;
        private static bool running = true;

        private static string LogFileLocation
        {
            get
            {
                var logLocation = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";
                if (Directory.Exists(logLocation) == false)
                {
                    Directory.CreateDirectory(logLocation);
                }

                var todayLogLocation = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\" + DateTime.Now.ToString("MM-dd-yyyy");
                if (Directory.Exists(todayLogLocation) == false)
                {
                    Directory.CreateDirectory(todayLogLocation);
                }
                return todayLogLocation;
            }
        }

        public static string LogSettingLocation
        {
            get
            {
                var settingsLocation = AppDomain.CurrentDomain.BaseDirectory + "\\LogSettings.json";

                return !File.Exists(settingsLocation) ? string.Empty : settingsLocation;
            }
        }

        public static bool Load(string _logSettingLocation = "")
        {
            if (_logSettingLocation == string.Empty)
            {
                _logSettingLocation = LogSettingLocation;
            }
            LogEvent($"=============================================Logger started at: {DateTime.Now}============================================= ", LogLevel.System);

            try
            {
                _settings = JsonConvert.DeserializeObject<Objects.Settings>(File.ReadAllText(_logSettingLocation));
            }
            catch (Exception e)
            {
                LogEvent($"Logger Couldn't Load settings: {e.ToString()}", LogLevel.System);
                return false;
            }

            EmailBufferThread();
            return true;
        }

        private static void EmailBufferThread()
        {
            Task.Run(async () =>
            {
                while (running)
                {
                    await Task.Delay(_settings.EmailInterval * 1000);

                    if (ErrorCache.Count == 0)
                    {
                        continue;
                    }

                    var smtpClient = new SmtpClient(_settings.EmailServerAddress)
                    {
                        Port = _settings.Port,
                        EnableSsl = _settings.EnableSsl
                    };

                    if (_settings.RequiresAuth)
                    {
                        smtpClient.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
                    }
                    var appName = AppDomain.CurrentDomain.FriendlyName;

                    var body = $"{ErrorCache.Count} Errors have been report from {appName}.<br /><br />";
                    body += "";

                    lock (ErrorCache)
                    {
                        ErrorCache.ForEach(error => { body += $"{error} <hr noshade><br/>"; });

                        body += $"<br /><br />Log Generated at: {DateTime.Now}";

                        var mailMessage = new MailMessage
                        {
                            From = new MailAddress(_settings.SenderAddress),
                            Subject = _settings.Subject,
                            Body = body,
                            IsBodyHtml = true
                        };

                        _settings.EmailAddresses.ForEach(address => { mailMessage.To.Add(address); });

                        try
                        {
                            smtpClient.Send(mailMessage);
                        }
                        catch (Exception e)
                        {
                            LogEvent($"Logger Couldn't send Email: {e.ToString()}", LogLevel.System);
                        }
                        
                        ErrorCache.Clear();
                    }
                }
            });
        }

        public static void LogEvent(string Message, LogLevel LogLevel)
        {
            var errorMessage = $@"LogLevel: {LogLevel.ToString()}, {Message} {Environment.NewLine}";
            File.AppendAllText(LogFileLocation + "\\Log.txt", errorMessage);

            if (_settings != null && _settings.SendEmailAlerts && (int)LogLevel >= _settings.EmailAlertLogLevel)
            {
                ErrorCache.Add(errorMessage);
            }
        }

        public enum LogLevel
        {
            System = -1,
            Info,
            Error,
            Critical
        }

    }
}
