using System;
using System.Collections;
using ThoughtWorks.QRCode.Codec.Util;
using ThoughtWorks.QRCode.ExceptionHandler;
using ThoughtWorks.QRCode.Geom;
namespace ThoughtWorks.QRCode.Codec.Reader.Pattern
{
	public class FinderPattern
	{
		public const int UL = 0;
		public const int UR = 1;
		public const int DL = 2;
		internal static readonly int[] VersionInfoBit;
		internal static DebugCanvas canvas;
		internal Point[] center;
		internal int version;
		internal int[] sincos;
		internal int[] width;
		internal int[] moduleSize;
		public virtual int Version
		{
			get
			{
				return this.version;
			}
		}
		public virtual int SqrtNumModules
		{
			get
			{
				return 17 + 4 * this.version;
			}
		}
		public static FinderPattern findFinderPattern(bool[][] image)
		{
			Line[] lineAcross = FinderPattern.findLineAcross(image);
			Line[] crossLines = FinderPattern.findLineCross(lineAcross);
			Point[] centers = null;
			try
			{
				centers = FinderPattern.getCenter(crossLines);
			}
			catch (FinderPatternNotFoundException ex)
			{
				throw ex;
			}
			int[] angle = FinderPattern.getAngle(centers);
			centers = FinderPattern.sort(centers, angle);
			int[] array = FinderPattern.getWidth(image, centers, angle);
			int[] array2 = new int[]
			{
				(array[0] << QRCodeImageReader.DECIMAL_POINT) / 7,
				(array[1] << QRCodeImageReader.DECIMAL_POINT) / 7,
				(array[2] << QRCodeImageReader.DECIMAL_POINT) / 7
			};
			int num = FinderPattern.calcRoughVersion(centers, array);
			if (num > 6)
			{
				try
				{
					num = FinderPattern.calcExactVersion(centers, angle, array2, image);
				}
				catch (VersionInformationException)
				{
				}
			}
			return new FinderPattern(centers, num, angle, array, array2);
		}
		internal FinderPattern(Point[] center, int version, int[] sincos, int[] width, int[] moduleSize)
		{
			this.center = center;
			this.version = version;
			this.sincos = sincos;
			this.width = width;
			this.moduleSize = moduleSize;
		}
		public virtual Point[] getCenter()
		{
			return this.center;
		}
		public virtual Point getCenter(int position)
		{
			Point result;
			if (position >= 0 && position <= 2)
			{
				result = this.center[position];
			}
			else
			{
				result = null;
			}
			return result;
		}
		public virtual int getWidth(int position)
		{
			return this.width[position];
		}
		public virtual int[] getAngle()
		{
			return this.sincos;
		}
		public virtual int getModuleSize()
		{
			return this.moduleSize[0];
		}
		public virtual int getModuleSize(int place)
		{
			return this.moduleSize[place];
		}
		internal static Line[] findLineAcross(bool[][] image)
		{
			int num = 0;
			int num2 = 1;
			int num3 = image.Length;
			int num4 = image[0].Length;
			Point point = new Point();
			ArrayList arrayList = ArrayList.Synchronized(new ArrayList(10));
			int[] array = new int[5];
			int num5 = 0;
			int num6 = num;
			bool flag = false;
			while (true)
			{
				bool flag2 = image[point.X][point.Y];
				if (flag2 == flag)
				{
					array[num5]++;
				}
				else
				{
					if (!flag2)
					{
						if (FinderPattern.checkPattern(array, num5))
						{
							int num7;
							int x;
							int num8;
							int y;
							if (num6 == num)
							{
								num7 = point.X;
								for (int i = 0; i < 5; i++)
								{
									num7 -= array[i];
								}
								x = point.X - 1;
								y = (num8 = point.Y);
							}
							else
							{
								x = (num7 = point.X);
								num8 = point.Y;
								for (int i = 0; i < 5; i++)
								{
									num8 -= array[i];
								}
								y = point.Y - 1;
							}
							arrayList.Add(new Line(num7, num8, x, y));
						}
					}
					num5 = (num5 + 1) % 5;
					array[num5] = 1;
					flag = !flag;
				}
				if (num6 == num)
				{
					if (point.X < num3 - 1)
					{
						point.translate(1, 0);
					}
					else
					{
						if (point.Y < num4 - 1)
						{
							point.set_Renamed(0, point.Y + 1);
							array = new int[5];
						}
						else
						{
							point.set_Renamed(0, 0);
							array = new int[5];
							num6 = num2;
						}
					}
				}
				else
				{
					if (point.Y < num4 - 1)
					{
						point.translate(0, 1);
					}
					else
					{
						if (point.X >= num3 - 1)
						{
							break;
						}
						point.set_Renamed(point.X + 1, 0);
						array = new int[5];
					}
				}
			}
			Line[] array2 = new Line[arrayList.Count];
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = (Line)arrayList[j];
			}
			FinderPattern.canvas.drawLines(array2, Color_Fields.LIGHTGREEN);
			return array2;
		}
		internal static bool checkPattern(int[] buffer, int pointer)
		{
			int[] array = new int[]
			{
				1,
				1,
				3,
				1,
				1
			};
			int num = 0;
			for (int i = 0; i < 5; i++)
			{
				num += buffer[i];
			}
			num <<= QRCodeImageReader.DECIMAL_POINT;
			num /= 7;
			bool result;
			for (int j = 0; j < 5; j++)
			{
				int num2 = num * array[j] - num / 2;
				int num3 = num * array[j] + num / 2;
				int num4 = buffer[(pointer + j + 1) % 5] << QRCodeImageReader.DECIMAL_POINT;
				if (num4 < num2 || num4 > num3)
				{
					result = false;
					return result;
				}
			}
			result = true;
			return result;
		}
		internal static Line[] findLineCross(Line[] lineAcross)
		{
			ArrayList arrayList = ArrayList.Synchronized(new ArrayList(10));
			ArrayList arrayList2 = ArrayList.Synchronized(new ArrayList(10));
			ArrayList arrayList3 = ArrayList.Synchronized(new ArrayList(10));
			for (int i = 0; i < lineAcross.Length; i++)
			{
				arrayList3.Add(lineAcross[i]);
			}
			for (int i = 0; i < arrayList3.Count - 1; i++)
			{
				arrayList2.Clear();
				arrayList2.Add(arrayList3[i]);
				for (int j = i + 1; j < arrayList3.Count; j++)
				{
					if (Line.isNeighbor((Line)arrayList2[arrayList2.Count - 1], (Line)arrayList3[j]))
					{
						arrayList2.Add(arrayList3[j]);
						Line line = (Line)arrayList2[arrayList2.Count - 1];
						if (arrayList2.Count * 5 > line.Length && j == arrayList3.Count - 1)
						{
							arrayList.Add(arrayList2[arrayList2.Count / 2]);
							for (int k = 0; k < arrayList2.Count; k++)
							{
								arrayList3.Remove(arrayList2[k]);
							}
						}
					}
					else
					{
						if (FinderPattern.cantNeighbor((Line)arrayList2[arrayList2.Count - 1], (Line)arrayList3[j]) || j == arrayList3.Count - 1)
						{
							Line line = (Line)arrayList2[arrayList2.Count - 1];
							if (arrayList2.Count * 6 > line.Length)
							{
								arrayList.Add(arrayList2[arrayList2.Count / 2]);
								for (int k = 0; k < arrayList2.Count; k++)
								{
									arrayList3.Remove(arrayList2[k]);
								}
							}
							break;
						}
					}
				}
			}
			Line[] array = new Line[arrayList.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = (Line)arrayList[i];
			}
			return array;
		}
		internal static bool cantNeighbor(Line line1, Line line2)
		{
			bool result;
			if (Line.isCross(line1, line2))
			{
				result = true;
			}
			else
			{
				if (line1.Horizontal)
				{
					result = (Math.Abs(line1.getP1().Y - line2.getP1().Y) > 1);
				}
				else
				{
					result = (Math.Abs(line1.getP1().X - line2.getP1().X) > 1);
				}
			}
			return result;
		}
		internal static int[] getAngle(Point[] centers)
		{
			Line[] array = new Line[3];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new Line(centers[i], centers[(i + 1) % array.Length]);
			}
			Line longest = Line.getLongest(array);
			Point point = new Point();
			for (int i = 0; i < centers.Length; i++)
			{
				if (!longest.getP1().equals(centers[i]) && !longest.getP2().equals(centers[i]))
				{
					point = centers[i];
					break;
				}
			}
			FinderPattern.canvas.println("originPoint is: " + point);
			Point point2 = new Point();
			if (point.Y <= longest.getP1().Y & point.Y <= longest.getP2().Y)
			{
				if (longest.getP1().X < longest.getP2().X)
				{
					point2 = longest.getP2();
				}
				else
				{
					point2 = longest.getP1();
				}
			}
			else
			{
				if (point.X >= longest.getP1().X & point.X >= longest.getP2().X)
				{
					if (longest.getP1().Y < longest.getP2().Y)
					{
						point2 = longest.getP2();
					}
					else
					{
						point2 = longest.getP1();
					}
				}
				else
				{
					if (point.Y >= longest.getP1().Y & point.Y >= longest.getP2().Y)
					{
						if (longest.getP1().X < longest.getP2().X)
						{
							point2 = longest.getP1();
						}
						else
						{
							point2 = longest.getP2();
						}
					}
					else
					{
						if (longest.getP1().Y < longest.getP2().Y)
						{
							point2 = longest.getP1();
						}
						else
						{
							point2 = longest.getP2();
						}
					}
				}
			}
			int length = new Line(point, point2).Length;
			return new int[]
			{
				(point2.Y - point.Y << QRCodeImageReader.DECIMAL_POINT) / length,
				(point2.X - point.X << QRCodeImageReader.DECIMAL_POINT) / length
			};
		}
		internal static Point[] getCenter(Line[] crossLines)
		{
			ArrayList arrayList = ArrayList.Synchronized(new ArrayList(10));
			for (int i = 0; i < crossLines.Length - 1; i++)
			{
				Line line = crossLines[i];
				for (int j = i + 1; j < crossLines.Length; j++)
				{
					Line line2 = crossLines[j];
					if (Line.isCross(line, line2))
					{
						int x;
						int y;
						if (line.Horizontal)
						{
							x = line.Center.X;
							y = line2.Center.Y;
						}
						else
						{
							x = line2.Center.X;
							y = line.Center.Y;
						}
						arrayList.Add(new Point(x, y));
					}
				}
			}
			Point[] array = new Point[arrayList.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = (Point)arrayList[i];
			}
			if (array.Length == 3)
			{
				FinderPattern.canvas.drawPolygon(array, Color_Fields.RED);
				return array;
			}
			throw new FinderPatternNotFoundException("Invalid number of Finder Pattern detected");
		}
		internal static Point[] sort(Point[] centers, int[] angle)
		{
			Point[] array = new Point[3];
			switch (FinderPattern.getURQuadant(angle))
			{
			case 1:
				array[1] = FinderPattern.getPointAtSide(centers, 1, 2);
				array[2] = FinderPattern.getPointAtSide(centers, 2, 4);
				break;
			case 2:
				array[1] = FinderPattern.getPointAtSide(centers, 2, 4);
				array[2] = FinderPattern.getPointAtSide(centers, 8, 4);
				break;
			case 3:
				array[1] = FinderPattern.getPointAtSide(centers, 4, 8);
				array[2] = FinderPattern.getPointAtSide(centers, 1, 8);
				break;
			case 4:
				array[1] = FinderPattern.getPointAtSide(centers, 8, 1);
				array[2] = FinderPattern.getPointAtSide(centers, 2, 1);
				break;
			}
			for (int i = 0; i < centers.Length; i++)
			{
				if (!centers[i].equals(array[1]) && !centers[i].equals(array[2]))
				{
					array[0] = centers[i];
				}
			}
			return array;
		}
		internal static int getURQuadant(int[] angle)
		{
			int num = angle[0];
			int num2 = angle[1];
			int result;
			if (num >= 0 && num2 > 0)
			{
				result = 1;
			}
			else
			{
				if (num > 0 && num2 <= 0)
				{
					result = 2;
				}
				else
				{
					if (num <= 0 && num2 < 0)
					{
						result = 3;
					}
					else
					{
						if (num < 0 && num2 >= 0)
						{
							result = 4;
						}
						else
						{
							result = 0;
						}
					}
				}
			}
			return result;
		}
		internal static Point getPointAtSide(Point[] points, int side1, int side2)
		{
			Point point = new Point();
			int x = (side1 == 1 || side2 == 1) ? 0 : 2147483647;
			int y = (side1 == 2 || side2 == 2) ? 0 : 2147483647;
			point = new Point(x, y);
			for (int i = 0; i < points.Length; i++)
			{
				switch (side1)
				{
				case 1:
					if (point.X < points[i].X)
					{
						point = points[i];
					}
					else
					{
						if (point.X == points[i].X)
						{
							if (side2 == 2)
							{
								if (point.Y < points[i].Y)
								{
									point = points[i];
								}
							}
							else
							{
								if (point.Y > points[i].Y)
								{
									point = points[i];
								}
							}
						}
					}
					break;
				case 2:
					if (point.Y < points[i].Y)
					{
						point = points[i];
					}
					else
					{
						if (point.Y == points[i].Y)
						{
							if (side2 == 1)
							{
								if (point.X < points[i].X)
								{
									point = points[i];
								}
							}
							else
							{
								if (point.X > points[i].X)
								{
									point = points[i];
								}
							}
						}
					}
					break;
				case 3:
					break;
				case 4:
					if (point.X > points[i].X)
					{
						point = points[i];
					}
					else
					{
						if (point.X == points[i].X)
						{
							if (side2 == 2)
							{
								if (point.Y < points[i].Y)
								{
									point = points[i];
								}
							}
							else
							{
								if (point.Y > points[i].Y)
								{
									point = points[i];
								}
							}
						}
					}
					break;
				default:
					if (side1 == 8)
					{
						if (point.Y > points[i].Y)
						{
							point = points[i];
						}
						else
						{
							if (point.Y == points[i].Y)
							{
								if (side2 == 1)
								{
									if (point.X < points[i].X)
									{
										point = points[i];
									}
								}
								else
								{
									if (point.X > points[i].X)
									{
										point = points[i];
									}
								}
							}
						}
					}
					break;
				}
			}
			return point;
		}
		internal static int[] getWidth(bool[][] image, Point[] centers, int[] sincos)
		{
			int[] array = new int[3];
			for (int i = 0; i < 3; i++)
			{
				bool flag = false;
				int y = centers[i].Y;
				int j;
				for (j = centers[i].X; j > 0; j--)
				{
					if (image[j][y] && !image[j - 1][y])
					{
						if (flag)
						{
							break;
						}
						flag = true;
					}
				}
				flag = false;
				int k;
				for (k = centers[i].X; k < image.Length; k++)
				{
					if (image[k][y] && !image[k + 1][y])
					{
						if (flag)
						{
							break;
						}
						flag = true;
					}
				}
				array[i] = k - j + 1;
			}
			return array;
		}
		internal static int calcRoughVersion(Point[] center, int[] width)
		{
			int dECIMAL_POINT = QRCodeImageReader.DECIMAL_POINT;
			int num = new Line(center[0], center[1]).Length << dECIMAL_POINT;
			int num2 = (width[0] + width[1] << dECIMAL_POINT) / 14;
			int num3 = (num / num2 - 10) / 4;
			if ((num / num2 - 10) % 4 >= 2)
			{
				num3++;
			}
			return num3;
		}
		internal static int calcExactVersion(Point[] centers, int[] angle, int[] moduleSize, bool[][] image)
		{
			bool[] array = new bool[18];
			Point[] array2 = new Point[18];
			Axis axis = new Axis(angle, moduleSize[1]);
			axis.Origin = centers[1];
			for (int i = 0; i < 6; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					Point point = axis.translate(j - 7, i - 3);
					array[j + i * 3] = image[point.X][point.Y];
					array2[j + i * 3] = point;
				}
			}
			FinderPattern.canvas.drawPoints(array2, Color_Fields.RED);
			int result = 0;
			try
			{
				result = FinderPattern.checkVersionInfo(array);
			}
			catch (InvalidVersionInfoException)
			{
				FinderPattern.canvas.println("Version info error. now retry with other place one.");
				axis.Origin = centers[2];
				axis.ModulePitch = moduleSize[2];
				for (int j = 0; j < 6; j++)
				{
					for (int i = 0; i < 3; i++)
					{
						Point point = axis.translate(j - 3, i - 7);
						array[i + j * 3] = image[point.X][point.Y];
						array2[j + i * 3] = point;
					}
				}
				FinderPattern.canvas.drawPoints(array2, Color_Fields.RED);
				try
				{
					result = FinderPattern.checkVersionInfo(array);
				}
				catch (VersionInformationException ex)
				{
					throw ex;
				}
			}
			return result;
		}
		internal static int checkVersionInfo(bool[] target)
		{
			int num = 0;
			int i;
			for (i = 0; i < FinderPattern.VersionInfoBit.Length; i++)
			{
				num = 0;
				for (int j = 0; j < 18; j++)
				{
					if (target[j] ^ (FinderPattern.VersionInfoBit[i] >> j) % 2 == 1)
					{
						num++;
					}
				}
				if (num <= 3)
				{
					break;
				}
			}
			if (num <= 3)
			{
				return 7 + i;
			}
			throw new InvalidVersionInfoException("Too many errors in version information");
		}
		static FinderPattern()
		{
			FinderPattern.VersionInfoBit = new int[]
			{
				31892,
				34236,
				39577,
				42195,
				48118,
				51042,
				55367,
				58893,
				63784,
				68472,
				70749,
				76311,
				79154,
				84390,
				87683,
				92361,
				96236,
				102084,
				102881,
				110507,
				110734,
				117786,
				119615,
				126325,
				127568,
				133589,
				136944,
				141498,
				145311,
				150283,
				152622,
				158308,
				161089,
				167017
			};
			FinderPattern.canvas = QRCodeDecoder.Canvas;
		}
	}
}
