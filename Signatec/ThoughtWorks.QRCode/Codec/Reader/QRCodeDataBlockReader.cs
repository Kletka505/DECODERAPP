using System;
using System.IO;
using ThoughtWorks.QRCode.Codec.Util;
using ThoughtWorks.QRCode.ExceptionHandler;
namespace ThoughtWorks.QRCode.Codec.Reader
{
	public class QRCodeDataBlockReader
	{
		private const int MODE_NUMBER = 1;
		private const int MODE_ROMAN_AND_NUMBER = 2;
		private const int MODE_8BIT_BYTE = 4;
		private const int MODE_KANJI = 8;
		internal int[] blocks;
		internal int dataLengthMode;
		internal int blockPointer;
		internal int bitPointer;
		internal int dataLength;
		internal int numErrorCorrectionCode;
		internal DebugCanvas canvas;
		private int[][] sizeOfDataLengthInfo = new int[][]
		{
			new int[]
			{
				10,
				9,
				8,
				8
			},
			new int[]
			{
				12,
				11,
				16,
				10
			},
			new int[]
			{
				14,
				13,
				16,
				12
			}
		};
		internal virtual int NextMode
		{
			get
			{
				int result;
				if (this.blockPointer > this.blocks.Length - this.numErrorCorrectionCode - 2)
				{
					result = 0;
				}
				else
				{
					result = this.getNextBits(4);
				}
				return result;
			}
		}
		public virtual sbyte[] DataByte
		{
			get
			{
				this.canvas.println("Reading data blocks.");
				MemoryStream memoryStream = new MemoryStream();
				try
				{
					int nextMode;
					while (true)
					{
						nextMode = this.NextMode;
						if (nextMode == 0)
						{
							break;
						}
						if (nextMode != 1 && nextMode != 2 && nextMode != 4 && nextMode != 8)
						{
							goto Block_7;
						}
						this.dataLength = this.getDataLength(nextMode);
						if (this.dataLength < 1)
						{
							goto Block_8;
						}
						int num = nextMode;
						switch (num)
						{
						case 1:
						{
							sbyte[] array = SystemUtils.ToSByteArray(SystemUtils.ToByteArray(this.getFigureString(this.dataLength)));
							memoryStream.Write(SystemUtils.ToByteArray(array), 0, array.Length);
							break;
						}
						case 2:
						{
							sbyte[] array2 = SystemUtils.ToSByteArray(SystemUtils.ToByteArray(this.getRomanAndFigureString(this.dataLength)));
							memoryStream.Write(SystemUtils.ToByteArray(array2), 0, array2.Length);
							break;
						}
						case 3:
							break;
						case 4:
						{
							sbyte[] array3 = this.get8bitByteArray(this.dataLength);
							memoryStream.Write(SystemUtils.ToByteArray(array3), 0, array3.Length);
							break;
						}
						default:
							if (num == 8)
							{
								sbyte[] array4 = SystemUtils.ToSByteArray(SystemUtils.ToByteArray(this.getKanjiString(this.dataLength)));
								memoryStream.Write(SystemUtils.ToByteArray(array4), 0, array4.Length);
							}
							break;
						}
					}
					if (memoryStream.Length > 0L)
					{
						goto IL_24B;
					}
					throw new InvalidDataBlockException("Empty data block");
					Block_7:
					throw new InvalidDataBlockException(string.Concat(new object[]
					{
						"Invalid mode: ",
						nextMode,
						" in (block:",
						this.blockPointer,
						" bit:",
						this.bitPointer,
						")"
					}));
					Block_8:
					throw new InvalidDataBlockException("Invalid data length: " + this.dataLength);
				}
				catch (IndexOutOfRangeException throwable)
				{
					SystemUtils.WriteStackTrace(throwable, Console.Error);
					throw new InvalidDataBlockException(string.Concat(new object[]
					{
						"Data Block Error in (block:",
						this.blockPointer,
						" bit:",
						this.bitPointer,
						")"
					}));
				}
				catch (IOException ex)
				{
					throw new InvalidDataBlockException(ex.Message);
				}
				IL_24B:
				return SystemUtils.ToSByteArray(memoryStream.ToArray());
			}
		}
		public virtual string DataString
		{
			get
			{
				this.canvas.println("Reading data blocks...");
				string text = "";
				while (true)
				{
					int nextMode = this.NextMode;
					this.canvas.println("mode: " + nextMode);
					if (nextMode == 0)
					{
						break;
					}
					if (nextMode != 1 && nextMode != 2 && nextMode != 4 && nextMode != 8)
					{
					}
					this.dataLength = this.getDataLength(nextMode);
					this.canvas.println(Convert.ToString(this.blocks[this.blockPointer]));
					Console.Out.WriteLine("length: " + this.dataLength);
					int num = nextMode;
					switch (num)
					{
					case 1:
						text += this.getFigureString(this.dataLength);
						break;
					case 2:
						text += this.getRomanAndFigureString(this.dataLength);
						break;
					case 3:
						break;
					case 4:
						text += this.get8bitByteString(this.dataLength);
						break;
					default:
						if (num == 8)
						{
							text += this.getKanjiString(this.dataLength);
						}
						break;
					}
				}
				Console.Out.WriteLine("");
				return text;
			}
		}
		public QRCodeDataBlockReader(int[] blocks, int version, int numErrorCorrectionCode)
		{
			this.blockPointer = 0;
			this.bitPointer = 7;
			this.dataLength = 0;
			this.blocks = blocks;
			this.numErrorCorrectionCode = numErrorCorrectionCode;
			if (version <= 9)
			{
				this.dataLengthMode = 0;
			}
			else
			{
				if (version >= 10 && version <= 26)
				{
					this.dataLengthMode = 1;
				}
				else
				{
					if (version >= 27 && version <= 40)
					{
						this.dataLengthMode = 2;
					}
				}
			}
			this.canvas = QRCodeDecoder.Canvas;
		}
		internal virtual int getNextBits(int numBits)
		{
			int result;
			if (numBits < this.bitPointer + 1)
			{
				int num = 0;
				for (int i = 0; i < numBits; i++)
				{
					num += 1 << i;
				}
				num <<= this.bitPointer - numBits + 1;
				int num2 = (this.blocks[this.blockPointer] & num) >> this.bitPointer - numBits + 1;
				this.bitPointer -= numBits;
				result = num2;
			}
			else
			{
				if (numBits < this.bitPointer + 1 + 8)
				{
					int num3 = 0;
					for (int i = 0; i < this.bitPointer + 1; i++)
					{
						num3 += 1 << i;
					}
					int num2 = (this.blocks[this.blockPointer] & num3) << numBits - (this.bitPointer + 1);
					this.blockPointer++;
					num2 += this.blocks[this.blockPointer] >> 8 - (numBits - (this.bitPointer + 1));
					this.bitPointer -= numBits % 8;
					if (this.bitPointer < 0)
					{
						this.bitPointer = 8 + this.bitPointer;
					}
					result = num2;
				}
				else
				{
					if (numBits >= this.bitPointer + 1 + 16)
					{
						throw new Exception("QRCodeDataBlockReader::getNextBits: Error!");
					}
					int num3 = 0;
					int num4 = 0;
					for (int i = 0; i < this.bitPointer + 1; i++)
					{
						num3 += 1 << i;
					}
					int num5 = (this.blocks[this.blockPointer] & num3) << numBits - (this.bitPointer + 1);
					this.blockPointer++;
					int num6 = this.blocks[this.blockPointer] << numBits - (this.bitPointer + 1 + 8);
					this.blockPointer++;
					for (int i = 0; i < numBits - (this.bitPointer + 1 + 8); i++)
					{
						num4 += 1 << i;
					}
					num4 <<= 8 - (numBits - (this.bitPointer + 1 + 8));
					int num7 = (this.blocks[this.blockPointer] & num4) >> 8 - (numBits - (this.bitPointer + 1 + 8));
					int num2 = num5 + num6 + num7;
					this.bitPointer -= (numBits - 8) % 8;
					if (this.bitPointer < 0)
					{
						this.bitPointer = 8 + this.bitPointer;
					}
					result = num2;
				}
			}
			return result;
		}
		internal virtual int guessMode(int mode)
		{
			int result;
			switch (mode)
			{
			case 3:
				result = 1;
				return result;
			case 5:
				result = 4;
				return result;
			case 6:
				result = 4;
				return result;
			case 7:
				result = 4;
				return result;
			case 9:
				result = 8;
				return result;
			case 10:
				result = 8;
				return result;
			case 11:
				result = 8;
				return result;
			case 12:
				result = 4;
				return result;
			case 13:
				result = 4;
				return result;
			case 14:
				result = 4;
				return result;
			case 15:
				result = 4;
				return result;
			}
			result = 8;
			return result;
		}
		internal virtual int getDataLength(int modeIndicator)
		{
			int num = 0;
			while (modeIndicator >> num != 1)
			{
				num++;
			}
			return this.getNextBits(this.sizeOfDataLengthInfo[this.dataLengthMode][num]);
		}
		internal virtual string getFigureString(int dataLength)
		{
			int num = dataLength;
			int num2 = 0;
			string text = "";
			do
			{
				if (num >= 3)
				{
					num2 = this.getNextBits(10);
					if (num2 < 100)
					{
						text += "0";
					}
					if (num2 < 10)
					{
						text += "0";
					}
					num -= 3;
				}
				else
				{
					if (num == 2)
					{
						num2 = this.getNextBits(7);
						if (num2 < 10)
						{
							text += "0";
						}
						num -= 2;
					}
					else
					{
						if (num == 1)
						{
							num2 = this.getNextBits(4);
							num--;
						}
					}
				}
				text += Convert.ToString(num2);
			}
			while (num > 0);
			return text;
		}
		internal virtual string getRomanAndFigureString(int dataLength)
		{
			int num = dataLength;
			string text = "";
			char[] array = new char[]
			{
				'0',
				'1',
				'2',
				'3',
				'4',
				'5',
				'6',
				'7',
				'8',
				'9',
				'A',
				'B',
				'C',
				'D',
				'E',
				'F',
				'G',
				'H',
				'I',
				'J',
				'K',
				'L',
				'M',
				'N',
				'O',
				'P',
				'Q',
				'R',
				'S',
				'T',
				'U',
				'V',
				'W',
				'X',
				'Y',
				'Z',
				' ',
				'$',
				'%',
				'*',
				'+',
				'-',
				'.',
				'/',
				':'
			};
			do
			{
				if (num > 1)
				{
					int nextBits = this.getNextBits(11);
					int num2 = nextBits / 45;
					int num3 = nextBits % 45;
					text += Convert.ToString(array[num2]);
					text += Convert.ToString(array[num3]);
					num -= 2;
				}
				else
				{
					if (num == 1)
					{
						int nextBits = this.getNextBits(6);
						text += Convert.ToString(array[nextBits]);
						num--;
					}
				}
			}
			while (num > 0);
			return text;
		}
		public virtual sbyte[] get8bitByteArray(int dataLength)
		{
			int num = dataLength;
			MemoryStream memoryStream = new MemoryStream();
			do
			{
				this.canvas.println("Length: " + num);
				int nextBits = this.getNextBits(8);
				memoryStream.WriteByte((byte)nextBits);
				num--;
			}
			while (num > 0);
			return SystemUtils.ToSByteArray(memoryStream.ToArray());
		}
		internal virtual string get8bitByteString(int dataLength)
		{
			int num = dataLength;
			string text = "";
			do
			{
				int nextBits = this.getNextBits(8);
				text += (char)nextBits;
				num--;
			}
			while (num > 0);
			return text;
		}
		internal virtual string getKanjiString(int dataLength)
		{
			int num = dataLength;
			string text = "";
			do
			{
				int nextBits = this.getNextBits(13);
				int num2 = nextBits % 192;
				int num3 = nextBits / 192;
				int num4 = (num3 << 8) + num2;
				int num5;
				if (num4 + 33088 <= 40956)
				{
					num5 = num4 + 33088;
				}
				else
				{
					num5 = num4 + 49472;
				}
				text += new string(SystemUtils.ToCharArray(SystemUtils.ToByteArray(new sbyte[]
				{
					(sbyte)(num5 >> 8),
					(sbyte)(num5 & 255)
				})));
				num--;
			}
			while (num > 0);
			return text;
		}
	}
}
