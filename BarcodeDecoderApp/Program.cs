using System;
using System.IO;
using System.Linq;
using System.Text;
using ThoughtWorks.QRCode.Codec;
using SixLabors.ImageSharp.Formats.Png;
using Signatec.BarcodeScaning;  // Предполагаю, что здесь TaskCompressor и HiginaCompressor

namespace BarcodeDecoderApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Укажите путь к .dat файлу: ");
            string filePath = Console.ReadLine() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Console.WriteLine("❌ Файл не найден или путь пустой.");
                return;
            }

            try
            {
                // Регистрация кодировки для поддержки старых кодировок (если нужно)
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                // Читаем байты файла
                byte[] fileBytes = File.ReadAllBytes(filePath);

                // Создаём QRCodeEncoder и задаём параметры
                QRCodeEncoder qrCodeEncoder = new QRCodeEncoder();
                qrCodeEncoder.QRCodeEncodeMode = QRCodeEncoder.ENCODE_MODE.BYTE;
                qrCodeEncoder.QRCodeErrorCorrect = QRCodeEncoder.ERROR_CORRECTION.M;
                qrCodeEncoder.QRCodeScale = 2;
                qrCodeEncoder.QRCodeVersion = 12;

                // Кодируем данные в QR-код
                var qrImage = qrCodeEncoder.Encode(fileBytes);

                // Сохраняем изображение QR-кода (путь с заменой расширения на .png)
                string outputImagePath = Path.ChangeExtension(filePath, ".png");
                using (var fs = File.OpenWrite(outputImagePath))
                {
                    qrImage.Save(fs, new PngEncoder());
                }

                Console.WriteLine($"✅ QR-код сохранён: {outputImagePath}");

                // Распаковка данных через BarBuffer и компрессоры
                var barBuffer = new BarBuffer(fileBytes.ToList());
                var inputBuffer = barBuffer.GetPureBuffer();

                var res = HiginaCompressor.TryDecompress(inputBuffer);
                if (res == null)
                {
                    res = TaskCompressor.DeCompress(inputBuffer);
                }

                if (res != null)
                {
                    Console.WriteLine("✅ Декодирование прошло успешно. Данные:");
                    foreach (var field in res.Fields)
                    {
                        Console.WriteLine($"{field.Name}: {field.Value}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Не удалось декодировать данные.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Ошибка:");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
