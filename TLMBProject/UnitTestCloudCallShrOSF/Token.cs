using Kingdee.BOS.Resource;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestCloudCallShrOSF
{
    public class Token
    {
        public static string Base64Decode(string Message)
        {
            if ((Message.Length % 4) != 0)
            {
                throw new ArgumentException(ResManager.LoadKDString("不是正确的BASE64编码，请检查。", "001005000005005", SubSystemType.BASE, new object[0]), "Message");
            }
            string str = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
            int num = Message.Length / 4;
            ArrayList list = new ArrayList(num * 3);
            char[] chArray = Message.ToCharArray();
            for (int i = 0; i < num; i++)
            {
                byte[] buffer = new byte[] { (byte)str.IndexOf(chArray[i * 4]), (byte)str.IndexOf(chArray[(i * 4) + 1]), (byte)str.IndexOf(chArray[(i * 4) + 2]), (byte)str.IndexOf(chArray[(i * 4) + 3]) };
                byte[] buffer2 = new byte[3];
                buffer2[0] = (byte)((buffer[0] << 2) ^ ((buffer[1] & 0x30) >> 4));
                if (buffer[2] != 0x40)
                {
                    buffer2[1] = (byte)((buffer[1] << 4) ^ ((buffer[2] & 60) >> 2));
                }
                else
                {
                    buffer2[2] = 0;
                }
                if (buffer[3] != 0x40)
                {
                    buffer2[2] = (byte)((buffer[2] << 6) ^ buffer[3]);
                }
                else
                {
                    buffer2[2] = 0;
                }
                list.Add(buffer2[0]);
                if (buffer2[1] != 0)
                {
                    list.Add(buffer2[1]);
                }
                if (buffer2[2] != 0)
                {
                    list.Add(buffer2[2]);
                }
            }
            byte[] bytes = (byte[])list.ToArray(Type.GetType("System.Byte"));
            return Encoding.Default.GetString(bytes);
        }

        private static string Base64Encode(byte[] Message)
        {
            char[] chArray = new char[] {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
            'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f',
            'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
            'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/',
            '='
        };
            byte num = 0;
            ArrayList list = new ArrayList(Message);
            int count = list.Count;
            int num3 = count / 3;
            int num4 = 0;
            num4 = count % 3;
            if (num4 > 0)
            {
                for (int j = 0; j < (3 - num4); j++)
                {
                    list.Add(num);
                }
                num3++;
            }
            StringBuilder builder = new StringBuilder(num3 * 4);
            for (int i = 0; i < num3; i++)
            {
                byte[] buffer = new byte[] { (byte)list[i * 3], (byte)list[(i * 3) + 1], (byte)list[(i * 3) + 2] };
                int[] numArray = new int[4];
                numArray[0] = buffer[0] >> 2;
                numArray[1] = ((buffer[0] & 3) << 4) ^ (buffer[1] >> 4);
                if (!buffer[1].Equals(num))
                {
                    numArray[2] = ((buffer[1] & 15) << 2) ^ (buffer[2] >> 6);
                }
                else
                {
                    numArray[2] = 0x40;
                }
                if (!buffer[2].Equals(num))
                {
                    numArray[3] = buffer[2] & 0x3f;
                }
                else
                {
                    numArray[3] = 0x40;
                }
                builder.Append(chArray[numArray[0]]);
                builder.Append(chArray[numArray[1]]);
                builder.Append(chArray[numArray[2]]);
                builder.Append(chArray[numArray[3]]);
            }
            return builder.ToString();
        }

        private static string Base64EncodeJava(byte[] data)
        {
            int num7;
            StringBuilder builder = new StringBuilder();
            string str = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
            int[] numArray = new int[4];
            int num = data.Length - (data.Length % 3);
            for (int i = 0; i < num; i += 3)
            {
                int num3 = 0;
                for (int j = 0; j < 3; j++)
                {
                    num3 = num3 << 8;
                    num3 |= data[i + j] & 0xff;
                }
                for (int k = 0; k < 4; k++)
                {
                    numArray[k] = num3 & 0x3f;
                    num3 = num3 >> 6;
                }
                for (int m = 3; m >= 0; m--)
                {
                    builder.Append(str.Substring(numArray[m], 1));
                }
            }
            switch ((data.Length % 3))
            {
                case 1:
                    num7 = data[data.Length - 1] >> 2;
                    builder.Append(str.Substring(num7, 1));
                    num7 = (data[data.Length - 1] & 3) << 4;
                    builder.Append(str.Substring(num7, 1));
                    builder.Append("==");
                    break;

                case 2:
                    num7 = data[(data.Length - 1) - 1] >> 2;
                    builder.Append(str.Substring(num7, 1));
                    num7 = ((data[(data.Length - 1) - 1] & 3) << 4) | (data[data.Length - 1] >> 4);
                    builder.Append(str.Substring(num7, 1));
                    num7 = (data[data.Length - 1] & 15) << 2;
                    builder.Append(str.Substring(num7, 1));
                    builder.Append('=');
                    break;
            }
            return builder.ToString();
        }

        public static string Byte2Hex(byte[] b)
        {
            string str = "";
            string str2 = "";
            for (int i = 0; i < b.Length; i++)
            {
                str2 = b[i].ToString("X2");
                str = str + str2;
            }
            return str.ToUpper();
        }

        private static byte[] Copybyte(byte[] a, byte[] b)
        {
            byte[] array = new byte[a.Length + b.Length];
            a.CopyTo(array, 0);
            b.CopyTo(array, a.Length);
            return array;
        }

        private static byte[] CreateSha1(byte[] code)
        {
            byte[] buffer = new SHA1CryptoServiceProvider().ComputeHash(code);
            new HMACSHA1();
            return buffer;
        }

        public static string CreateToken(string userName, string pwd, int time)
        {
            string str3;
            if (string.IsNullOrEmpty(userName))
            {
                throw new Exception(ResManager.LoadKDString("用户名不存在!", "001005000005003", SubSystemType.BASE, new object[0]));
            }
            try
            {
                byte[] a = new byte[] { 0, 1, 2, 3 };
                DateTime time2 = DateTime.Parse("1970-1-1");
                DateTime utcNow = DateTime.UtcNow;
                DateTime time4 = utcNow.AddMinutes((double)time);
                byte[] bytes = Encoding.Default.GetBytes(DateDiff(utcNow, time2).ToString("X"));
                byte[] buffer3 = Encoding.Default.GetBytes(DateDiff(time4, time2).ToString("X"));
                byte[] b = Encoding.UTF8.GetBytes(userName);
                byte[] buffer5 = Copybyte(a, Copybyte(bytes, Copybyte(buffer3, b)));
                string s = Base64Decode(pwd);
                SHA1 sha = SHA1.Create();
                byte[] buffer = Copybyte(buffer5, Encoding.Default.GetBytes(s));
                byte[] buffer7 = sha.ComputeHash(buffer);
                str3 = Base64Encode(Copybyte(buffer5, Encoding.Default.GetBytes(Byte2Hex(buffer7)))).Replace("\n", "").Replace("\t", "").Replace("\r", "").Replace("\f", "");
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return str3;
        }

        private static long DateDiff(DateTime DateTime1, DateTime DateTime2)
        {
            TimeSpan span = new TimeSpan(DateTime1.Ticks);
            TimeSpan ts = new TimeSpan(DateTime2.Ticks);
            TimeSpan span3 = span.Subtract(ts).Duration();
            return (long)(((((span3.Days * 0x18) * 0xe10) + (span3.Hours * 0xe10)) + (span3.Minutes * 60)) + span3.Seconds);
        }
    }
}
