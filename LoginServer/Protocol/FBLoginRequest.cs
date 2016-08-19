using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.Protocol
{
    struct FBLoginRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public char[] id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        char[] password;
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        //char[] cookie;

        public FBLoginRequest(string id, string  pw)
        {
            this.id = new char[12];
            this.password = new char[18];
            //this.cookie = new char[256];

            Array.Copy(id.ToCharArray(), this.id, id.Length);
            Array.Copy(pw.ToCharArray(), this.password, password.Length);
            //Array.Copy(cookie.ToCharArray(), this.cookie, cookie.Length);

        }
    }
}
