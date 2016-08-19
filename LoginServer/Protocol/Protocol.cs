using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.Protocol
{
    public struct Packet
    {
        public struct Header
        {
            public ushort code;
            public ushort size;
            public Header(ushort code, ushort size)
            {
                this.code = code;
                this.size = size;
            }
        }

        public Header header;
        public byte[] data;

        public Packet(Header header, byte[] data)
        {
            this.header = header;
            this.data = data;
        }
    }
}
