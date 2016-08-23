using Login.Protocol;
using System;
using System.Runtime.InteropServices;
using System.Text;
using static Login.Protocol.Packet;
using static System.BitConverter;

namespace Login
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
                int datasize = Marshal.SizeOf(userrank);

                IntPtr buff = Marshal.AllocHGlobal(datasize); 

                Marshal.StructureToPtr(userrank, buff, false); 

                byte[] data = new byte[datasize];

                Marshal.Copy(buff, data, 0, datasize); 

                Marshal.FreeHGlobal(buff); 


                Array.Copy(data, 0, resultArr, idx * (datasize), data.Length);

                idx++;

            }

            return resultArr;

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
