using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Login
{
    class Program
    {
        static void Main(string[] args)
        {
            LoginServer server = new LoginServer();
            server.Start();

            while (server.listening)
            {
                server.AcceptClient();
                server.AcceptFE();
                server.AcceptAgent();
            }

            Console.WriteLine("[Server]End");
            Console.ReadKey();
        }

    
    }
}
