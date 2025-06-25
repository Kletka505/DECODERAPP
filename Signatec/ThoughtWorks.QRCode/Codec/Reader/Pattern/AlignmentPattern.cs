using System;
using ThoughtWorks.QRCode.Codec.Util;
using ThoughtWorks.QRCode.ExceptionHandler;
using ThoughtWorks.QRCode.Geom;
namespace ThoughtWorks.QRCode.Codec.Reader.Pattern
{
	public class AlignmentPattern
	{
		internal const int RIGHT = 1;
		internal const int BOTTOM = 2;
		internal const int LEFT = 3;
		internal const int TOP = 4;
		internal static DebugCanvas canvas;
		internal Point[][] center;
		internal int patternDistance;
		public virtual int LogicalDistance
		{
			get
			{
				return this.patternDistance;
			}
		}
		internal AlignmentPattern(Point[][] center, int patternDistance)
		{
			this.center = center;
			this.patternDistance = patternDistance;
		}
		public static AlignmentPattern findAlignmentPattern(bool[][] image, FinderPattern finderPattern)
		{
			Point[][] logicalCenter = AlignmentPattern.getLogicalCenter(finderPattern);
			int num = logicalCenter[1][0].X - logicalCenter[0][0].X;
			Point[][] array = AlignmentPattern.getCenter(image, finderPattern, logicalCenter);
			return new AlignmentPattern(array, num);
		}
		public virtual Point[][] getCenter()
		{
			return this.center;
		}
		public virtual void setCenter(Point[][] center)
		{
			this.center = center;
		}
		internal static Point[][] getCenter(bool[][] image, FinderPattern finderPattern, Point[][] logicalCenters)
		{
			int moduleSize = finderPattern.getModuleSize();
			Axis axis = new Axis(finderPattern.getAngle(), moduleSize);
			int num = logicalCenters.Length;
			Point[][] array = new Point[num][];
			for (int i = 0; i < num; i++)
			{
				array[i] = new Point[num];
			}
			axis.Origin = finderPattern.getCenter(0);
			array[0][0] = axis.translate(3, 3);
			AlignmentPattern.canvas.drawCross(array[0][0], Color_Fields.BLUE);
			axis.Origin = finderPattern.getCenter(1);
			array[num - 1][0] = axis.translate(-3, 3);
			AlignmentPattern.canvas.drawCross(array[num - 1][0], Color_Fields.BLUE);
			axis.Origin = finderPattern.getCenter(2);
			array[0][num - 1] = axis.translate(3, -3);
			AlignmentPattern.canvas.drawCross(array[0][num - 1], Color_Fields.BLUE);
			Point p = array[0][0];
			for (int j = 0; j < num; j++)
			{
				for (int k = 0; k < num; k++)
				{
					if ((k != 0 || j != 0) && (k != 0 || j != num - 1) && (k != num - 1 || j != 0))
					{
						Point point = null;
						if (j == 0)
						{
							if (k > 0 && k < num - 1)
							{
								point = axis.translate(array[k - 1][j], logicalCenters[k][j].X - logicalCenters[k - 1][j].X, 0);
							}
							array[k][j] = new Point(point.X, point.Y);
							AlignmentPattern.canvas.drawCross(array[k][j], Color_Fields.RED);
						}
						else
						{
							if (k == 0)
							{
								if (j > 0 && j < num - 1)
								{
									point = axis.translate(array[k][j - 1], 0, logicalCenters[k][j].Y - logicalCenters[k][j - 1].Y);
								}
								array[k][j] = new Point(point.X, point.Y);
								AlignmentPattern.canvas.drawCross(array[k][j], Color_Fields.RED);
							}
							else
							{
								Point point2 = axis.translate(array[k - 1][j], logicalCenters[k][j].X - logicalCenters[k - 1][j].X, 0);
								Point point3 = axis.translate(array[k][j - 1], 0, logicalCenters[k][j].Y - logicalCenters[k][j - 1].Y);
								array[k][j] = new Point((point2.X + point3.X) / 2, (point2.Y + point3.Y) / 2 + 1);
							}
						}
						if (finderPattern.Version > 1)
						{
							Point precisionCenter = AlignmentPattern.getPrecisionCenter(image, array[k][j]);
							if (array[k][j].distanceOf(precisionCenter) < 6)
							{
								AlignmentPattern.canvas.drawCross(array[k][j], Color_Fields.RED);
								int num2 = precisionCenter.X - array[k][j].X;
								int num3 = precisionCenter.Y - array[k][j].Y;
								AlignmentPattern.canvas.println(string.Concat(new object[]
								{
									"Adjust AP(",
									k,
									",",
									j,
									") to d(",
									num2,
									",",
									num3,
									")"
								}));
								array[k][j] = precisionCenter;
							}
						}
						AlignmentPattern.canvas.drawCross(array[k][j], Color_Fields.BLUE);
						AlignmentPattern.canvas.drawLine(new Line(p, array[k][j]), Color_Fields.LIGHTBLUE);
						p = array[k][j];
					}
				}
			}
			return array;
		}
		internal static Point getPrecisionCenter(bool[][] image, Point targetPoint)
		{
			int x = targetPoint.X;
			int y = targetPoint.Y;
			if (x < 0 || y < 0 || x > image.Length - 1 || y > image[0].Length - 1)
			{
				throw new AlignmentPatternNotFoundException("Alignment Pattern finder exceeded out of image");
			}
			if (!image[targetPoint.X][targetPoint.Y])
			{
				int num = 0;
				bool flag = false;
				while (!flag)
				{
					num++;
					for (int i = num; i > -num; i--)
					{
						for (int j = num; j > -num; j--)
						{
							int num2 = targetPoint.X + j;
							int num3 = targetPoint.Y + i;
							if (num2 < 0 || num3 < 0 || num2 > image.Length - 1 || num3 > image[0].Length - 1)
							{
								throw new AlignmentPatternNotFoundException("Alignment Pattern finder exceeded out of image");
							}
							if (image[num2][num3])
							{
								targetPoint = new Point(targetPoint.X + j, targetPoint.Y + i);
								flag = true;
							}
						}
					}
				}
			}
			int num6;
			int num5;
			int num4 = num5 = (num6 = targetPoint.X);
			int num9;
			int num8;
			int num7 = num8 = (num9 = targetPoint.Y);
			while (num4 >= 1 && !AlignmentPattern.targetPointOnTheCorner(image, num4, num8, num4 - 1, num8))
			{
				num4--;
			}
			while (num6 < image.Length - 1 && !AlignmentPattern.targetPointOnTheCorner(image, num6, num8, num6 + 1, num8))
			{
				num6++;
			}
			while (num7 >= 1 && !AlignmentPattern.targetPointOnTheCorner(image, num5, num7, num5, num7 - 1))
			{
				num7--;
			}
			while (num9 < image[0].Length - 1 && !AlignmentPattern.targetPointOnTheCorner(image, num5, num9, num5, num9 + 1))
			{
				num9++;
			}
			return new Point((num4 + num6 + 1) / 2, (num7 + num9 + 1) / 2);
		}
		internal static bool targetPointOnTheCorner(bool[][] image, int x, int y, int nx, int ny)
		{
			if (x < 0 || y < 0 || nx < 0 || ny < 0 || x > image.Length || y > image[0].Length || nx > image.Length || ny > image[0].Length)
			{
				throw new AlignmentPatternNotFoundException("Alignment Pattern Finder exceeded image edge");
			}
			return !image[x][y] && image[nx][ny];
		}
		public static Point[][] getLogicalCenter(FinderPattern finderPattern)
		{
			int version = finderPattern.Version;
			Point[][] array = new Point[1][];
			for (int i = 0; i < 1; i++)
			{
				array[i] = new Point[1];
			}
			int[] array2 = new int[1];
			array2 = LogicalSeed.getSeed(version);
			array = new Point[array2.Length][];
			for (int j = 0; j < array2.Length; j++)
			{
				array[j] = new Point[array2.Length];
			}
			for (int k = 0; k < array.Length; k++)
			{
				for (int l = 0; l < array.Length; l++)
				{
					array[l][k] = new Point(array2[l], array2[k]);
				}
			}
			return array;
		}
		static AlignmentPattern()
		{
			AlignmentPattern.canvas = QRCodeDecoder.Canvas;
		}
	}
}
