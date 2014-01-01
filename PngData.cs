using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace CodeCombatGridmancer
{
	internal class PngData
	{
		public Color Background { get; private set; }
		public Color Grid01 { get; private set; }
		public Color Grid05 { get; private set; }
		public Color Grid10 { get; private set; }
		public Color AreaGrid { get; private set; }

		public Color AreaBorder { get; private set; }
		public Color ConcaveVertex { get; private set; }
		public Color Diagonal { get; private set; }
		public Color MaximumCoverage { get; private set; }
		public Color Slice { get; private set; }

		public int CellSize { get; private set; }
		public int Padding { get; private set; }

		public bool ShowInitialBlank { get; set; }
		public bool ShowGrid { get; set; }
		public bool ColorGrid05 { get; set; }
		public bool ColorGrid10 { get; set; }
		public bool ShowAreaBorder { get; set; }
		public bool ShowAreaGrid { get; set; }
		public bool ShowConcaveVertex { get; set; }
		public bool ShowDiagonal { get; set; }
		public bool ShowMaximumCoverage { get; set; }
		public bool ShowSlice { get; set; }
		public bool GridFront { get; set; }


		private int _ticksPerSecond = 100;
		public int TicksPerSecond
		{
			get
			{
				return _ticksPerSecond;
			}
			set
			{
				_ticksPerSecond = value;
				
			}
		}

		private string _filename="rectangle";
		private bool _filenameSet;

		/// <summary>
		/// Write once value for the entire sequence.
		/// </summary>
		public string FileNameBase
		{
			get { return _filename; }
			set
			{
				if (_filenameSet)
				{
					return;
				}
				if (string.IsNullOrWhiteSpace(value))
				{
					return;
				}
				_filename = value;
				_filenameSet = true;
			}
		}

		private int _imageCount;
		public int Output{get { return _imageCount; }}

		public double SecondsShowCompleteStage { get; set; }
		public double SecondsShowConcaveVertex { get; set; }
		public double SecondsShowDiagonal { get; set; }
		public double SecondsShowMaximumCoverage { get; set; }
		public double SecondsShowSlice { get; set; }
		public double SecondsShowFinal { get; set; }
		public double SecondsShowBlank { get; set; }

		private readonly List<Color> _areaColors = new List<Color>();

		private Bitmap _bitmap;

		public PngData()
		{
			Background = Color.FromArgb(0, 0, 0);
			Grid01 = Color.LightGray;
			Grid05 = Color.DarkCyan;
			Grid10 = Color.Cyan;
			AreaGrid = Color.DarkSlateGray;
			AreaBorder = Color.OldLace;
			ConcaveVertex = Color.Yellow;
			Diagonal = Color.HotPink;
			MaximumCoverage = Color.Red;
			Slice = Color.White;

			CellSize = 10;
			Padding = 5;
			ShowGrid = true;
			ColorGrid05 = true;
			ColorGrid10 = true;
			ShowAreaBorder = true;
			ShowAreaGrid = true;
			ShowConcaveVertex = true;
			ShowDiagonal = true;
			ShowMaximumCoverage = true;
			ShowSlice = true;
			GridFront = false;
			ShowInitialBlank = true;

			SecondsShowCompleteStage = 0.75;
			SecondsShowConcaveVertex = 0.25;
			SecondsShowDiagonal = 0.25;
			SecondsShowMaximumCoverage = 0.5;
			SecondsShowSlice = 0.25;
			SecondsShowFinal = 10;
			SecondsShowBlank = 0.25;

			SetColorBase(null);
		}

		public void DrawBase(List<Cell> cells)
		{
			int mapWidth = cells.Max(c => c.X) + 1;
			int mapHeight = cells.Max(c => c.Y) + 1;

			DrawGrid(mapWidth,mapHeight);
	
			string fn = _filename + _imageCount.ToString("D5") + ".png";
			_bitmap.Save(fn,ImageFormat.Png);
			_imageCount++;
		}

		public void DrawAreas(Dictionary<int, List<Cell>> areas)
		{
			foreach (KeyValuePair<int, List<Cell>> keyValuePair in areas)
			{
				int index = (keyValuePair.Key - 1) % _areaColors.Count;

				foreach (Cell cell in keyValuePair.Value)
				{
					int x = cell.X;
					int y = cell.Y;

					int height = _bitmap.Height;

					int startX = Padding + x * ( CellSize + 1 ) + 1;
					int startY = height - Padding - ( y + 1 ) * ( CellSize + 1 );
					using (Graphics g = Graphics.FromImage(_bitmap))
					{
						g.FillRectangle(
							new SolidBrush(_areaColors[index]),
							startX,startY,CellSize,CellSize
							);
					}
				}
			}
			string fn = _filename + _imageCount.ToString("D5") + ".png";
			_bitmap.Save(fn, ImageFormat.Png);
			_imageCount++;
		}


		#region Color handling
		public void SetColorBase(int lowlevel, int stepRed, int stepGreen, int stepBlue)
		{
			lowlevel = lowlevel % 256;
			int span = 255 - lowlevel;

			int red = lowlevel;
			int green = lowlevel;
			int blue = lowlevel;
			for (int i = 0; i < 200; i++)
			{
				_areaColors.Add(Color.FromArgb(red, green, blue));
				red = ( red + stepRed ) % span + lowlevel;
				green = (green + stepGreen) % span + lowlevel;
				blue = (blue + stepBlue) % span + lowlevel;
			}
		}

		public void SetColorBase(IEnumerable<Color> colors)
		{
			List<Color> palette;
			if (colors == null)
			{
				palette=new List<Color>();
			}
			else
			{
				palette = new List<Color>(colors);
			}

			if (colors == null || !palette.Any())
			{
				//	On your head be it.  I'll give you a default.
				SetColorBase(153, 31, 37, 41);
				return;
			}

			int count = palette.Count();
			for (int i = 0; i < 200; i++)
			{
				_areaColors.Add(palette[i%count]);
			}
		}
		#endregion


		private void DrawGrid(int width, int height)
		{
			int areaPixelWidth = width * CellSize + width + 1 + Padding * 2;
			int areaPixelHeight = height * CellSize + height + 1 + Padding * 2;

			_bitmap = new Bitmap(areaPixelWidth, areaPixelHeight);
			using (Graphics g = Graphics.FromImage(_bitmap))
			{
				g.FillRectangle(new SolidBrush(Background), 0, 0, areaPixelWidth, areaPixelHeight);
				Pen p10 = new Pen(Grid10);
				Pen p05 = new Pen(Grid05);
				using (Pen p01 = new Pen(Grid01))
				{
					//	Draw vertical
					for (int i = 0; i <= width; i++)
					{
						Pen p = p01;
						if (i % 5 == 0)
						{
							p = p05;
						}
						if (i % 10 == 0)
						{
							p = p10;
						}

						g.DrawLine(
						           p,
							Padding + ( CellSize + 1 ) * i,
							Padding,
							Padding + ( CellSize + 1 ) * i,
							( CellSize + 1 ) * height + Padding
							);
					}
					//	Draw horizontal
					for (int i = 0; i <= height; i++)
					{
						Pen p = p01;
						if (i % 5 == 0)
						{
							p = p05;
						}
						if (i % 10 == 0)
						{
							p = p10;
						}
						g.DrawLine(
								   p,
							Padding,
							Padding + (CellSize + 1) * (height - i),
							(CellSize + 1) * width + Padding,
							Padding + (CellSize + 1) * (height - i)
							);

					}
				}
			}
		}

	}
}
