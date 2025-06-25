using System;

namespace Signatec.BarcodeScaning
{
	/// <summary>
	/// Исключение бросаемое из <see cref="IBarcodeScaning"/>.
	/// </summary>
	public class BarcodeException : Exception
	{
		
		public BarcodeException() { }
		public BarcodeException(string message) : base(message) { }
		public BarcodeException(string message, Exception inner) : base(message, inner) { }
		protected BarcodeException(
			System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}

	/// <summary>
	/// Невозможно расшифровать штрихкод.
	/// </summary>
	public class InvalidBarcodeException : BarcodeException
	{

		public InvalidBarcodeException() { }
		public InvalidBarcodeException(string message) : base(message) { }
		public InvalidBarcodeException(string message, Exception inner) : base(message, inner) { }
		protected InvalidBarcodeException(
			System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}

	/// <summary>
	/// Ошибка, связанная с портом сканера.
	/// </summary>
	public class ScanerPortBarcodeException : BarcodeException
	{

		public ScanerPortBarcodeException() { }
		public ScanerPortBarcodeException(string message) : base(message) { }
		public ScanerPortBarcodeException(string message, Exception inner) : base(message, inner) { }
		protected ScanerPortBarcodeException(
			System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}
}