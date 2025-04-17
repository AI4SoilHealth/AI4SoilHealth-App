using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Cryptography;
using HandlebarsDotNet;
using System.Data;
using Dapper;
using System.Net.Http.Headers;
using System.Drawing;
using Newtonsoft.Json;
using System.Net;
using MailKit;
using PublicAPI.Common;
using PublicAPI.Common.Models;

namespace PublicAPI.Common.Services {

    /**
        * Mailing related operations   
        @module MailService
    */

    public class MailService {

        private readonly ILogger<MailService> _logger;
        private readonly IDistributedCache _cache;
        private readonly CurrentUserInfo _currentUserInfo;
        private readonly AppOptions _options;
        private readonly Db _db;

        public MailService(Db db, ILogger<MailService> logger, IDistributedCache cache, CurrentUserInfo currentUserInfo, IOptions<AppOptions> options) {
            _logger = logger;
            _cache = cache;
            _currentUserInfo = currentUserInfo;
            _options = options.Value;
            _db = db;
        }

        private async Task SendAsync(SmtpClient client, string eMail, string subject, string htmlString, string fromName, string fromAddress, DkimSigner signer, bool doSign) {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(new MailboxAddress(eMail, eMail));
            message.Subject = subject;
            if (!doSign) {
                message.Body = new TextPart("html")
                {
                    Text = htmlString
                };
            } else {
                var builder = new BodyBuilder();
                builder.TextBody = htmlString;
                builder.HtmlBody = htmlString;

                message.Body = builder.ToMessageBody();
                message.Prepare(EncodingConstraint.SevenBit);

                var headers = new HeaderId[] { HeaderId.From, HeaderId.Subject, HeaderId.To };

                signer.Sign(message, headers);
            }

            var res = await client.SendAsync(message);
        }

        private async static Task SendMailBaby(HttpClient client, string eMail, string subject, string htmlString, string fromName, string fromAddress) {

            var content = new FormUrlEncodedContent(new[] {
                    new KeyValuePair<string, string>("to", eMail),
                    new KeyValuePair<string, string>("from", fromAddress),
                    new KeyValuePair<string, string>("subject", subject),
                    new KeyValuePair<string, string>("body", htmlString)
                });

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.mailbaby.net/mail/send"),
                Headers = {
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    //{ HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" },
                    { HttpRequestHeader.ContentType.ToString(), "application/json" },
                    { "X-API-KEY", "aFHQ7oEwdgponJGFRV1OZVrPLWwHytvwZOsAEBGdn91VHFyACrZqNTvNwTH5SHe6mgvMzsQdbbNDX9IyxMObMly3JStVMUVz5eSlKvX8qnVic2k69MEyZgTOyK7HyBNj" }
                },
                Content = content
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            if (!response.IsSuccessStatusCode) {
                string responseBody = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);
                string message = errorResponse.message;
                throw new Exception(message);
            }

        }

        public async Task<string> SendMail(string taskId, int[] keys, int templateId, int providerId, string senderName, string senderFrom, string subj = null, string message = null, string to=null) {

            if (taskId != null) WebSocketHandler.Start(taskId);

            int n = 0;

            using var connection = _db.CreateConnection(_options.ConnectionString, true);
            connection.Open();

            MailMsg msg = await connection.QueryFirstOrDefaultAsync<MailMsg>("general.get_mail_msg", new { TemplateId = templateId, ProviderId = providerId }, commandType: CommandType.StoredProcedure);

            SmtpClient client = null;
            HttpClient httpClient = null;

            client = new SmtpClient();

            if (msg.InternalName == "MailBaby") {
                msg.Smtp = "relay.mailbaby.net";
                msg.Port = 587;
                msg.Username = "mb69030";
                msg.Password = "Krv3DfDCAsZQ8npbumjc";
            }

            try {
                await client.ConnectAsync(msg.Smtp, msg.Port, (SecureSocketOptions)msg.EmailSslId); //msg.UseTLS ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None);
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                if (msg.Username != null) await client.AuthenticateAsync(msg.Username, msg.Password);
            } catch (Exception ex) {
                if (taskId != null) WebSocketHandler.Finish(taskId, "SMTP connection error " + ex);
                return Helper.CreateError(_logger, ex.Message);
            }

            if (message != null) {
                await SendAsync(client, to, subj, message, msg.SenderName, msg.SenderFrom, null, false );
                return "";
            }

            string mail, subject;

            Handlebars.RegisterHelper("ifEquals", (writer, options, context, arguments) =>
            {
                if (arguments[0].Equals(arguments[1])) {
                    options.Template(writer, (object)context);
                } else {
                    options.Inverse(writer, (object)context);
                }
            });

            var messageTemplate = Handlebars.Compile(msg.Body);
            var subjectMessageTemplate = Handlebars.Compile(msg.Subject);

            IEnumerable<MailData> rs = null;
            DkimSigner signer;
            try {
                rs = await connection.QueryAsync<MailData>("data.get_mail_data", new { Keys = keys }, commandType: CommandType.StoredProcedure);
                signer = new DkimSigner(
        "data/dkimkey.txt", // path to your privatekey
        "anothersurvey.cloud", // your domain name
        "1727969401.anothersurvey") // The selector given on https://dkimcore.org/
                {
                    HeaderCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Simple,
                    BodyCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Simple,
                    AgentOrUserIdentifier = "@anothersurvey.cloud", // your domain name
                    QueryMethod = "dns/txt",
                };
            } catch (Exception ex) {
                if (taskId != null) WebSocketHandler.Finish(taskId, "Error " + ex.Message);
                return Helper.CreateError(_logger, ex.Message);
            }

            string ret = "";
            int sentMails = 0;

            foreach (MailData m in rs) {
                string mailAddr = "";
                try {
                    mail = messageTemplate(m);
                    mail = mail.Replace(m.Url, "<a href=\"" + m.Url + "\">" + m.Url + "</a>");
                    mailAddr = m.Email.Trim();
                    subject = subjectMessageTemplate(m).Trim() + " [ID #" + m.UniqueId + "]";

                    if (mailAddr != "") {
                        foreach (var ma in mailAddr.Split(";")) {
                            if (msg.InternalName == "MailBabyObsolete") {
                                await SendMailBaby(httpClient, ma.Trim(), subject, mail, senderName, senderFrom);
                            } else {
                                await SendAsync(client, ma.Trim(), subject, mail, senderName, senderFrom, signer, msg.InternalName == "MailBaby");
                            }
                            await connection.ExecuteAsync("data.sent_mail", new { RequiredRespondentId = m.Id, m.UserId, EmailTemplateId = templateId, Status = "OK", m.UniqueId, ProviderId = providerId }, commandType: CommandType.StoredProcedure);
                            sentMails++;
                            if (taskId != null && !WebSocketHandler.Notify(taskId, ++n)) break;
                        }
                    }
                } catch (Exception ex) {
                    string s = $"Error when sending to {mailAddr}: {ex.Message}<br>";
                    ret += s;
                    await connection.ExecuteAsync("data.sent_mail", new { RequiredRespondentId = m.Id, m.UserId, EmailTemplateId = templateId, Status = ex.Message, m.UniqueId, ProviderId = providerId }, commandType: CommandType.StoredProcedure);
                }
            }

            string r = $"Sent messages: {sentMails}<br>" + ret;
            if (taskId != null) WebSocketHandler.Finish(taskId, r);
            await client.DisconnectAsync(true);
            if (msg.InternalName == null) await client.DisconnectAsync(true);
            return "";
        }

    }
}
