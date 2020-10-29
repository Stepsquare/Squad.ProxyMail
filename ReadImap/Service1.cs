using ReadImap.Business;
using S22.Imap;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReadImap
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        Thread thread;
        protected override void OnStart(string[] args)
        {
            thread = new Thread(this.Loop);
            thread.Start();
        }

        protected override void OnStop()
        {
        }

        public void Loop()
        {
            var serv = new NAVMailService();
            while (true)
            {
                Thread.Sleep(13000);
                serv.Loop();
                Thread.Sleep(5000);
            }
        }
    }
}
