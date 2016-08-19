using System.Runtime.InteropServices;

namespace LoginServer.Protocol
{

    //Login and ConnectPassing
    struct FBLoginResponse
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        char[] ip;
        int port;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        char[] cookie;

    }
}
