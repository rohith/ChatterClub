using Microsoft.AspNet.SignalR;
using Serilog;
using System;
using System.Configuration;
using System.Net.Mail;

namespace CreativeColon.ChatterClub.Web
{
    static class Utility
    {
        static ILogger _Logger;

        public static ILogger Logger
        {
            get
            {
                if (_Logger == null) _Logger = SetupLogger();
                return _Logger;
            }
        }

        public static bool MailCode(string username, string mailAddress, string code)
        {
            var IsMailSent = false;

            try
            {
                var Subject = "Authentication Code for Getting Started at Chatter Club";
                var Body = string.Format("Your code is {0}", code);
                var Message = new MailMessage() { Subject = Subject, Body = Body };
                Message.To.Add(new MailAddress(mailAddress, username));
                var SMTPClient = new SmtpClient();
                SMTPClient.Send(Message);
                IsMailSent = true;
            }

            catch (Exception)
            {

            }

            return IsMailSent;
        }

        static ILogger SetupLogger()
        {
            var AppLogFilePath = ConfigurationManager.AppSettings["AppLog"];

            //if (!string.IsNullOrWhiteSpace(AppLogFilePath))
            //    File.WriteAllText(AppLogFilePath, string.Empty);

            return !string.IsNullOrWhiteSpace(AppLogFilePath)
                                ? new LoggerConfiguration().WriteTo.File(AppLogFilePath, outputTemplate: "{Timestamp:dd hh:mm:ss tt} {Message}{NewLine}{Exception}").CreateLogger()
                                : new LoggerConfiguration().CreateLogger();
        }

        public static string GetAgentIdentifier(IRequest request)
        {
            return request.Headers["User-Agent"] ?? string.Empty;
        }
    }
}
