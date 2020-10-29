using MailKit;
using MimeKit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ReadImap.Business.NAVService;

namespace ReadImap.Business
{
    public class NAVMailService
    {
        public Configs.ConfigModel GetConfig()
        {
            using (StreamReader r = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "/Configs.json"))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<ReadImap.Configs.ConfigModel>(json);
            }
        }

        public void Loop()
        {

            var config = GetConfig();
            try
            {
                foreach (var i in config.mailboxes)
                {
                    if (i.type == "imap")
                    {
                        ReadMail_MailKitImap(i);
                    }
                }
            }
            catch (Exception ex)
            {
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry(ex.InnerException.ToString(), EventLogEntryType.Information, 101, 1);
                }
            }
        }

        public bool Reply(string sender, string subject, string body, Mail emailInfo)
        {
            try
            {
                using (MailMessage mail2 = new MailMessage())
                {
                    mail2.From = new MailAddress(emailInfo.mailFrom);
                    mail2.To.Add(sender);
                    mail2.Subject = subject;
                    mail2.Body = body;
                    mail2.IsBodyHtml = true;


                    using (SmtpClient smtp = new SmtpClient(emailInfo.host, emailInfo.sendPort))
                    {
                        smtp.Credentials = new NetworkCredential(emailInfo.userName, emailInfo.password);
                        smtp.EnableSsl = emailInfo.EnableSsl;
                        smtp.Send(mail2);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry("Não foi possivel enviar mail.");
                }
                return false;
            }
        }

        public void ReadMail_MailKitImap(Configs.ConfigModel.MailBox mail)
        {
            try
            {

                using (var client = new MailKit.Net.Imap.ImapClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    client.Connect(mail.receiveHost, mail.receivePort, mail.receiveEnabledSsl);
                    client.Authenticate(mail.username, mail.password);

                    client.Inbox.Open(FolderAccess.ReadWrite);
                    var ids = client.Inbox.Search(MailKit.Search.SearchQuery.NotSeen);//Fetch(0, -1, MessageSummaryItems.UniqueId | MessageSummaryItems.BodyStructure | MessageSummaryItems.Envelope);

                    for (int j = 0; j < ids.Count; j++)
                    {
                        var i = client.Inbox.GetMessage(ids[j]);

                        //-----
                        Mail infoMail = new Mail();
                        infoMail.mailFrom = mail.mailFrom;
                        infoMail.userName = mail.username;
                        infoMail.password = mail.password;
                        infoMail.host = mail.sendHost;
                        infoMail.EnableSsl = mail.sendEnabledSsl;
                        infoMail.sendPort = mail.sendPort;
                        infoMail.receivePort = mail.receivePort;


                        if (i.From == null)
                            continue;

                        if (i.From.Mailboxes == null)
                            continue;

                        if (i.From.Mailboxes.Count() == 0)
                            continue;

                        if (string.IsNullOrEmpty(i.From.Mailboxes.First().Address))
                            continue;

                        if (i.Subject == "Delivery Status")
                            continue;


                        try
                        {
                            if (i.From.Mailboxes.First().Address.ToString() == infoMail.mailFrom ||
                            i.To.Mailboxes.Where(k => k.Address == mail.mailFrom).Count() == 0)
                            {
                                //Ignoramos
                                client.Inbox.AddFlags(ids[j], MessageFlags.Seen, true);
                                continue;
                            }
                        }
                        catch
                        {
                            //client.Inbox.AddFlags(ids[j], MessageFlags.Seen, true);
                        }


                        var allAttachs = new List<MimeEntity>();
                        if (i.Attachments != null && i.Attachments.Count() > 0)
                            allAttachs.AddRange(i.Attachments);

                        foreach (MimePart part in i.BodyParts.Where(x => x.ContentDisposition != null && x.ContentDisposition.FileName != null).ToList())
                        {
                            if (!part.IsAttachment)
                                allAttachs.Add((MimeEntity)part);
                        }

                        var body = i.GetTextBody(MimeKit.Text.TextFormat.Html);
                        if (string.IsNullOrEmpty(body))
                        {
                            body = i.GetTextBody(MimeKit.Text.TextFormat.Text);
                        }

                        var res = new NAVService().ProxySendToClient(client, i, i.Subject, i.From.Mailboxes.First().Address, body, allAttachs, infoMail, mail.mailFrom);

                        client.Inbox.AddFlags(ids[j], MessageFlags.Seen, true);

                        SaveFailedMessage(mail.mailFrom, i.From.Mailboxes.First().Address, i.Subject, body, ids[j].ToString(), (i.Date != null) ? i.Date.Date : DateTime.Now);


                    }

                    client.Inbox.Close();
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry("Problemas a conectar com " + mail.username, EventLogEntryType.Information, 101, 1);
                    eventLog.WriteEntry(ex.Message, EventLogEntryType.Information, 101, 1);
                }
            }
        }

        public void SaveFailedMessage(string mailbox, string from, string subject, string content, string messageid, DateTime dateReceived)
        {
            try
            {
                string approot = AppDomain.CurrentDomain.BaseDirectory;

                System.IO.Directory.CreateDirectory(approot + "/failed/" + mailbox);
                System.IO.Directory.CreateDirectory(approot + "/failedMessages/" + mailbox);

                string pathFailedLog = approot + "/failed/" + mailbox;
                string pathFailedMessages = approot + "/failedMessages/" + mailbox;

                string pathFileFailedLog = pathFailedLog + "/" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + ".txt";
                string pathFileFailedMessages = pathFailedMessages + "/" + dateReceived.ToString("dd-MM-yyyy HH_mm_ss") + "_" + from + "_" + messageid + ".html";

                //var file = System.IO.File.OpenWrite(pathFileFailedLog);

                //-- Guardar no ficheiro de log

                using (StreamWriter sw = new StreamWriter(pathFileFailedLog, true))
                {
                    sw.WriteLine("mailbox: " + mailbox + " | " + "from:" + from + " | " + "subject: " + subject + " | " + "datereceived: " + dateReceived.ToString("dd-MM-yyyy HH:mm:ss") + "_" + messageid);
                    sw.Close();
                }

                //-- Guardar Mensagens
                System.IO.File.WriteAllText(pathFileFailedMessages,
                    "mailbox: " + mailbox + "<br />" +
                    "from: " + from + "<br />" +
                    "datereceived: " + dateReceived + "<br />" +
                    "subject: " + subject + "<br />" +
                    "body: " + content, System.Text.Encoding.UTF8);
            }
            catch
            {

            }
        }
    }
}
