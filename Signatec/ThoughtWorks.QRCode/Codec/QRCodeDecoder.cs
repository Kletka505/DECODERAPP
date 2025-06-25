using System;
using System.Collections;
using System.Text;
using ThoughtWorks.QRCode.Codec.Data;
using ThoughtWorks.QRCode.Codec.Ecc;
using ThoughtWorks.QRCode.Codec.Reader;
using ThoughtWorks.QRCode.Codec.Util;
using ThoughtWorks.QRCode.ExceptionHandler;
using ThoughtWorks.QRCode.Geom;
namespace ThoughtWorks.QRCode.Codec
{
	public class QRCodeDecoder
	{
		internal class DecodeResult
		{
			internal int numCorrections;
			internal bool correctionSucceeded;
			internal sbyte[] decodedBytes;
			private QRCodeDecoder enclosingInstance;
			public virtual sbyte[] DecodedBytes
			{
				get
				{
					return this.decodedBytes;
				}
			}
			public virtual int NumErrors
			{
				get
				{
					return this.numCorrections;
				}
			}
			public virtual bool CorrectionSucceeded
			{
				get
				{
					return this.correctionSucceeded;
				}
			}
			public QRCodeDecoder Enclosing_Instance
			{
				get
				{
					return this.enclosingInstance;
				}
			}
			public DecodeResult(QRCodeDecoder enclosingInstance, sbyte[] decodedBytes, int numErrors, bool correctionSucceeded)
			{
				this.InitBlock(enclosingInstance);
				this.decodedBytes = decodedBytes;
				this.numCorrections = numErrors;
				this.correctionSucceeded = correctionSucceeded;
			}
			private void InitBlock(QRCodeDecoder enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
		}
		internal QRCodeSymbol qrCodeSymbol;
		internal int numTryDecode;
		internal ArrayList results;
		internal ArrayList lastResults = ArrayList.Synchronized(new ArrayList(10));
		internal static DebugCanvas canvas;
		internal QRCodeImageReader imageReader;
		internal int numLastCorrections;
		internal bool correctionSucceeded;
		public static DebugCanvas Canvas
		{
			get
			{
				return QRCodeDecoder.canvas;
			}
			set
			{
				QRCodeDecoder.canvas = value;
			}
		}
		internal virtual Point[] AdjustPoints
		{
			get
			{
				ArrayList arrayList = ArrayList.Synchronized(new ArrayList(10));
				for (int i = 0; i < 4; i++)
				{
					arrayList.Add(new Point(1, 1));
				}
				int num = 0;
				int num2 = 0;
				for (int j = 0; j > -4; j--)
				{
					for (int k = 0; k > -4; k--)
					{
						if (k != j && (k + j) % 2 == 0)
						{
							arrayList.Add(new Point(k - num, j - num2));
							num = k;
							num2 = j;
						}
					}
				}
				Point[] array = new Point[arrayList.Count];
				for (int l = 0; l < array.Length; l++)
				{
					array[l] = (Point)arrayList[l];
				}
				return array;
			}
		}
		public QRCodeDecoder()
		{
			this.numTryDecode = 0;
			this.results = ArrayList.Synchronized(new ArrayList(10));
			QRCodeDecoder.canvas = new DebugCanvasAdapter();
		}
		public virtual sbyte[] decodeBytes(QRCodeImage qrCodeImage)
		{
			Point[] adjustPoints = this.AdjustPoints;
			ArrayList arrayList = ArrayList.Synchronized(new ArrayList(10));
			sbyte[] decodedBytes;
			while (this.numTryDecode < adjustPoints.Length)
			{
				try
				{
					QRCodeDecoder.DecodeResult decodeResult = this.decode(qrCodeImage, adjustPoints[this.numTryDecode]);
					if (decodeResult.CorrectionSucceeded)
					{
						decodedBytes = decodeResult.DecodedBytes;
						return decodedBytes;
					}
					arrayList.Add(decodeResult);
					QRCodeDecoder.canvas.println("Decoding succeeded but could not correct");
					QRCodeDecoder.canvas.println("all errors. Retrying..");
				}
				catch (DecodingFailedException ex)
				{
					if (ex.Message.IndexOf("Finder Pattern") >= 0)
					{
						throw ex;
					}
				}
				finally
				{
					this.numTryDecode++;
				}
			}
			if (arrayList.Count == 0)
			{
				throw new DecodingFailedException("Give up decoding");
			}
			int num = -1;
			int num2 = 2147483647;
			for (int i = 0; i < arrayList.Count; i++)
			{
				QRCodeDecoder.DecodeResult decodeResult = (QRCodeDecoder.DecodeResult)arrayList[i];
				if (decodeResult.NumErrors < num2)
				{
					num2 = decodeResult.NumErrors;
					num = i;
				}
			}
			QRCodeDecoder.canvas.println("All trials need for correct error");
			QRCodeDecoder.canvas.println("Reporting #" + num + " that,");
			QRCodeDecoder.canvas.println("corrected minimum errors (" + num2 + ")");
			QRCodeDecoder.canvas.println("Decoding finished.");
			decodedBytes = ((QRCodeDecoder.DecodeResult)arrayList[num]).DecodedBytes;
			return decodedBytes;
		}
		public byte[] DecodeImageToBytes(QRCodeImage qrCodeImage)
		{
			sbyte[] array = this.decodeBytes(qrCodeImage);
			byte[] array2 = new byte[array.Length];
			Buffer.BlockCopy(array, 0, array2, 0, array2.Length);
			return array2;
		}
		public virtual string decode(QRCodeImage qrCodeImage, Encoding encoding)
		{
			sbyte[] array = this.decodeBytes(qrCodeImage);
			byte[] array2 = new byte[array.Length];
			Buffer.BlockCopy(array, 0, array2, 0, array2.Length);
			return encoding.GetString(array2);
		}
		public virtual string decode(QRCodeImage qrCodeImage)
		{
			sbyte[] array = this.decodeBytes(qrCodeImage);
			byte[] array2 = new byte[array.Length];
			Buffer.BlockCopy(array, 0, array2, 0, array2.Length);
			Encoding encoding;
			if (QRCodeUtility.IsUnicode(array2))
			{
				encoding = Encoding.Unicode;
			}
			else
			{
				encoding = Encoding.ASCII;
			}
			return encoding.GetString(array2);
		}
		internal virtual QRCodeDecoder.DecodeResult decode(QRCodeImage qrCodeImage, Point adjust)
		{
			try
			{
				if (this.numTryDecode == 0)
				{
					QRCodeDecoder.canvas.println("Decoding started");
					int[][] image = this.imageToIntArray(qrCodeImage);
					this.imageReader = new QRCodeImageReader();
					this.qrCodeSymbol = this.imageReader.getQRCodeSymbol(image);
				}
				else
				{
					QRCodeDecoder.canvas.println("--");
					QRCodeDecoder.canvas.println("Decoding restarted #" + this.numTryDecode);
					this.qrCodeSymbol = this.imageReader.getQRCodeSymbolWithAdjustedGrid(adjust);
				}
			}
			catch (SymbolNotFoundException ex)
			{
				throw new DecodingFailedException(ex.Message);
			}
			QRCodeDecoder.canvas.println("Created QRCode symbol.");
			QRCodeDecoder.canvas.println("Reading symbol.");
			QRCodeDecoder.canvas.println("Version: " + this.qrCodeSymbol.VersionReference);
			QRCodeDecoder.canvas.println("Mask pattern: " + this.qrCodeSymbol.MaskPatternRefererAsString);
			int[] blocks = this.qrCodeSymbol.Blocks;
			QRCodeDecoder.canvas.println("Correcting data errors.");
			blocks = this.correctDataBlocks(blocks);
			QRCodeDecoder.DecodeResult result;
			try
			{
				sbyte[] decodedByteArray = this.getDecodedByteArray(blocks, this.qrCodeSymbol.Version, this.qrCodeSymbol.NumErrorCollectionCode);
				result = new QRCodeDecoder.DecodeResult(this, decodedByteArray, this.numLastCorrections, this.correctionSucceeded);
			}
			catch (InvalidDataBlockException ex2)
			{
				QRCodeDecoder.canvas.println(ex2.Message);
				throw new DecodingFailedException(ex2.Message);
			}
			return result;
		}
		internal virtual int[][] imageToIntArray(QRCodeImage image)
		{
			int width = image.Width;
			int height = image.Height;
			int[][] array = new int[width][];
			for (int i = 0; i < width; i++)
			{
				array[i] = new int[height];
			}
			for (int j = 0; j < height; j++)
			{
				for (int k = 0; k < width; k++)
				{
					array[k][j] = image.getPixel(k, j);
				}
			}
			return array;
		}
		internal virtual int[] correctDataBlocks(int[] blocks)
		{
			int num = 0;
			int dataCapacity = this.qrCodeSymbol.DataCapacity;
			int[] array = new int[dataCapacity];
			int numErrorCollectionCode = this.qrCodeSymbol.NumErrorCollectionCode;
			int numRSBlocks = this.qrCodeSymbol.NumRSBlocks;
			int num2 = numErrorCollectionCode / numRSBlocks;
			int[] result;
			if (numRSBlocks == 1)
			{
				ReedSolomon reedSolomon = new ReedSolomon(blocks, num2);
				reedSolomon.correct();
				num += reedSolomon.NumCorrectedErrors;
				if (num > 0)
				{
					QRCodeDecoder.canvas.println(Convert.ToString(num) + " data errors corrected.");
				}
				else
				{
					QRCodeDecoder.canvas.println("No errors found.");
				}
				this.numLastCorrections = num;
				this.correctionSucceeded = reedSolomon.CorrectionSucceeded;
				result = blocks;
			}
			else
			{
				int num3 = dataCapacity % numRSBlocks;
				if (num3 == 0)
				{
					int num4 = dataCapacity / numRSBlocks;
					int[][] array2 = new int[numRSBlocks][];
					for (int i = 0; i < numRSBlocks; i++)
					{
						array2[i] = new int[num4];
					}
					int[][] array3 = array2;
					for (int i = 0; i < numRSBlocks; i++)
					{
						for (int j = 0; j < num4; j++)
						{
							array3[i][j] = blocks[j * numRSBlocks + i];
						}
						ReedSolomon reedSolomon = new ReedSolomon(array3[i], num2);
						reedSolomon.correct();
						num += reedSolomon.NumCorrectedErrors;
						this.correctionSucceeded = reedSolomon.CorrectionSucceeded;
					}
					int num5 = 0;
					for (int i = 0; i < numRSBlocks; i++)
					{
						for (int j = 0; j < num4 - num2; j++)
						{
							array[num5++] = array3[i][j];
						}
					}
				}
				else
				{
					int num6 = dataCapacity / numRSBlocks;
					int num7 = dataCapacity / numRSBlocks + 1;
					int num8 = numRSBlocks - num3;
					int[][] array4 = new int[num8][];
					for (int k = 0; k < num8; k++)
					{
						array4[k] = new int[num6];
					}
					int[][] array5 = array4;
					int[][] array6 = new int[num3][];
					for (int l = 0; l < num3; l++)
					{
						array6[l] = new int[num7];
					}
					int[][] array7 = array6;
					for (int i = 0; i < numRSBlocks; i++)
					{
						if (i < num8)
						{
							int num9 = 0;
							for (int j = 0; j < num6; j++)
							{
								if (j == num6 - num2)
								{
									num9 = num3;
								}
								array5[i][j] = blocks[j * numRSBlocks + i + num9];
							}
							ReedSolomon reedSolomon = new ReedSolomon(array5[i], num2);
							reedSolomon.correct();
							num += reedSolomon.NumCorrectedErrors;
							this.correctionSucceeded = reedSolomon.CorrectionSucceeded;
						}
						else
						{
							int num9 = 0;
							for (int j = 0; j < num7; j++)
							{
								if (j == num6 - num2)
								{
									num9 = num8;
								}
								array7[i - num8][j] = blocks[j * numRSBlocks + i - num9];
							}
							ReedSolomon reedSolomon = new ReedSolomon(array7[i - num8], num2);
							reedSolomon.correct();
							num += reedSolomon.NumCorrectedErrors;
							this.correctionSucceeded = reedSolomon.CorrectionSucceeded;
						}
					}
					int num5 = 0;
					for (int i = 0; i < numRSBlocks; i++)
					{
						if (i < num8)
						{
							for (int j = 0; j < num6 - num2; j++)
							{
								array[num5++] = array5[i][j];
							}
						}
						else
						{
							for (int j = 0; j < num7 - num2; j++)
							{
								array[num5++] = array7[i - num8][j];
							}
						}
					}
				}
				if (num > 0)
				{
					QRCodeDecoder.canvas.println(Convert.ToString(num) + " data errors corrected.");
				}
				else
				{
					QRCodeDecoder.canvas.println("No errors found.");
				}
				this.numLastCorrections = num;
				result = array;
			}
			return result;
		}
		internal virtual sbyte[] getDecodedByteArray(int[] blocks, int version, int numErrorCorrectionCode)
		{
			QRCodeDataBlockReader qRCodeDataBlockReader = new QRCodeDataBlockReader(blocks, version, numErrorCorrectionCode);
			sbyte[] dataByte;
			try
			{
				dataByte = qRCodeDataBlockReader.DataByte;
			}
			catch (InvalidDataBlockException ex)
			{
				throw ex;
			}
			return dataByte;
		}
		internal virtual string getDecodedString(int[] blocks, int version, int numErrorCorrectionCode)
		{
			string result = null;
			QRCodeDataBlockReader qRCodeDataBlockReader = new QRCodeDataBlockReader(blocks, version, numErrorCorrectionCode);
			try
			{
				result = qRCodeDataBlockReader.DataString;
			}
			catch (IndexOutOfRangeException ex)
			{
				throw new InvalidDataBlockException(ex.Message);
			}
			return result;
		}
	}
}
