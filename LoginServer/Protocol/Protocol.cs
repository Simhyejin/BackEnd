using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.Protocol
{
    public struct Header
    {
        public long uid;
        public ushort code;
        public ushort size;


    }

    public struct Packet
    {
        public Header header;
        public byte[] data;

        public Packet(Header header, byte[] data)
        {
            this.header = header;
            this.data = data;
        }
    }
}
