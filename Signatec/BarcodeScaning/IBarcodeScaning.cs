using System;

namespace Signatec.BarcodeScaning
{

	/// <summary>
	/// Аргументы события сканирования части штрихкода.
	/// </summary>
	public class ScanedEventArgs : EventArgs
	{
		public BarcodePartInfo BarcodePartInfo { get; set; }
	}

	/// <summary>
	/// Интерфейс работы со сканером. 
	/// </summary>
	public interface IBarcodeScaning
	{
		/// <summary>
		/// Событие сканирования части штрихкода.
		/// </summary>
		event EventHandler<ScanedEventArgs> Scaned;

		/// <summary>
		/// Получить результат процесса сканирования.
		/// </summary>
		/// <returns>Результат процесса сканирования. null, если процесс еще не завершен.</returns>
		/// <exception cref="BarcodeException">Не удается интерпретировать штрихкод. Возможно, часть штрихкода оказалась битой или относится к другому штрихкоду.</exception>
		Barcode GetResult();

		/// <summary>
		/// Сбросить процесс сканирования.
		/// </summary>
		void Reset();


		/// <summary>
		/// Вызовет остановку текущей прослушки
		/// установит новое имя порта
		/// и начнет прослушку нового порта
		/// т.е. то же самое что Reset  только с установкой имени порта
		/// </summary>
		/// <param name="portName"></param>
		void SetSerialPortNameAndStartLisn(string portName);
	}
}
