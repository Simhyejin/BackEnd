using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.Protocol
{
    struct CFLoginRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public char[] id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public char[] password;
    }
}
