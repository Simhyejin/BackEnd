using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackEnd
{
    class Program
    {
        static void Main(string[] args)
        {
            BackEnd be = new BackEnd(41469);
            be.Start();

            while (be.listening)
            {
                be.AcceptClient();
                be.AcceptAgent();
            }

            Console.WriteLine("[Server]End");
            Console.ReadKey();
        }
    }
}
