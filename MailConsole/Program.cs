using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReadImap.Business;

namespace MailConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var serv = new NAVMailService();
            while (true)
            {
                System.Threading.Thread.Sleep(1000);
                serv.Loop();
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
