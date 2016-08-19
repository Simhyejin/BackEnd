using System;
using System.Runtime.InteropServices;

namespace LoginServer.Protocol
{

    //Login and ConnectPassing
    struct CFLoginResponse
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        char[] ip;
        int port;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        char[] cookie;

        public CFLoginResponse(string ip, int port, string cookie)
        {
            this.ip = new char[15];
            this.port = port;
            this.cookie = new char[256];

            Array.Copy(ip.ToCharArray(), this.ip, ip.Length);
            Array.Copy(cookie.ToCharArray(), this.cookie, cookie.Length);

        }
    }
}
