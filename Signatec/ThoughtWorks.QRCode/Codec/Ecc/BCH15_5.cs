using System;
namespace ThoughtWorks.QRCode.Codec.Ecc
{
	public class BCH15_5
	{
		internal int[][] gf16;
		internal bool[] recieveData;
		internal int numCorrectedError;
		internal static string[] bitName = new string[]
		{
			"c0",
			"c1",
			"c2",
			"c3",
			"c4",
			"c5",
			"c6",
			"c7",
			"c8",
			"c9",
			"d0",
			"d1",
			"d2",
			"d3",
			"d4"
		};
		public virtual int NumCorrectedError
		{
			get
			{
				return this.numCorrectedError;
			}
		}
		public BCH15_5(bool[] source)
		{
			this.gf16 = this.createGF16();
			this.recieveData = source;
		}
		public virtual bool[] correct()
		{
			int[] s = this.calcSyndrome(this.recieveData);
			int[] errorPos = this.detectErrorBitPosition(s);
			return this.correctErrorBit(this.recieveData, errorPos);
		}
		internal virtual int[][] createGF16()
		{
			this.gf16 = new int[16][];
			for (int i = 0; i < 16; i++)
			{
				this.gf16[i] = new int[4];
			}
			int[] array = new int[4];
			array[0] = 1;
			array[1] = 1;
			int[] array2 = array;
			for (int i = 0; i < 4; i++)
			{
				this.gf16[i][i] = 1;
			}
			for (int i = 0; i < 4; i++)
			{
				this.gf16[4][i] = array2[i];
			}
			for (int i = 5; i < 16; i++)
			{
				for (int j = 1; j < 4; j++)
				{
					this.gf16[i][j] = this.gf16[i - 1][j - 1];
				}
				if (this.gf16[i - 1][3] == 1)
				{
					for (int j = 0; j < 4; j++)
					{
						this.gf16[i][j] = (this.gf16[i][j] + array2[j]) % 2;
					}
				}
			}
			return this.gf16;
		}
		internal virtual int searchElement(int[] x)
		{
			int i;
			for (i = 0; i < 15; i++)
			{
				if (x[0] == this.gf16[i][0] && x[1] == this.gf16[i][1] && x[2] == this.gf16[i][2] && x[3] == this.gf16[i][3])
				{
					break;
				}
			}
			return i;
		}
		internal virtual int[] getCode(int input)
		{
			int[] array = new int[15];
			int[] array2 = new int[8];
			for (int i = 0; i < 15; i++)
			{
				int num = array2[7];
				int num2;
				int num3;
				if (i < 7)
				{
					num2 = (input >> 6 - i) % 2;
					num3 = (num2 + num) % 2;
				}
				else
				{
					num2 = num;
					num3 = 0;
				}
				array2[7] = (array2[6] + num3) % 2;
				array2[6] = (array2[5] + num3) % 2;
				array2[5] = array2[4];
				array2[4] = (array2[3] + num3) % 2;
				array2[3] = array2[2];
				array2[2] = array2[1];
				array2[1] = array2[0];
				array2[0] = num3;
				array[14 - i] = num2;
			}
			return array;
		}
		internal virtual int addGF(int arg1, int arg2)
		{
			int[] array = new int[4];
			for (int i = 0; i < 4; i++)
			{
				int num = (arg1 < 0 || arg1 >= 15) ? 0 : this.gf16[arg1][i];
				int num2 = (arg2 < 0 || arg2 >= 15) ? 0 : this.gf16[arg2][i];
				array[i] = (num + num2) % 2;
			}
			return this.searchElement(array);
		}
		internal virtual int[] calcSyndrome(bool[] y)
		{
			int[] array = new int[5];
			int[] array2 = new int[4];
			int i;
			for (i = 0; i < 15; i++)
			{
				if (y[i])
				{
					for (int j = 0; j < 4; j++)
					{
						array2[j] = (array2[j] + this.gf16[i][j]) % 2;
					}
				}
			}
			i = this.searchElement(array2);
			array[0] = ((i >= 15) ? -1 : i);
			array2 = new int[4];
			for (i = 0; i < 15; i++)
			{
				if (y[i])
				{
					for (int j = 0; j < 4; j++)
					{
						array2[j] = (array2[j] + this.gf16[i * 3 % 15][j]) % 2;
					}
				}
			}
			i = this.searchElement(array2);
			array[2] = ((i >= 15) ? -1 : i);
			array2 = new int[4];
			for (i = 0; i < 15; i++)
			{
				if (y[i])
				{
					for (int j = 0; j < 4; j++)
					{
						array2[j] = (array2[j] + this.gf16[i * 5 % 15][j]) % 2;
					}
				}
			}
			i = this.searchElement(array2);
			array[4] = ((i >= 15) ? -1 : i);
			return array;
		}
		internal virtual int[] calcErrorPositionVariable(int[] s)
		{
			int[] array = new int[4];
			array[0] = s[0];
			int arg = (s[0] + s[1]) % 15;
			int num = this.addGF(s[2], arg);
			num = ((num >= 15) ? -1 : num);
			arg = (s[2] + s[1]) % 15;
			int num2 = this.addGF(s[4], arg);
			num2 = ((num2 >= 15) ? -1 : num2);
			array[1] = ((num2 < 0 && num < 0) ? -1 : ((num2 - num + 15) % 15));
			arg = (s[1] + array[0]) % 15;
			int arg2 = this.addGF(s[2], arg);
			arg = (s[0] + array[1]) % 15;
			array[2] = this.addGF(arg2, arg);
			return array;
		}
		internal virtual int[] detectErrorBitPosition(int[] s)
		{
			int[] array = this.calcErrorPositionVariable(s);
			int[] array2 = new int[4];
			int[] result;
			if (array[0] == -1)
			{
				result = array2;
			}
			else
			{
				if (array[1] == -1)
				{
					array2[0] = 1;
					array2[1] = array[0];
					result = array2;
				}
				else
				{
					for (int i = 0; i < 15; i++)
					{
						int arg = i * 3 % 15;
						int num = i * 2 % 15;
						int num2 = i;
						int num3 = (array[0] + num) % 15;
						int arg2 = this.addGF(arg, num3);
						num3 = (array[1] + num2) % 15;
						int arg3 = this.addGF(num3, array[2]);
						int num4 = this.addGF(arg2, arg3);
						if (num4 >= 15)
						{
							array2[0]++;
							array2[array2[0]] = i;
						}
					}
					result = array2;
				}
			}
			return result;
		}
		internal virtual bool[] correctErrorBit(bool[] y, int[] errorPos)
		{
			for (int i = 1; i <= errorPos[0]; i++)
			{
				y[errorPos[i]] = !y[errorPos[i]];
			}
			this.numCorrectedError = errorPos[0];
			return y;
		}
	}
}
