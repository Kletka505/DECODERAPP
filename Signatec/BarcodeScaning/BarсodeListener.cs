using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace Signatec.BarcodeScaning
{
	internal class SerialPortHelper : IDisposable
	{
		public delegate void DataArrived(int barNumber, int barCount, byte[] barcode);

		static readonly object LockObj = new object();

		readonly SerialPort _serialPort = new SerialPort();
		readonly List<byte> _thePortBuffer = new List<byte>();
		readonly BarBuffer _currentBar = new BarBuffer(new List<byte>(5000));

		public SerialPortHelper(string portName)
		{
			// Set the port's settings
			_serialPort.BaudRate = int.Parse("9600");
			_serialPort.DataBits = int.Parse("8");
			_serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), "One");
			_serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), "None");
			_serialPort.PortName = portName;
			_serialPort.DataReceived += SerialDataReceivedHandler;

			// Иногда на некоторых компьютерах так случалось
			// Что наше же закрытие этого порта не делало его тутже
			// Доступным для повторного открытия
			// Поэтому добавлен код делающий несколько попыток открытия с небольшими задержками
			Exception exc = null;
			for (int i = 0; i < 10; i++)
			{
				try
				{
					_serialPort.Open();
					return;
				}
				catch (Exception ex)
				{
					exc = new ScanerPortBarcodeException("Ошибка при открытии порта", ex);
					Thread.Sleep(100);
				}
			}
			if (null != exc)
			{
				throw exc;
			}
		}

		public void Dispose()
		{
			if (_serialPort.IsOpen)
			{
				_serialPort.Close();
			}
			_serialPort.Dispose();
		}

		/// <summary>
		/// Этот метод выполняется в другом потоке
		/// Перекладывет что есть в порте в отдельный буфер, окуда потом эти данные выгребет поток владельца
		/// </summary>
		void SerialDataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
		{
			if (!_serialPort.IsOpen) return;

			int bytes = _serialPort.BytesToRead;
			byte[] buffer = new byte[bytes];
			_serialPort.Read(buffer, 0, bytes);

			lock (LockObj)
			{
				foreach (byte b in buffer)
				{
					_thePortBuffer.Add(b);
				}
			}
		}

		/// <summary>
		/// Возвращает копию буфера и очищает накопительный массив. Потокобезопасно.
		/// </summary>
		/// <returns></returns>
		IEnumerable<byte> GetBuffer()
		{
			byte[] buffer = null;
			lock (LockObj)
			{
				buffer = _thePortBuffer.ToArray();
				_thePortBuffer.Clear();
			}
			return buffer;
		}

		/// <summary>
		/// Этот метод вызывается владельцем по таймеру в его тред пуле
		/// </summary>
		/// <param name="barArrivedBack"></param>
		public void DispatchBarBuffer(DataArrived barArrivedBack)
		{
			var isComplete = false;
			var barNumber = 0;
			var barsCount = 0;
			byte[] bytes = null;
			lock (_currentBar)
			{
				// Переложим в накопительный буфер данные для обработки
				foreach (byte b in GetBuffer())
				{
					_currentBar.AddByte(b);
				}

				// если еще не считали заголовок то ничего не делаем
				if (!_currentBar.IsHeaderCoplited()) return;

				// если заголовок неправильный, то фсе очищаем и возвращаемся в исходную позицию
				if (!_currentBar.IsCoplitedHeaderOk())
				{
					_currentBar.Clear();
					return;
				}

				if (_currentBar.IsBufferComplited())
				{
					// завершить текущий, начать новый
					isComplete = true;
					barNumber = _currentBar.BarNumber;
					barsCount = _currentBar.BarCount;

                    // Хоть и есть возможность точно узнать количество и номер штрихкода Хижины
					// Вся дальнейшая работа комплекса строится на том что от хижины всегда идет один штрихкод
					if (_currentBar.IsHigina)
                    {
                        barNumber = 0;
                        barsCount = 1;
                    }
					bytes = _currentBar.GetPureBuffer();
					_currentBar.Clear();
				}
			}
			if (isComplete)
			{
				barArrivedBack(barNumber, barsCount, bytes);
			}
		}
	}

	public class BarсodeListener : IBarcodeScaning
	{
		private string _portName;
		private Timer _portTimer = null;
		private SerialPortHelper _portHelper;
		readonly Dictionary<int, byte[]> _inputData = new Dictionary<int, byte[]>();
		//private int _barsCount = 0;

		//public bool IsScanComplited
		//{
		//    get { return (_barsCount > 0 && _barsCount == _inputData.Count); }
		//}

		public BarсodeListener()
		{
		}

		public event EventHandler<ScanedEventArgs> Scaned;

		public Barcode GetResult()
		{
			try
			{
				var inputBuffer = GetInputBuffer();
				var res = HiginaCompressor.TryDecompress(inputBuffer);
				res = res ?? TaskCompressor.DeCompress(inputBuffer);
				using (var sha1 = SHA1.Create())
				{
					var hash = sha1.ComputeHash(inputBuffer);
					res.Hash = Convert.ToBase64String(hash);
				}
				return res;
			}
			catch (Exception ex)
			{
				throw new InvalidBarcodeException("Невозможно расшифровать штрихкод", ex);
			}
		}

		public void Reset()
		{
			StopLisn();
			StartLisn();
		}

		/// <summary>
		/// Вызовет остановку текущей прослушки
		/// установит новое имя порта
		/// и начнет прослушку нового порта
		/// т.е. то же самое что Reset  только с установкой имени порта
		/// </summary>
		/// <param name="portName"></param>
		public void SetSerialPortNameAndStartLisn(string portName)
		{
			StopLisn();
			_portName = portName;
			StartLisn();
		}

		void StartLisn()
		{
			_inputData.Clear();
			if (string.IsNullOrEmpty(_portName))
			{
				throw new ScanerPortBarcodeException("Не задан порт сканера");
			}
			_portHelper = new SerialPortHelper(_portName);
			_portTimer = new Timer(DispatchInput, null, 0, 1000);
		}

		void StopLisn()
		{
			if (_portTimer != null)
			{
				_portTimer.Dispose();
				_portTimer = null;
			}

			if (_portHelper != null)
			{
				_portHelper.Dispose();
				_portHelper = null;
			}

			//_inputData.Clear();
			//_barsCount = 0;
		}

		void DispatchInput(object obj)
		{
			if (_portHelper != null)
			{
				_portHelper.DispatchBarBuffer(OnDataArrived);
			}
		}

		void OnDataArrived(int barNumber, int barCount, byte[] barcode)
		{
			var isManualStop = false;
			_inputData[barNumber] = barcode;

			// Если все считали то останавливаем процесс до явного сброса
			if (isManualStop || (barCount <= _inputData.Count))
			{
				StopLisn();
			}

			if (Scaned != null)
			{
				var arg = new ScanedEventArgs { BarcodePartInfo = new BarcodePartInfo() { Count = barCount, Number = barNumber } };
				Scaned(this, arg);
			}
		}

		byte[] GetInputBuffer()
		{
			int bufLen = 0;
			for (int ii = 0; ii < _inputData.Count; ii++)
			{
				bufLen += _inputData[ii].Length;// .Count;
			}


			byte[] buf = new byte[bufLen];
			int dstOffcet = 0;
			for (int ii = 0; ii < _inputData.Count; ii++)
			{
				byte[] theInputBuf = _inputData[ii].ToArray();
				Buffer.BlockCopy(theInputBuf, 0, buf, dstOffcet, theInputBuf.Length);
				dstOffcet += theInputBuf.Length;
			}

			return buf;
		}

	}
}
