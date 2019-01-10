using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FastCypher
{
    internal class __FastCypher
    {
        private class Generator
        {
            #region Mask
            List<byte> _maskTable = new List<byte>();
            long generator;

            private const int a = 43;
            private const int m = 197;
            #endregion

            internal byte NextMask()
            {
                byte next = (byte)(generator % 255);
                byte mask = 0;
                for (short p = 0; p < 8; p++)
                {
                    mask |= (byte)(next & (2 << p));
                }
                generator = (a * generator + next) % m;
                return mask;
            }

            internal Generator(long initial)
            {
                generator = initial;
            }
        }

        private static Generator GetGenerator(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 2 || password.Length > 256)
                throw new ArgumentOutOfRangeException("password", "Password length must be in range 2..256");
            long initial = password[0];
            for (int i = 1; i < Math.Min(password.Length, 15); i++)
            {
                if (i % 2 == 0)
                {
                    initial += password[i] << i;
                }
                else
                {
                    initial ^= password[i] << i - 1;
                }
            }
            return new Generator(initial);
        }

        internal static void Encode(Stream inputStream, Stream outputStream, string password)
        {
            var generator = GetGenerator(password);
            var RRC = new RoundRobinCollection<byte>();
            foreach (var b in Encoding.UTF8.GetBytes(password)) RRC.Add(b);
            int count;
            var buffer = new byte[8];
            byte salt;
            int position;
            var currentSeq = RRC.GetCurrentSeq().GetEnumerator();
            while ((count = inputStream.Read(buffer, 0, 8)) > 0)
            {
                salt = 0;
                for (int i = 0; i < count; i++)
                {
                    if (currentSeq.MoveNext() == false)
                    {
                        RRC.MoveNext();
                        currentSeq = RRC.GetCurrentSeq().GetEnumerator();
                        currentSeq.MoveNext();
                    }
                    position = currentSeq.Current % 8;
                    buffer[i] ^= currentSeq.Current;
                    buffer[i] ^= generator.NextMask();
                    SetBit(i, ref salt, GetBit(position, buffer[i]));
                    SetBit(position, ref buffer[i], GetBit(position, currentSeq.Current));
                }
                outputStream.Write(buffer, 0, count);
                outputStream.WriteByte(salt);
            }
            RRC.Dispose();
            RRC = null;
        }

        internal static void Decode(Stream inputStream, Stream outputStream, string password)
        {
            var generator = GetGenerator(password);
            var RRC = new RoundRobinCollection<byte>();
            foreach (var b in Encoding.UTF8.GetBytes(password)) RRC.Add(b);
            int count;
            var buffer = new byte[9];
            byte salt;
            int position;
            var currentSeq = RRC.GetCurrentSeq().GetEnumerator();
            while ((count = inputStream.Read(buffer, 0, 9)) > 0)
            {
                salt = buffer[count - 1];
                for (int i = 0; i < count - 1; i++)
                {
                    if (currentSeq.MoveNext() == false)
                    {
                        RRC.MoveNext();
                        currentSeq = RRC.GetCurrentSeq().GetEnumerator();
                        currentSeq.MoveNext();
                    }
                    position = currentSeq.Current % 8;
                    SetBit(position, ref buffer[i], GetBit(i, salt));
                    buffer[i] ^= generator.NextMask();
                    buffer[i] ^= currentSeq.Current;
                }
                outputStream.Write(buffer, 0, count - 1);
            }
            RRC.Dispose();
            RRC = null;
        }

        private static bool GetBit(int position, byte num)
        {
            return (num & (1 << position)) != 0;
        }

        private static void SetBit(int position, ref byte num, bool value)
        {
            if (value)
                num |= (byte)(1 << position); // 1
            else
                num &= (byte)(~(1 << position)); // 0
        }

        internal static byte InversionBit(byte original, params int[] positions)
        {
            byte result = original;
            foreach (int position in positions)
            {
                result ^= (byte)(1 << position);
            }
            return result;
        }
    }
}
