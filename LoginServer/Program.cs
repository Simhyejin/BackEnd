using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer
{
    class Program
    {
        static void Main(string[] args)
        {
            LoginServer server = new LoginServer(11000);
            IPAddress ipBE = IPAddress.Parse("10.100.58.4");
            server.Start(ipBE, 41469);

            while (server.listening)
            {
                server.AcceptClient();
            }

           
            Console.WriteLine("[Server]End");
        }
    }
}
