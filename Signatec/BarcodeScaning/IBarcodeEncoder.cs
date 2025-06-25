using SixLabors.ImageSharp;

namespace Signatec.BarcodeScaning
{
    public class EncoderSettings
    {
        /// <summary>
        /// Уровень помехозащищенности (0,1,2,3 - Small, Medium, High, Highest) 
        /// </summary>
        public int Correction = 1;

        /// <summary>
        /// Квадратичная информационная емкость бара (15-40)
        /// </summary>
        public int Size = 20;

        /// <summary>
        /// Коэфициент маштабирования (1 - обычно, 2 - если плохое разрешение у принтера, бар будет увеличен в два раза)
        /// Если применять маштабирование на 2 то типичный сканер при Size=20 уже едва-ли сможет захватить весь бар для сканирования
        /// </summary>
        public int Scale = 1;
    }

    public interface IBarcodeEncoder
    {
        Image[] Encode(Barcode markerPairs, EncoderSettings encoderSettings);
    }
}
