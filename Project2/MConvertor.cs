using BackEnd.Protocol;
using Project2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static BackEnd.Protocol.Packet;
using static System.BitConverter;
namespace BackEnd
{
    class MConvert
    {
        public class FieldIndex
        {
            public const int UID = 0;
            public const int CODE = 8;
            public const int SIZE = 10;
            public const int DATA = 12;
        }

        private const int HEADER_SIZE = 12;

        public byte[] PacketToBytes(Packet packet)
        {
            byte[] buffer = new byte[sizeof(long) + sizeof(ushort) + sizeof(ushort) + packet.header.size];
            Array.Copy(GetBytes(packet.header.uid), 0, buffer, FieldIndex.UID, sizeof(long));
            Array.Copy(GetBytes(packet.header.code), 0, buffer, FieldIndex.CODE, sizeof(ushort));
            Array.Copy(GetBytes(packet.header.size), 0, buffer, FieldIndex.SIZE, sizeof(ushort));

            if (null != packet.data)
                Array.Copy(packet.data, 0, buffer, FieldIndex.DATA, packet.data.Length);
            return buffer;
        }

        public Packet BytesToPacket(byte[] bytes)
        {
            byte[] headerBytes = new byte[HEADER_SIZE];
            byte[] dataBytes = new byte[bytes.Length - HEADER_SIZE];
            Array.Copy(bytes, 0, headerBytes, 0, headerBytes.Length);
            Array.Copy(bytes, HEADER_SIZE, dataBytes, 0, dataBytes.Length);

            Header header = BytesToHeader(headerBytes);
            Packet packet = new Packet(header, dataBytes);

            return packet;
        }

        public Header BytesToHeader(byte[] bytes)
        {
            Header header = new Header();

            header.uid = ToInt64(bytes, FieldIndex.UID);
            header.code = ToUInt16(bytes, FieldIndex.CODE);
            header.size = ToUInt16(bytes, FieldIndex.SIZE);

            return header;
        }

        public object ByteToStructure(byte[] data, Type type)
        {
            IntPtr buff = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, buff, data.Length);
            object obj = Marshal.PtrToStructure(buff, type);
            Marshal.FreeHGlobal(buff);

            if (Marshal.SizeOf(obj) != data.Length)
            {
                return null;
            }

            return obj;
        }

        public byte[] StructureToByte(object obj)
        {
            int datasize = Marshal.SizeOf(obj);
            IntPtr buff = Marshal.AllocHGlobal(datasize);
            Marshal.StructureToPtr(obj, buff, false);
            byte[] data = new byte[datasize];
            Marshal.Copy(buff, data, 0, datasize);
            Marshal.FreeHGlobal(buff);
            return data;
        }

        public byte[] StructureArrayToByte(object obj, Type type)

        {

            UserHandle[] list = (UserHandle[])obj;

            byte[] resultArr = new byte[Marshal.SizeOf(type) * list.Length];

            int idx = 0;


            foreach (UserHandle userrank in list)

            {
                int datasize = Marshal.SizeOf(userrank);//((PACKET_DATA)obj).TotalBytes; // 구조체에 할당된 메모리의 크기를 구한다.

                IntPtr buff = Marshal.AllocHGlobal(datasize); // 비관리 메모리 영역에 구조체 크기만큼의 메모리를 할당한다.

                Marshal.StructureToPtr(userrank, buff, false); // 할당된 구조체 객체의 주소를 구한다.

                byte[] data = new byte[datasize]; // 구조체가 복사될 배열

                Marshal.Copy(buff, data, 0, datasize); // 구조체 객체를 배열에 복사

                Marshal.FreeHGlobal(buff); // 비관리 메모리 영역에 할당했던 메모리를 해제함


                Array.Copy(data, 0, resultArr, idx * (datasize), data.Length);

                idx++;

            }

            return resultArr; // 배열을 리턴

        }



        public List<int> BytesToList(byte[] body)
        {
            List<int> list = new List<int>();
            for (int idx = 0; idx < (body.Length / 4); idx++)
            {
                byte[] tmpArr = new byte[4];
                Array.Copy(body, idx * 4, tmpArr, 0, 4); // tmpArr에 byte4개 들어가있음.

                int tmp = BitConverter.ToInt32(tmpArr, 0);
                list.Add(tmp);
            }
            return list;
        }

        public string ByteToString(byte[] buff)
        {
            return Encoding.UTF8.GetString(buff);
        }

        public byte[] StringToByte(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public enum KeyType
        {
            Success,
            GoBack,
            LogOut,
            Exit,
            Delete

        };

        public KeyType TryReadLine(out string result)
        {
            var buf = new StringBuilder();
            for (;;)
            {
                //exit
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                {
                    result = "";
                    return KeyType.Exit;
                }
                //go to back
                else if (key.Key == ConsoleKey.F1)
                {
                    result = "";
                    return KeyType.GoBack;
                }
                else if (key.Key == ConsoleKey.F2)
                {
                    result = "";
                    return KeyType.LogOut;
                }
                else if (key.Key == ConsoleKey.F3)
                {
                    result = "";
                    return KeyType.Delete;
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    result = buf.ToString();
                    return KeyType.Success;
                }
                else if (key.Key == ConsoleKey.Backspace && buf.Length > 0)
                {
                    buf.Remove(buf.Length - 1, 1);
                    Console.Write("\b \b");
                }
                else if (key.KeyChar != 0)
                {
                    buf.Append(key.KeyChar);
                    Console.Write(key.KeyChar);
                }
            }
        }

    }

}
