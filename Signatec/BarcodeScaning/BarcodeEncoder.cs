using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using ThoughtWorks.QRCode.Codec;

namespace Signatec.BarcodeScaning
{
    public class BarcodeEncoder : IBarcodeEncoder
    {
        public Image[] Encode(Barcode markerPairs, EncoderSettings encoderSettings)
        {
            byte[] taskBytes = BarcodeEncoderUtil.GetTaskAsBytes(markerPairs);
            QRCodeEncoder qrEncoder = BarcodeEncoderUtil.CreateEncoder(encoderSettings);
            List<Image> taskBarImages = BarcodeEncoderUtil.SplitTaskToBarImages(taskBytes, qrEncoder);
            return taskBarImages.ToArray();
        }
    }

    internal class BarcodeEncoderUtil
    {
        /// <summary>
        /// Разбивает один входной бефер на несколько буферов в соответствии с максимальной емкостью бара 
        /// для данных настройках кодировщика баров.
        ///  
        /// Дополняет данные буфера служебным заголовком формата в 4 байта в формате:
        /// 
        /// 0 байт:  4бита - номер бара, 4бита - всего баров в задании
        /// 1 и 2 байты: всего байт в данном баре без служебного заголовка
        /// 3 байт: котрольная сумма бара без служ. заголовка полученная простым арифметическим сложением.
        /// </summary>
        /// <param name="bytesBuffer">Все задание уложенное в непрерывный буфер</param>
        /// <param name="qrCodeEncoder"></param>
        /// <returns>Возвращает готовый список буферов для баров 
        /// (стараясь использовать максимальную емкость бара) 
        /// в каждый из которых дополнен служебной информацией</returns>
        public static List<Image> SplitTaskToBarImages(byte[] bytesBuffer, QRCodeEncoder qrCodeEncoder)
        {
            int maxBytesInSquare = qrCodeEncoder.CalcMaxDataBytes();

            int maxCopyBytes = maxBytesInSquare - BarBuffer.ServiceBufferSize;
            List<byte[]> bytesList = new List<byte[]>();

            for (int iPointer = 0; iPointer < bytesBuffer.Length; iPointer += maxCopyBytes)
            {
                int bytesToCopy = bytesBuffer.Length - iPointer;
                if (bytesToCopy > maxCopyBytes) bytesToCopy = maxCopyBytes;

                byte[] theBytesBuf = new byte[bytesToCopy + BarBuffer.ServiceBufferSize];
                Buffer.BlockCopy(bytesBuffer, iPointer, theBytesBuf, BarBuffer.ServiceBufferSize, bytesToCopy);
                bytesList.Add(theBytesBuf);
            }

            byte listCount = (byte)bytesList.Count;
            List<Image> imageList = new List<Image>();

            for (int ii = 0; ii < listCount; ii++)
            {
                byte[] bytes = bytesList[ii];
                BarBuffer.SetBufferValues(bytes, ii, bytesList.Count);
                try
                {
                    imageList.Add(qrCodeEncoder.Encode(bytes));
                }
                catch (Exception ex)
                {
                    throw new BarcodeException("Ошибка при кодировании Бара: " + ex.Message, ex);
                }
            }

            return imageList;
        }

        /// <summary>
        /// Берет не пустые элементы задания и возвращает их в виде набора байт
        /// </summary>
        /// <returns></returns>
        public static byte[] GetTaskAsBytes(Barcode barcode)
        {
            BarcodeHelper helper = new BarcodeHelper();

            foreach (var field in barcode.Fields)
            {
                if (!String.IsNullOrEmpty(field.Value))
                {
                    helper.AddOrReplaceField(field.Name, field.Value);
                }
            }
            return TaskCompressor.Compress(helper.Barcode);
        }

        /// <summary>
        /// СОздает обект кодера баров с применёнными настройками кодирования
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static QRCodeEncoder CreateEncoder(EncoderSettings settings)
        {
            QRCodeEncoder qrCodeEncoder = new QRCodeEncoder();
            qrCodeEncoder.QRCodeEncodeMode = QRCodeEncoder.ENCODE_MODE.BYTE;
            QRCodeEncoder.ERROR_CORRECTION errCorrection = (QRCodeEncoder.ERROR_CORRECTION)settings.Correction;
            qrCodeEncoder.QRCodeErrorCorrect = errCorrection; //QRCodeEncoder.ERROR_CORRECTION).M;
            qrCodeEncoder.QRCodeScale = settings.Scale;//2
            qrCodeEncoder.QRCodeVersion = settings.Size; //12
            return qrCodeEncoder;
        }
    }
}
