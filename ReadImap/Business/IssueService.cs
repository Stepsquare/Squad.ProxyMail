using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using S22.Imap;
using System.Diagnostics;
using RestSharp;
using System.Collections.Specialized;
using OpenPop.Mime;
using MimeKit;
using MailKit;

namespace ReadImap.Business
{
    class IssueService
    {
        public object[] CreateIssueMailKitImap(MailKit.Net.Imap.ImapClient ccli, MimeMessage message, string subject, string mail, string body, List<MimeEntity> attch, Mail emailInfo, string serviceUrl, string password, string username, string mailFrom)
        {
            try
            {
                List<FileInfo> filesLst = new List<FileInfo>();

                if (attch != null)
                    foreach (var i in attch)
                    {
                        if (i?.ContentType.Name != null)
                            if (i.ContentType.Name != "exe" && i.ContentType.Name != "js" && i.ContentType.Name != "pif" &&
                                i.ContentType.Name != "application" && i.ContentType.Name != "gadget" && i.ContentType.Name != "msi" &&
                                i.ContentType.Name != "msp" && i.ContentType.Name != "com" && i.ContentType.Name != "scr" &&
                                i.ContentType.Name != "hta" && i.ContentType.Name != "cpl" && i.ContentType.Name != "msc" &&
                                i.ContentType.Name != "jar" && i.ContentType.Name != "bat" && i.ContentType.Name != "cmd" &&
                                i.ContentType.Name != "vb" && i.ContentType.Name != "vbs" && i.ContentType.Name != "vbe" &&
                                i.ContentType.Name != "js" && i.ContentType.Name != "jse" && i.ContentType.Name != "ws" &&
                                i.ContentType.Name != "wsf" && i.ContentType.Name != "wsc" && i.ContentType.Name != "wsf" &&
                                i.ContentType.Name != "ps1" && i.ContentType.Name != "ps1xml" && i.ContentType.Name != "ps2" &&
                                i.ContentType.Name != "ps2xml" && i.ContentType.Name != "psc1" && i.ContentType.Name != "psc2" &&
                                i.ContentType.Name != "msh" && i.ContentType.Name != "msh1" && i.ContentType.Name != "msh2" &&
                                i.ContentType.Name != "mshxml" && i.ContentType.Name != "msh1xml" && i.ContentType.Name != "msh2xml" &&
                                i.ContentType.Name != "scf" && i.ContentType.Name != "lnk" && i.ContentType.Name != "inf" &&
                                i.ContentType.Name != "reg" && i.ContentType.Name != "pif")
                            {
                                byte[] buffer = new byte[16 * 1024];
                                MemoryStream ms = new MemoryStream();

                                var part = (MimePart)i;
                                part.Content.DecodeTo(ms);

                                int read;
                                while ((read = ms.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    ms.Write(buffer, 0, read);
                                }

                                FileInfo f = new FileInfo();
                                f.base64 = Convert.ToBase64String(ms.ToArray());
                                f.name = part.FileName;
                                f.type = (!string.IsNullOrEmpty(i.ContentType.MediaType)) ? i.ContentType.MediaType + ((!string.IsNullOrEmpty(i.ContentType.MediaSubtype)) ? "/" + i.ContentType.MediaSubtype : "") : i.ContentType.ToString();
                                f.size = ms.Length.ToString();

                                filesLst.Add(f);
                            }
                    }

                var obj = new
                {
                    title = subject,
                    description = body,
                    sender = mail,
                    files = filesLst,
                    subject_id = emailInfo.subjectId,
                    team_id = (!string.IsNullOrEmpty(emailInfo.teamId)) ? emailInfo.teamId : null,
                    mailFrom
                };

                //Faz a chamada ao WS.

                var client = new RestClient(serviceUrl);

                var request = new RestRequest("", Method.POST);
                request.RequestFormat = DataFormat.Json;

                // easily add HTTP Headers
                request.AddHeader("usr", username);
                request.AddHeader("pw", password);
                request.AddJsonBody(obj);

                RestResponse<ApiResponse> response2 = (RestResponse<ApiResponse>)client.Execute<ApiResponse>(request);

                if (response2.Data == null)
                {
                    return new object[] { false, null };
                }
                else
                if (response2.Data.status == 500)
                {
                    return new object[] { false, null };
                }
                else if (!string.IsNullOrEmpty(response2.Data.data))
                {
                    return new object[] { true, response2.Data.data };
                }
                else
                {
                    return new object[] { true, null };
                }
            }
            catch (Exception ex)
            {
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry(ex.InnerException.ToString());
                }
                return new object[] { false, null };
            }
        }
    }

    public class Mail
    {
        public string mailFrom { get; set; }
        public string userName { get; set; }
        public string password { get; set; }
        public string host { get; set; }
        public int sendPort { get; set; }
        public int receivePort { get; set; }
        public bool EnableSsl { get; set; }
        public string subjectId { get; set; }
        public string teamId { get; set; }
    }

    public class ApiResponse
    {
        public ApiResponse()
        {
        }

        public int status { get; set; }
        public List<string> messages { get; set; }
        public string data { get; set; }
    }

    public class FileInfo
    {
        //public string[] bytes { get; set; }
        public string base64 { get; set; }
        public string name { get; set; }
        public string size { get; set; }
        public string type { get; set; }
    }
}
