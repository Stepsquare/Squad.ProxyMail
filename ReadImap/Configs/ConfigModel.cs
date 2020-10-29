using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadImap.Configs
{
    public class ConfigModel
    {
        public Webservice webservice { get; set; }
        public Webservice getQueue_WS { get; set; }
        public Webservice UpdateQueueMessage_WS { get; set; }
        public List<MailBox> mailboxes { get; set; }

        public class Webservice
        {
            public string endpoint { get; set; }
            public string password { get; set; }
            public string username { get; set; }
        }

        public class MailBox
        {
            public string type { get; set; }
            public string mailFrom { get; set; }
            public string username { get; set; }
            public string password { get; set; }
            public string receiveHost { get; set; }
            public string sendHost { get; set; }
            public int sendPort { get; set; }
            public int receivePort { get; set; }
            public bool receiveEnabledSsl { get; set; }
            public bool sendEnabledSsl { get; set; }
            public string subjectId { get; set; }
            public string teamId { get; set; }
            public string responseTemplate { get; set; }
        }
    }
}
