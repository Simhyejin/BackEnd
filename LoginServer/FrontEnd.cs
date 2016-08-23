using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer
{
    public class FrontEnd
    {
        private string name;
        private string ip;
        private int port;


        public FrontEnd() { }

        public FrontEnd(string name, string ip, int port)
        {
            this.name = name;
            this.ip = ip;
            this.port = port;

        }

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public string Ip
        {
            get { return this.ip; }
            set { this.ip = value; }
        }
        public int Port
        {
            get { return this.port; }
            set { this.port = value; }
        }
    }
}