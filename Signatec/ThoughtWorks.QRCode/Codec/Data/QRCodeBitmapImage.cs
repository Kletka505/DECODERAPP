using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ThoughtWorks.QRCode.Codec.Data
{
	public class QRCodeBitmapImage : QRCodeImage
	{
		private Image image;
		public virtual int Width
		{
			get
			{
				return this.image.Width;
			}
		}
		public virtual int Height
		{
			get
			{
				return this.image.Height;
			}
		}
		public QRCodeBitmapImage(Image image)
		{
			this.image = image;
		}
		public virtual int getPixel(int x, int y)
        {
            return (int) ((Image<Argb32>) this.image)[x, y].Argb;
		}
	}
}
