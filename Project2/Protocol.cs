using System;
using System.Collections.Generic;
using System.Text;
using static System.BitConverter;

namespace BackEnd
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

    public class Protocol
    {
        public byte[] StructToBytes(object strct)
        {
            int size = 0;
            Queue<byte[]> otherStructs = new Queue<byte[]>();
            foreach (var field in strct.GetType().GetFields())
            {
                switch (Type.GetTypeCode(field.FieldType))
                {
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        size += 4;
                        break;
                    case TypeCode.UInt16:
                        size += 2;
                        break;
                    case TypeCode.String:
                        byte[] stringbytes = Encoding.UTF8.GetBytes((string)field.GetValue(strct));
                        size += stringbytes.Length;
                        break;
                    default:
                        byte[] tempbytes;
                        if (field.FieldType.IsArray)
                        {
                            tempbytes = (byte[])field.GetValue(strct);
                            otherStructs.Enqueue(tempbytes);
                            size += tempbytes.Length;
                        }
                        else if (field.FieldType.IsNested)
                        {
                            tempbytes = StructToBytes(field.GetValue(strct));
                            otherStructs.Enqueue(tempbytes);
                            size += tempbytes.Length;
                        }
                        else
                            Console.WriteLine("ERR: FieldInfo[1] - not implemented for type {0}", field.FieldType);
                        break;
                }
            }

            byte[] bytes = new byte[size];
            int copyIndex = 0;
            foreach (var field in strct.GetType().GetFields())
            {
                if (field.FieldType.IsArray || field.FieldType.IsNested)
                {
                    byte[] tempbytes = otherStructs.Dequeue();
                    Array.Copy(tempbytes, 0, bytes, copyIndex, tempbytes.Length);
                    copyIndex += tempbytes.Length;
                }
                else
                {
                    switch (Type.GetTypeCode(field.FieldType))
                    {
                        case TypeCode.Int32:
                            Array.Copy(GetBytes((int)field.GetValue(strct)), 0, bytes, copyIndex, sizeof(int));
                            copyIndex += sizeof(int);
                            break;
                        case TypeCode.UInt16:
                            Array.Copy(GetBytes((ushort)field.GetValue(strct)), 0, bytes, copyIndex, sizeof(ushort));
                            copyIndex += sizeof(ushort);
                            break;
                        case TypeCode.UInt32:
                            Array.Copy(GetBytes((uint)field.GetValue(strct)), 0, bytes, copyIndex, sizeof(uint));
                            copyIndex += sizeof(uint);
                            break;
                        case TypeCode.String:
                            byte[] stringbytes = Encoding.UTF8.GetBytes((string)field.GetValue(strct));
                            Array.Copy(stringbytes, 0, bytes, copyIndex, stringbytes.Length);
                            copyIndex += stringbytes.Length;
                            break;
                        default:
                            Console.WriteLine("ERR: FieldInfo[2] - not implemented for type {0}", field.FieldType);
                            break;
                    }
                }
            }
            return bytes;
        }
        public byte[] PacketToBytes(Packet packet)
        {
            byte[] buffer = new byte[2 + 2 + packet.data.Length];
            Array.Copy(GetBytes(packet.header.code), 0, buffer, FieldIndex.CODE, sizeof(ushort));
            Array.Copy(GetBytes(packet.header.size), 0, buffer, FieldIndex.SIZE, sizeof(ushort));
            Array.Copy(packet.data, 0, buffer, FieldIndex.DATA, packet.data.Length);
            return buffer;
        }

        public Packet BytesToPacket(byte[] bytes)
        {
            byte[] headerBytes = new byte[4];
            byte[] dataBytes = new byte[bytes.Length - 4];
            Array.Copy(bytes, 0, headerBytes, 0, headerBytes.Length);
            Array.Copy(bytes, 4, dataBytes, 0, dataBytes.Length);

            Packet.Header header = BytesToHeader(headerBytes);
            Packet packet = new Packet(header, dataBytes);

            return packet;
        }

        public Packet.Header BytesToHeader(byte[] bytes)
        {
            Packet.Header header = new Packet.Header();

            header.code = ToUInt16(bytes, FieldIndex.CODE);
            header.size = ToUInt16(bytes, FieldIndex.SIZE);

            return header;
        }

        public class FieldIndex
        {
            public const int CODE = 0;
            public const int SIZE = 2;
            public const int DATA = 4;
        }

        public class Code
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
            public const ushort JOIN_SUCCESS = 602;
            public const ushort JOIN_FAIL = 605;
            public const ushort JOIN_FULL_FAIL = 615;
            public const ushort JOIN_NULL_FAIL = 625;

            public const ushort CONNECTION_PASS = 650;
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
        }
        public string PacketDebug(Packet p)
        {
            if (null == p.data)
                return "CODE: " + p.header.code + "\nSIZE: " + p.header.size + "\nDATA: ";
            else
                return "CODE: " + p.header.code + "\nSIZE: " + p.header.size + "\nDATA: " + Encoding.UTF8.GetString(p.data);
        }
    }
}
