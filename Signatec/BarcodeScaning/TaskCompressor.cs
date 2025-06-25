using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lomont.Compression;

namespace Signatec.BarcodeScaning
{
	public class TaskCompressor
	{
		/*
		public static Dictionary<string, string> DeCompress(byte[] buffer)
		{
			ArithmeticCompressor comp = new ArithmeticCompressor();
			byte[] buffer2 = comp.Decompress(buffer);

			string theString = new string(Encoding.GetEncoding(1251).GetChars(buffer2));
			string[] theArr = theString.Split(new[] { '\0' });
			Dictionary<string, string> task = new Dictionary<string, string>();
			for (int ii = 0; ii < theArr.Length; ii += 2)
			{
				if (ii + 1 >= theArr.Length) continue;
				task.Add(theArr[ii], theArr[ii + 1]);
			}
			return task;
		}

		private static byte[] Compress(Dictionary<string, string> task)
		{
			StringBuilder sb = new StringBuilder();
			foreach (var taskRow in task)
			{
				sb.AppendFormat("{0}\0{1}\0", taskRow.Key, taskRow.Value);
			}
			string theStr = sb.ToString();
			byte[] buffer = Encoding.GetEncoding(1251).GetBytes(theStr);

			ArithmeticCompressor comp = new ArithmeticCompressor();
			byte[] buffer2 = comp.Compress(buffer);

			return buffer2;
		}*/

		public static Barcode DeCompress(byte[] buffer)
		{
			ArithmeticCompressor comp = new ArithmeticCompressor();
			byte[] buffer2 = comp.Decompress(buffer);

			string theString = new string(Encoding.GetEncoding(1251).GetChars(buffer2));
			string[] theArr = theString.Split(new[] { '\0' });
			//Dictionary<string, string> task = new Dictionary<string, string>();
			BarcodeHelper barcodeHelper = new BarcodeHelper();
			for (int ii = 0; ii < theArr.Length; ii += 2)
			{
				if (ii + 1 >= theArr.Length) continue;
				//task.Add(theArr[ii], theArr[ii + 1]);
				barcodeHelper.AddOrReplaceField(theArr[ii], theArr[ii + 1]);
			}
			return barcodeHelper.Barcode;
		}


		public static byte[] Compress(Barcode barcode)
		{
			StringBuilder sb = new StringBuilder();
			foreach (var field in barcode.Fields)
			{
				sb.AppendFormat("{0}\0{1}\0", field.Name, field.Value);
			}
			string theStr = sb.ToString();
			byte[] buffer = Encoding.GetEncoding(1251).GetBytes(theStr);

			ArithmeticCompressor comp = new ArithmeticCompressor();
			byte[] buffer2 = comp.Compress(buffer);

			return buffer2;
		}



		//List<BarcodeDocField> fields

	}
}
