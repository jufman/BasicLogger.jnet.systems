using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Threading.Tasks;
using BasicLogger.jnet.systems.Interfaces;
using BasicLogger.jnet.systems.Objects;
using Newtonsoft.Json;

namespace BasicLogger.jnet.systems
{
    public class Logger
    {
        private static List<Event> EmailErrorCache = new List<Event>();

        private static List<Event> ErrorCache = new List<Event>();

        private static Objects.Settings _settings;
        private static bool running = true;

        private static string PluginLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

        private static List<ILoggerPlugin> ILoggerPlugins = new List<ILoggerPlugin>();

        private static string LogFileLocation
        {
            get
            {
                var BaseFolder = AppDomain.CurrentDomain.BaseDirectory;
                if (_settings != null && _settings.LogFolderLocation != string.Empty)
                {
                    BaseFolder = _settings.LogFolderLocation;
                }

                var logLocation = Path.Combine(BaseFolder, "Logs\\");
                if (Directory.Exists(logLocation) == false)
                {
                    Directory.CreateDirectory(logLocation);
                }

                var todayLogLocation = Path.Combine(BaseFolder, "Logs", DateTime.Now.ToString("dd-MM-yyyy"));
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
                var settingsLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogSettings.json");

                return !File.Exists(settingsLocation) ? string.Empty : settingsLocation;
            }
        }

        public static bool Load(string _logSettingLocation = "")
        {
            LogEvent($"=============================================Logger started at: {DateTime.Now}============================================= ", LogLevel.System);

            if (_logSettingLocation == string.Empty)
            {
                _logSettingLocation = LogSettingLocation;
            }

            if (File.Exists(_logSettingLocation) == false)
            {
                LogEvent($"Settings file not found!", LogLevel.System);
            }

            try
            {
                _settings = JsonConvert.DeserializeObject<Objects.Settings>(File.ReadAllText(_logSettingLocation));
            }
            catch (Exception e)
            {
                LogEvent($"Error Loading settings: {e.ToString()}", LogLevel.System);
                HandleDumpLogs();
                return false;
            }

            LoadPlugins();

            DumpLogs();
            EmailBufferThread();
            return true;
        }

        private static void LoadPlugins()
        {
            ILoggerPlugins = PluginController.GetPluginClasses(PluginLocation);
            ILoggerPlugins.ForEach(plugin => plugin.Load());
        }

        public static void Unload()
        {
            LogEvent($"=============================================Logger Ended at: {DateTime.Now}============================================= ", LogLevel.System);
            running = false;
            HandleEmailBuffer();
            HandleDumpLogs();
            ILoggerPlugins.ForEach(plugin => plugin.Unload());
        }

        private static void EmailBufferThread()
        {
            Task.Run(async () =>
            {
                while (running)
                {
                    await Task.Delay(_settings.EmailInterval * 1000);
                    
                    HandleEmailBuffer();
                }
            });
        }

        private static void HandleEmailBuffer()
        {
            if (EmailErrorCache.Count == 0)
            {
                return;
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
            if (_settings != null && _settings.AppName != string.Empty)
            {
                appName = _settings.AppName;
            }

            var body = $"{EmailErrorCache.Count} Errors have been report from {appName}.<br /><br />";
            body += @"<style>table {
                      font-family: arial, sans-serif;
                      border-collapse: collapse;
                      width: 100%;
                    }

                    td, th {
                      border: 1px solid #dddddd;
                      text-align: left;
                      padding: 8px;
                    }
                    </style>";

            lock (EmailErrorCache)
            {
                body += @"<table>";
                body += "<tr>";
                body += "<th style=\"width: 50px;\">Date Time</th>";
                body += "<th style=\"width: 150px;\">Log Level</th>";
                body += "<th>Message</th>";
                body += "</tr>";
                EmailErrorCache.ForEach(error =>
                {
                    var backgroundColorString = "#66bb6a";
                    var colorString = "#212121";
                    switch (error.LogLevel)
                    {
                        case LogLevel.Error:
                            backgroundColorString = "#ff3d00";
                            colorString = "#FFFFFF";
                            break;
                        case LogLevel.Critical:
                            backgroundColorString = "#d50000";
                            colorString = "#FFFFFF";
                            break;
                        case LogLevel.System:
                            backgroundColorString = "#1976d2";
                            colorString = "#FFFFFF";
                            break;
                    }

                    body += $"<tr style=\"background-color: {backgroundColorString}; color: {colorString};\">";
                    body += $"<td>{error.DateTime}</td>";
                    body += $"<td>{error.LogLevel}</td>";
                    body += $"<td>{error.Message}</td>";
                    body += $"</tr>";
                });

                body += "</table>";

                body += $"<br /><br /><hr noshade>Log Generated at: {DateTime.Now}<br />";
                body += $"System Name: {Environment.MachineName}<br />";
                body +=
                    $"Running Folder: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName)}<br />";

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

                EmailErrorCache.Clear();
            }
        }

        private static void DumpLogs()
        {
            Task.Run(async () =>
            {
                while (running)
                {
                    await Task.Delay(1000);
                    HandleDumpLogs();
                }
            });
        }

        private static void HandleDumpLogs()
        {
            lock (ErrorCache)
            {
                foreach (var error in ErrorCache)
                {
                    var errorMessage =
                        $@"{error.DateTime} - LogLevel: {error.LogLevel.ToString()}, {error.Message} {Environment.NewLine}";
                    File.AppendAllText(LogFileLocation + "\\Log.txt", errorMessage);
                }

                ErrorCache.Clear();
            }
        }

        public static void LogEvent(string Message, LogLevel LogLevel)
        {
            var dateTime = DateTime.Now;
            var _event = new Event
            {
                DateTime = dateTime,
                LogLevel = LogLevel,
                Message = Message
            };

            lock (ErrorCache)
            {
                ErrorCache.Add(_event);
            }

            if (_settings != null && _settings.SendEmailAlerts && (int)LogLevel >= _settings.EmailAlertLogLevel)
            {
                EmailErrorCache.Add(_event);
            }

            ILoggerPlugins.ForEach(plugin => plugin.LogEvent(_event));
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
