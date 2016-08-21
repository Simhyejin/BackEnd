using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Project2.Protocol
{
    struct CFSignupRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        char[] user;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        char[] password;
    }


    struct CFDummySigninRequest
    {
    }

    struct CFSigninRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        char[] user;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        char[] password;
    }

    struct CFSignoutRequest
    {
    }

    //=======================================================================

    struct CFSignupResponse
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        char[] cookie;
    }

    struct CFDummySigninResponse
    {
    }
    //Login and ConnectPassing
    struct CFSigninResponse
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        char[] ip;
        int port;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        char[] cookie;
    }

    struct CFSignoutResponse
    {
    }

    //===========================================================

    struct CFInitializeRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        char[] cookie;
    }

    struct CFInitializeResponse
    {
    }


}
