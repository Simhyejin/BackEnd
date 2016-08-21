using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackEnd.Protocol
{
    public class Command
    {
        public const ushort SIGNUP = 100;
        public const ushort SIGNUP_SUCCESS = 102;
        public const ushort SIGNUP_FAIL = 105;

        public const ushort DELETE_USER = 110;
        public const ushort DELETE_USER_SUCCESS = 112;
        public const ushort DELETE_USER_FAIL = 115;

        public const ushort UPDATE_USER = 120;
        public const ushort UPDATE_USER_SUCCESS = 122;
        public const ushort UPDATE_USER_USER_FAIL = 125;

        public const ushort SIGNIN = 200;
        public const ushort SIGNIN_SUCCESS = 202;
        public const ushort SIGNIN_FAIL = 205;

        public const ushort DUMMY_SIGNIN = 220;
        public const ushort DUMMY_SIGNIN_SUCCESS = 222;
        public const ushort DUMMY_SIGNIN_FAIL = 225;

        public const ushort INITIALIZE = 250; // CL to FE (check cookie as soon as connection established)
        public const ushort INITIALIZE_SUCCESS = 252;
        public const ushort INITIALIZE_FAIL = 255;

        public const ushort SIGNOUT = 300;
        public const ushort SIGNOUT_SUCCESS = 302;
        public const ushort SIGNOUT_FAIL = 305;

        public const ushort ROOM_LIST = 400;
        public const ushort ROOM_LIST_SUCCESS = 402;
        public const ushort ROOM_LIST_FAIL = 405;

        public const ushort CREATE_ROOM = 500;
        public const ushort CREATE_ROOM_SUCCESS = 502;
        public const ushort CREATE_ROOM_FAIL = 505;

        public const ushort JOIN = 600;
        public const ushort JOIN_SUCCESS = 602; //BE -> FE -> CL (room is in current FE - can join)
        public const ushort JOIN_FAIL = 605;
        public const ushort JOIN_FULL_FAIL = 615; //BE -> FE -> CL (room full)
        public const ushort JOIN_NULL_FAIL = 625; //BE -> FE -> CL (room does not exist)
        public const ushort JOIN_REDIRECT = 630;  //BE -> FE -> CL (room not in current FE - REDIRECT)

        public const ushort CONNECTION_PASS = 650; //BE -> FE (user is going your way with this cookie)
        public const ushort CONNECTION_PASS_SUCCESS = 652;
        public const ushort CONNECTION_PASS_FAIL = 655;

        public const ushort LEAVE_ROOM = 700;
        public const ushort LEAVE_ROOM_SUCCESS = 702;
        public const ushort LEAVE_ROOM_FAIL = 705;

        public const ushort DESTROY_ROOM = 800;
        public const ushort DESTROY_ROOM_SUCCESS = 802;
        public const ushort DESTROY_ROOM_FAIL = 805;

        public const ushort MSG = 900;
        public const ushort MSG_SUCCESS = 902;
        public const ushort MSG_FAIL = 905;

        public const ushort HEARTBEAT = 1000;
        public const ushort HEARTBEAT_SUCCESS = 1002;
        public const ushort HEARTBEAT_FAIL = 1005;

        public const ushort ADVERTISE = 1100;
        public const ushort ADVERTISE_SUCCESS = 1102;
        public const ushort ADVERTISE_FAIL = 1105;

        public const ushort SERVER_START = 1200;
        public const ushort SERVER_START_SUCCESS = 1202;
        public const ushort SERVER_START_FAIL = 1205;
        public const ushort SERVER_RESTART = 1240;
        public const ushort SERVER_RESTART_SUCCESS = 1242;
        public const ushort SERVER_RESTART_FAIL = 1245;
        public const ushort SERVER_STOP = 1270;
        public const ushort SERVER_STOP_SUCCESS = 1272;
        public const ushort SERVER_STOP_FAIL = 1275;

        public const ushort SERVER_INFO = 1300;
        public const ushort SERVER_INFO_SUCCESS = 1302;
        public const ushort SERVER_INFO_FAIL = 1305;

        public const ushort RANKINGS = 1400;
        public const ushort RANKINGS_SUCCESS = 1402;
        public const ushort RANKINGS_FAIL = 1405;
    }
}
