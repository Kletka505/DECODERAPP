using System;
using System.Collections.Generic;
using System.Linq;

namespace Signatec.BarcodeScaning
{
    public class BarBuffer
    {
        public const int ServiceBufferSize = 4;
        private const int HiginaServiceBufferSize = 10;

        static public byte CalcHeaderCheckSumForBuffer(byte[] buffer)
        {
            byte sum = 0;
            for (int ii = 0; ii < ServiceBufferSize - 1; ii++) sum += buffer[ii];
            return sum;
        }

        static public void SetBufferValues(byte[] buffer, int barNumber, int barCount)
        {
            buffer[0] = (byte)((barNumber << 4) + barCount);
            ushort theSize = Convert.ToUInt16(buffer.Length - ServiceBufferSize);
            buffer[1] = (byte)(theSize >> 8);
            buffer[2] = (byte)(theSize & 0xFF);
            buffer[3] = CalcHeaderCheckSumForBuffer(buffer);
        }

        public BarBuffer(List<byte> buffer)
        {
            _buffer = buffer;
        }

        private readonly List<byte> _buffer;// = new List<byte>();

        public byte[] GetPureBuffer()
        {
            if (IsHigina)
            {
                int count = GetHiginaPureBufferSize();
                var res = new byte[count];
                Array.Copy(_buffer.ToArray(), HiginaServiceBufferSize, res, 0, count);
                return res;
            }
            else
            {
                int count = GetPureBufferSize();
                var res = new byte[count];
                Array.Copy(_buffer.ToArray(), ServiceBufferSize, res, 0, count);
                return res;
            }
        }

        public bool IsHigina
        {
            get
            {
                if (_buffer.Count < HiginaServiceBufferSize) return false;
                if (CalcHiginaHeaderCheckSum() != HiginaHeaderCheckSum) return false;
                if (_buffer[6] != 0 || _buffer[7] != 0 || _buffer[8] != 0) return false;
                return true;
            }
        }

        public void Clear()
        {
            _buffer.Clear();
        }

        public bool IsEmpty
        {
            get { return _buffer.Count == 0; }
        }

        public int GetPureBufferSize()
        {
            return Math.Min(_buffer.Count - ServiceBufferSize, BufferSize);
        }

        private int GetHiginaPureBufferSize()
        {
            return Math.Min(_buffer.Count - HiginaServiceBufferSize, HiginaBufferSize);
        }

        public void AddByte(byte theByte)
        {
            _buffer.Add(theByte);
        }

        public int BufferCount()
        {
            return _buffer.Count;
        }

        public bool IsHeaderCoplited()
        {
            return (_buffer.Count >= ServiceBufferSize) || (_buffer.Count >= HiginaServiceBufferSize);
        }

        public bool IsCoplitedHeaderOk()
        {
            int cs = CalcHeaderCheckSum();
            var hcs = CalcHiginaHeaderCheckSum();
            if (cs != HeaderCheckSum && hcs != HiginaHeaderCheckSum) return false;

            //if (BufferSize > 1024) return false;

            //if (BarCount > 15) return false;
            //if (BarNumber > BarCount - 1) return false;

            return true;
        }

        public bool IsBufferComplited()
        {
            if (IsHigina)
            {
                var hbSize = HiginaBufferSize + HiginaServiceBufferSize;
                return _buffer.Count >= hbSize;
            }

            var bSize = BufferSize + ServiceBufferSize;
            return _buffer.Count >= bSize;
        }

        public byte CalcHeaderCheckSum()
        {
            byte sum = 0;
            for (int ii = 0; ii < ServiceBufferSize - 1; ii++) sum += _buffer[ii];
            return sum;
        }

        private int CalcHiginaHeaderCheckSum()
        {
            if (_buffer.Count < HiginaServiceBufferSize) return 0;
            return _buffer.Take(9).Sum(b => b) % 256;
        }

        //public void SetBufferValues(int barNumber, int barCount)
        //{
        //    _buffer[0] = (byte)((barNumber << 4) + barCount);
        //    ushort theSize = Convert.ToUInt16(_buffer.Length - ServiceBufferSize);
        //    _buffer[1] = (byte)(theSize >> 8);
        //    _buffer[2] = (byte)(theSize & 0x0F);
        //    _buffer[3] = CalcBufferCheckSum();
        //}

        public int BarNumber
        {
            get { return _buffer[0] >> 4; }
        }

        public int HiginaBarNumber
        {
            get { return _buffer[2] - 1; }
        }

        public int BarCount
        {
            get { return _buffer[0] & 0x0F; }
        }

        public int HiginaBarCount
        {
            get { return _buffer[3]; }
        }

        public int BufferSize
        {
            get { return (_buffer[1] << 8) + _buffer[2]; }
        }

        private int HiginaBufferSize
        {
            get
            {
                if (_buffer.Count < HiginaServiceBufferSize) return int.MaxValue;
                return (_buffer[4] << 8) + _buffer[5];
            }
        }

        public byte HeaderCheckSum
        {
            get { return _buffer[3]; }
        }

        private int HiginaHeaderCheckSum
        {
            get
            {
                if (_buffer.Count < HiginaServiceBufferSize) return -1;
                return _buffer[9];
            }
        }
    }
}
