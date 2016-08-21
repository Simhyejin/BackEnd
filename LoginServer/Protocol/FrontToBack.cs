using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.Protocol
{
    struct FBSignupRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public char[] user;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public char[] password;
    }

    struct FBDeleteUserRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public char[] cookie;
    }

    struct FBUpdateUserRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public char[] cookie;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public char[] password;
    }

    struct FBDummySigninRequest
    {
    }

    struct FBSigninRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public char[] user;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public char[] password;

        public FBSigninRequest(char[] usr, char[] pw)
        {
            this.user = new char[12];
            this.password = new char[18];
           

            Array.Copy(usr, this.user, usr.Length);
            Array.Copy(pw, this.password, pw.Length);

        }
    }

    struct FBSignoutRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public char[] cookie;
    }

    //==========================================================

    struct FBSignupResponse
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        char[] cookie;
    }
    struct FBDeleteUserResponse
    {
    }
    struct FBUpdateUserResponse
    {
    }

    struct FBDummySigninResponse
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public char[] ip;
        public int port;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public char[] cookie;
        public FBDummySigninResponse(string ip, int port, string cookie)
        {
            this.ip = new char[15];
            this.port = port;
            this.cookie = new char[64];

            Array.Copy(ip.ToCharArray(), this.ip, ip.Length);
            Array.Copy(cookie.ToCharArray(), this.cookie, cookie.Length);

        }
    }
    //Login and ConnectPassing
    struct FBSigninResponse
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public char[] ip;
        public int port;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public char[] cookie;
        public FBSigninResponse(string ip, int port, string cookie)
        {
            this.ip = new char[15];
            this.port = port;
            this.cookie = new char[64];

            Array.Copy(ip.ToCharArray(), this.ip, ip.Length);
            Array.Copy(cookie.ToCharArray(), this.cookie, cookie.Length);

        }
    }

    struct FBSignoutResponse
    {
    }

    //=================================================================

    struct FBConnectionPassRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        char[] cookie;
        public FBConnectionPassRequest(string cookie)
        {
            this.cookie = new char[64];

            Array.Copy(cookie.ToCharArray(), this.cookie, cookie.Length);

        }
    }

    //===============================================================

    struct FBInitializeRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        char[] cookie;

        public FBInitializeRequest(string cookie)
        {
            this.cookie = new char[64];

            Array.Copy(cookie.ToCharArray(), this.cookie, cookie.Length);

        }
    }

    //=========================================================

    struct FBRoomCreateRequest
    {
    }

    struct FBRoomListRequest
    {
    }

    struct FBRoomJoinRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        char[] cookie;
        int roomNum;
    }

    struct FBRoomLeaveRequest
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        char[] cookie;
    }

    //======================================================================

    struct FBRoomCreateResponse
    {
        int roomNum;
    }

    struct FBRoomListResponse
    {
    }

    struct FBRoomJoinResponse
    {
    }

    struct FBRoomLeaveResponse
    {
    }

    struct FBRoomJoinRedirectResponse
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        char[] ip;
        int port;
    }
}
