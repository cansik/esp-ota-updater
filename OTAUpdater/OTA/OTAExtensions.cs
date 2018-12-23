using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace OTAUpdater.OTA
{
    public static class OTAExtensions
    {
        public static string MD5Hash(this byte[] data, bool upperCase = false)
        {
            using (MD5 md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(data);
                return hash.ToHex(upperCase);
            }
        }

        public static string ToHex(this byte[] bytes, bool upperCase)
        {
            var result = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

            return result.ToString();
        }

        public static string Clean(this string data)
        {
            return data.Replace("\0", string.Empty);
        }

        public static byte[] EncodeUTF8(this string data)
        {
            return Encoding.UTF8.GetBytes(data);
        }

        public static string DecodeUTF8(this byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        public static byte[] ReceiveBuffer(this Socket socket, EndPoint endPoint, int bufferSize)
        {
            var buffer = new byte[bufferSize];
            //socket.Receive(buffer);
            socket.ReceiveFrom(buffer, ref endPoint);
            return buffer;
        }

        public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
    }
}
