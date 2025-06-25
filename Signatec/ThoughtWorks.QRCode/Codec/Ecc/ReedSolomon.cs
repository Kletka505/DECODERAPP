using System;
namespace ThoughtWorks.QRCode.Codec.Ecc
{
	public class ReedSolomon
	{
		internal int[] y;
		internal int[] gexp = new int[512];
		internal int[] glog = new int[256];
		internal int NPAR;
		internal int MAXDEG;
		internal int[] synBytes;
		internal int[] Lambda;
		internal int[] Omega;
		internal int[] ErrorLocs = new int[256];
		internal int NErrors;
		internal int[] ErasureLocs = new int[256];
		internal int NErasures = 0;
		internal bool correctionSucceeded = true;
		public virtual bool CorrectionSucceeded
		{
			get
			{
				return this.correctionSucceeded;
			}
		}
		public virtual int NumCorrectedErrors
		{
			get
			{
				return this.NErrors;
			}
		}
		public ReedSolomon(int[] source, int NPAR)
		{
			this.initializeGaloisTables();
			this.y = source;
			this.NPAR = NPAR;
			this.MAXDEG = NPAR * 2;
			this.synBytes = new int[this.MAXDEG];
			this.Lambda = new int[this.MAXDEG];
			this.Omega = new int[this.MAXDEG];
		}
		internal virtual void initializeGaloisTables()
		{
			int num7;
			int num6;
			int num5;
			int num4;
			int num3;
			int num2;
			int num = num2 = (num3 = (num4 = (num5 = (num6 = (num7 = 0)))));
			int num8 = 1;
			this.gexp[0] = 1;
			this.gexp[255] = this.gexp[0];
			this.glog[0] = 0;
			for (int i = 1; i < 256; i++)
			{
				int num9 = num7;
				num7 = num6;
				num6 = num5;
				num5 = num4;
				num4 = (num3 ^ num9);
				num3 = (num ^ num9);
				num = (num2 ^ num9);
				num2 = num8;
				num8 = num9;
				this.gexp[i] = num8 + num2 * 2 + num * 4 + num3 * 8 + num4 * 16 + num5 * 32 + num6 * 64 + num7 * 128;
				this.gexp[i + 255] = this.gexp[i];
			}
			for (int i = 1; i < 256; i++)
			{
				for (int j = 0; j < 256; j++)
				{
					if (this.gexp[j] == i)
					{
						this.glog[i] = j;
						break;
					}
				}
			}
		}
		internal virtual int gmult(int a, int b)
		{
			int result;
			if (a == 0 || b == 0)
			{
				result = 0;
			}
			else
			{
				int num = this.glog[a];
				int num2 = this.glog[b];
				result = this.gexp[num + num2];
			}
			return result;
		}
		internal virtual int ginv(int elt)
		{
			return this.gexp[255 - this.glog[elt]];
		}
		internal virtual void decode_data(int[] data)
		{
			for (int i = 0; i < this.MAXDEG; i++)
			{
				int num = 0;
				for (int j = 0; j < data.Length; j++)
				{
					num = (data[j] ^ this.gmult(this.gexp[i + 1], num));
				}
				this.synBytes[i] = num;
			}
		}
		public virtual void correct()
		{
			this.decode_data(this.y);
			this.correctionSucceeded = true;
			bool flag = false;
			for (int i = 0; i < this.synBytes.Length; i++)
			{
				if (this.synBytes[i] != 0)
				{
					flag = true;
				}
			}
			if (flag)
			{
				this.correctionSucceeded = this.correct_errors_erasures(this.y, this.y.Length, 0, new int[1]);
			}
		}
		internal virtual void Modified_Berlekamp_Massey()
		{
			int[] array = new int[this.MAXDEG];
			int[] array2 = new int[this.MAXDEG];
			int[] array3 = new int[this.MAXDEG];
			int[] array4 = new int[this.MAXDEG];
			this.init_gamma(array4);
			this.copy_poly(array3, array4);
			this.mul_z_poly(array3);
			this.copy_poly(array, array4);
			int num = -1;
			int num2 = this.NErasures;
			for (int i = this.NErasures; i < 8; i++)
			{
				int num3 = this.compute_discrepancy(array, this.synBytes, num2, i);
				if (num3 != 0)
				{
					for (int j = 0; j < this.MAXDEG; j++)
					{
						array2[j] = (array[j] ^ this.gmult(num3, array3[j]));
					}
					if (num2 < i - num)
					{
						int num4 = i - num;
						num = i - num2;
						for (int j = 0; j < this.MAXDEG; j++)
						{
							array3[j] = this.gmult(array[j], this.ginv(num3));
						}
						num2 = num4;
					}
					for (int j = 0; j < this.MAXDEG; j++)
					{
						array[j] = array2[j];
					}
				}
				this.mul_z_poly(array3);
			}
			for (int j = 0; j < this.MAXDEG; j++)
			{
				this.Lambda[j] = array[j];
			}
			this.compute_modified_omega();
		}
		internal virtual void compute_modified_omega()
		{
			int[] array = new int[this.MAXDEG * 2];
			this.mult_polys(array, this.Lambda, this.synBytes);
			this.zero_poly(this.Omega);
			for (int i = 0; i < this.NPAR; i++)
			{
				this.Omega[i] = array[i];
			}
		}
		internal virtual void mult_polys(int[] dst, int[] p1, int[] p2)
		{
			int[] array = new int[this.MAXDEG * 2];
			for (int i = 0; i < this.MAXDEG * 2; i++)
			{
				dst[i] = 0;
			}
			for (int i = 0; i < this.MAXDEG; i++)
			{
				for (int j = this.MAXDEG; j < this.MAXDEG * 2; j++)
				{
					array[j] = 0;
				}
				for (int j = 0; j < this.MAXDEG; j++)
				{
					array[j] = this.gmult(p2[j], p1[i]);
				}
				for (int j = this.MAXDEG * 2 - 1; j >= i; j--)
				{
					array[j] = array[j - i];
				}
				for (int j = 0; j < i; j++)
				{
					array[j] = 0;
				}
				for (int j = 0; j < this.MAXDEG * 2; j++)
				{
					dst[j] ^= array[j];
				}
			}
		}
		internal virtual void init_gamma(int[] gamma)
		{
			int[] array = new int[this.MAXDEG];
			this.zero_poly(gamma);
			this.zero_poly(array);
			gamma[0] = 1;
			for (int i = 0; i < this.NErasures; i++)
			{
				this.copy_poly(array, gamma);
				this.scale_poly(this.gexp[this.ErasureLocs[i]], array);
				this.mul_z_poly(array);
				this.add_polys(gamma, array);
			}
		}
		internal virtual void compute_next_omega(int d, int[] A, int[] dst, int[] src)
		{
			for (int i = 0; i < this.MAXDEG; i++)
			{
				dst[i] = (src[i] ^ this.gmult(d, A[i]));
			}
		}
		internal virtual int compute_discrepancy(int[] lambda, int[] S, int L, int n)
		{
			int num = 0;
			for (int i = 0; i <= L; i++)
			{
				num ^= this.gmult(lambda[i], S[n - i]);
			}
			return num;
		}
		internal virtual void add_polys(int[] dst, int[] src)
		{
			for (int i = 0; i < this.MAXDEG; i++)
			{
				dst[i] ^= src[i];
			}
		}
		internal virtual void copy_poly(int[] dst, int[] src)
		{
			for (int i = 0; i < this.MAXDEG; i++)
			{
				dst[i] = src[i];
			}
		}
		internal virtual void scale_poly(int k, int[] poly)
		{
			for (int i = 0; i < this.MAXDEG; i++)
			{
				poly[i] = this.gmult(k, poly[i]);
			}
		}
		internal virtual void zero_poly(int[] poly)
		{
			for (int i = 0; i < this.MAXDEG; i++)
			{
				poly[i] = 0;
			}
		}
		internal virtual void mul_z_poly(int[] src)
		{
			for (int i = this.MAXDEG - 1; i > 0; i--)
			{
				src[i] = src[i - 1];
			}
			src[0] = 0;
		}
		internal virtual void Find_Roots()
		{
			this.NErrors = 0;
			for (int i = 1; i < 256; i++)
			{
				int num = 0;
				for (int j = 0; j < this.NPAR + 1; j++)
				{
					num ^= this.gmult(this.gexp[j * i % 255], this.Lambda[j]);
				}
				if (num == 0)
				{
					this.ErrorLocs[this.NErrors] = 255 - i;
					this.NErrors++;
				}
			}
		}
		internal virtual bool correct_errors_erasures(int[] codeword, int csize, int nerasures, int[] erasures)
		{
			this.NErasures = nerasures;
			for (int i = 0; i < this.NErasures; i++)
			{
				this.ErasureLocs[i] = erasures[i];
			}
			this.Modified_Berlekamp_Massey();
			this.Find_Roots();
			bool result;
			if (this.NErrors <= this.NPAR || this.NErrors > 0)
			{
				for (int j = 0; j < this.NErrors; j++)
				{
					if (this.ErrorLocs[j] >= csize)
					{
						result = false;
						return result;
					}
				}
				for (int j = 0; j < this.NErrors; j++)
				{
					int i = this.ErrorLocs[j];
					int num = 0;
					for (int k = 0; k < this.MAXDEG; k++)
					{
						num ^= this.gmult(this.Omega[k], this.gexp[(255 - i) * k % 255]);
					}
					int num2 = 0;
					for (int k = 1; k < this.MAXDEG; k += 2)
					{
						num2 ^= this.gmult(this.Lambda[k], this.gexp[(255 - i) * (k - 1) % 255]);
					}
					int num3 = this.gmult(num, this.ginv(num2));
					codeword[csize - i - 1] ^= num3;
				}
				result = true;
			}
			else
			{
				result = false;
			}
			return result;
		}
	}
}
