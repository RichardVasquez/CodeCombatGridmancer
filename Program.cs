using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CodeCombatGridmancer
{
	class Program
	{
		private static readonly List<Cell> MapCells = new List<Cell>();

		private static Dictionary<int,List<Cell>> _areas = new Dictionary<int, List<Cell>>();

		private static readonly Dictionary<int, List<Cell>> FinalAreas = new Dictionary<int, List<Cell>>();

		private static readonly char[] Splitters = {'\n', '\r'};

		private static OutputType _output = default ( OutputType );

		private static TimeSpan _timeSpan;

		private static bool _showStatus;

		private static	PngData _gd = new PngData();

		static void Main(string[] args)
		{
			//	setups don't get timed.
			ReadMap();
			LinkMap();
			SetOutput();
			SetShowStatus();

			if (_output == OutputType.Png)
			{
				_gd = new PngData();
				_gd.DrawBase(MapCells);
			}

			var cells = MapCells.ToList();
			_areas = MapCellsToAreas(cells);

			if (_output == OutputType.Png)
			{
				_gd.DrawAreas(_areas);
			}


			Stopwatch sw = new Stopwatch();
			sw.Start();
			var firstPieces = FindInitialAreas();
			ProcessAreas(firstPieces);
			sw.Stop();
			_timeSpan = sw.Elapsed;
			DoOutput();
		}

		private static void SetShowStatus()
		{
			string s = ConfigurationManager.AppSettings["showStatus"];
			if (string.IsNullOrEmpty(s))
			{
				_showStatus = false;
				return;
			}

			s = s.Trim().ToUpperInvariant();
			_showStatus = s == "1" || s == "TRUE" || s == "T" || s == "Y";
		}

		private static void DoOutput()
		{
			switch (_output)
			{
				case OutputType.Screen:
					OutputScreen();
					break;
				case OutputType.Text:
					OutputText();
					break;
				case OutputType.CodeCombatGridmancer:
					OutputGridmancer();
					break;
				case OutputType.Png:
					OutputPng();
					break;
				default:
					Notice("No known output type.");
					break;
			}
		}

		private static void OutputPng()
		{
			Console.WriteLine("PNG ouotput is pretty basic right now.");
			Console.WriteLine("You should have {0} images starting with {1} in your directory.", _gd.Output, _gd.FileNameBase);
			Console.ReadLine();
		}

		private static void OutputGridmancer()
		{
			const int scale = 4;
			List<string> res = (
				from pair in FinalAreas
				let minX = pair.Value.Min(p => p.X)
				let minY = pair.Value.Min(p => p.Y)
				let maxX = pair.Value.Max(p => p.X) + 1
				let maxY = pair.Value.Max(p => p.Y) + 1
				let sizeX = maxX - minX
				let sizeY = maxY - minY
				let midX = ( minX + maxX ) * scale / 2
				let midY = ( minY + maxY ) * scale / 2
				select
					string.Format("this.addRect({0}, {1}, {2}, {3});", midX, midY, sizeX * scale, sizeY * scale)
				)
				.ToList();

			Console.WriteLine("// Mapped in {0} seconds.", _timeSpan.TotalSeconds);
			foreach (string re in res)
			{
				Console.WriteLine(re);
			}
			Console.ReadLine();
		}

		private static void OutputText()
		{
			string s = ConfigurationManager.AppSettings[ "outputFile" ];
			if (string.IsNullOrEmpty(s))
			{
				s = "tada.txt";
			}

			int max = FinalAreas.Keys.Max();
			int height = MapCells.Max(mc => mc.X) + 1;
			int width = MapCells.Max(mc => mc.Y) + 1;
			int spaceCount = max.ToString().Length + 2;
			string space = new string(' ', spaceCount);
			string format = "D" + ( spaceCount - 2 );
			string[][] output = new string[ height ][];
			for (int y = 0; y < height; y++)
			{
				output[ y ] = new string[ width ];
				for (int x = 0; x < width; x++)
				{
					output[ y ][ x ] = space;
				}
			}

			foreach (var area in FinalAreas)
			{
				string idx = string.Format( "[{0}]", area.Key.ToString(format));
				foreach (var cell in area.Value)
				{
					output[ cell.Y ][ cell.X ] = idx;
				}
			}

			List<string> res = output.Select(strings => string.Join(" ", strings)).ToList();
			res.Reverse();
			res.Add("");
			res.Add(string.Format("Mapped in {0} seconds.", _timeSpan.TotalSeconds));
			File.WriteAllLines(s,res);
		}

		/// <summary>
		/// Simple colored text output on screen.
		/// </summary>
		private static void OutputScreen()
		{
			const string s = "ABCDE";
			ConsoleColor[] cc = { ConsoleColor.Green, ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.Magenta};

			int wWidth = Console.WindowWidth;
			int wHeight = Console.WindowHeight;
			int iWidth = wWidth;
			int iHeight = wHeight;

			int mWidth = MapCells.Max(mc => mc.X) + 1;
			int mHeight = MapCells.Max(mc => mc.Y) + 1;

			if (wWidth - mWidth <= 0)
			{
				wWidth = mWidth + 1;
			}
			if (wHeight - mHeight <= -1)
			{
				wHeight = wHeight + 2;
			}

			Console.SetWindowSize(wWidth,wHeight);
			Console.Clear();
			Console.Title = string
				.Format(
				        "{0} rectangles on polygon in map space of {1}x{2} ({3} total cells)",
					FinalAreas.Count, mWidth, mHeight,
					FinalAreas.Sum(fa=>fa.Value.Count)
					);
			ConsoleColor fg = Console.ForegroundColor;
			bool cv = Console.CursorVisible;

			Console.CursorVisible = false;
			foreach (var mapPair in FinalAreas)
			{
				int index = mapPair.Key;
				char c = s[ index%s.Length];
				Console.ForegroundColor = cc[ index % cc.Length ];

				foreach (var cell in mapPair.Value)
				{
					Console.SetCursorPosition(cell.X,(wHeight-1)-cell.Y-(wHeight-mHeight));
					Console.Write(c);
				}
			}

			Console.SetCursorPosition(0, mHeight + 1);
			Console.ForegroundColor = fg;
			Console.WriteLine("Mapped in {0} seconds.", _timeSpan.TotalSeconds);
			Console.CursorVisible = cv;
			Console.ReadLine();
			Console.SetWindowSize(iWidth, iHeight);
		}

		/// <summary>
		/// This is the initial case, where polygon diagonals need to be found for
		/// initial slicing based on the maximum independent set of the crossing
		/// diagonals.  Initial information that led me to the solution can be
		/// found at:
		/// http://stackoverflow.com/questions/5919298/algorithm-for-finding-the-fewest-rectangles-to-cover-a-set-of-rectangles/6634668#6634668
		/// 
		/// From there, the links provided in the answer present a starting point.
		/// </summary>
		/// <returns></returns>
		private static Dictionary<int, List<Cell>> FindInitialAreas()
		{
			Dictionary<int,List<Cell>>res = new Dictionary<int, List<Cell>>();
			int index = 1;

			foreach (var key in _areas.Keys)
			{
				var lines = GetEdgeLines(_areas[key]);

				HashSet<Vertex> concaves = new HashSet<Vertex>();
				foreach (Vertex concaveVertex in _areas[key].SelectMany(cell => cell.ConcaveVertices))
				{
					concaves.Add(concaveVertex);
				}
				List<Vertex> allConcaves = concaves.OrderBy(v => v.Y).ThenBy(v => v.X).ToList();

				if (allConcaves.Count == 0)
				{
					StoreArea(_areas[ key ]);
					continue;
				}

				var diagonals = FindDiagonals(allConcaves, lines);
				var intersectionGraph = GetIntersectionGraph(diagonals);
				Dictionary<Line, int> sliceMap = new Dictionary<Line, int>();
				if (intersectionGraph.Count != 0)
				{
					sliceMap = GetSliceMap(intersectionGraph);
				}

				//	We now have a list of *must slice* lines in sliceMap
				List<Cell> newCells = _areas[key].ToList();
				foreach (KeyValuePair<Line, int> keyValuePair in sliceMap)
				{
					Slice(newCells, keyValuePair.Key);
				}
				var temp = MapCellsToAreas(newCells);

				foreach (KeyValuePair<int, List<Cell>> keyValuePair in temp)
				{
					var cells = keyValuePair.Value;
					foreach (Cell cell in cells)
					{
						cell.AreaIndex = index;
					}
					res[ index ] = cells;
					index++;
				}
			}
			return res;
		}

		/// <summary>
		/// Store a rectangle as part of the final answer.
		/// </summary>
		/// <param name="cells"></param>
		private static void StoreArea(List<Cell> cells)
		{
			int current = FinalAreas.Count + 1;
			if (_showStatus)
			{
				Important(string.Format("Storing area {0}", current));
			}

			foreach (var cell in cells)
			{
				cell.AreaIndex = current;
			}
			FinalAreas[ current ] = cells;
			if (_output == OutputType.Png)
			{
				_gd.DrawAreas(FinalAreas);
			}
		}

		/// <summary>
		/// This processes polygons to determine where to slice them in order to find rectangles.
		/// </summary>
		private static void ProcessAreas(Dictionary<int, List<Cell>> areas)
		{
			Queue<List<Cell>> areasQueue = new Queue<List<Cell>>();

			foreach (var pair in areas)
			{
				areasQueue.Enqueue(pair.Value);
			}

			while (areasQueue.Count > 0)
			{
				if (_showStatus)
				{
					Console.WriteLine("{0} area{1} to process.", areasQueue.Count, areasQueue.Count == 1 ? "" : "s");
				}
				var newArea = areasQueue.Dequeue();
				newArea = ResetArea(newArea);
				var lines = GetEdgeLines(newArea);
				HashSet<Vertex> concaves = new HashSet<Vertex>();
				foreach (Vertex concaveVertex in newArea.SelectMany(cell => cell.ConcaveVertices))
				{
					concaves.Add(concaveVertex);
				}
				List<Vertex> allConcaves = concaves.OrderBy(v => v.Y).ThenBy(v => v.X).ToList();

				//	We're done with this rectangle, store it.
				if (allConcaves.Count == 0)
				{
					StoreArea(newArea);
					continue;
				}

				//	Slice on a diagonal and enqueue the results
				var diagonals = FindDiagonals(allConcaves, lines);
				if (diagonals.Count > 0)
				{
					int max = diagonals.Max(d => d.Length);
					Line l = diagonals.First(d => d.Length == max);
					List<Cell> newCells = newArea.ToList();
					Slice(newCells, l);
					var possibles = MapCellsToAreas(newCells);
					foreach (var possible in possibles)
					{
						if (_showStatus)
						{
							Console.WriteLine("Enqueueing....");
						}
						areasQueue.Enqueue(possible.Value);
					}
					continue;
				}

				//	Take a concave, find a valid closest wall, and slice to it.
				if (allConcaves.Count > 0)
				{
					Vertex bad1 = new Vertex(int.MaxValue, int.MaxValue);
					Vertex bad2 = new Vertex(int.MaxValue - 1, int.MaxValue);
					Vertex bad3 = new Vertex(int.MaxValue, int.MaxValue - 1);

					Vertex v = allConcaves.First();
					var vLines = lines.Where(l => l.Vertical && !(l.V1==v||l.V2==v)).OrderBy(l => Math.Abs(l.V1.X - v.X)).ToList();
					var hLines = lines.Where(l => l.Horizontal && !(l.V1==v||l.V2==v)).OrderBy(l => Math.Abs(l.V1.Y - v.Y)).ToList();

					Line hLineTest = new Line();
					foreach (var hLine in hLines)
					{
						Vertex v2 = new Vertex(v.X, hLine.V1.Y);
						if (TestLineIntersection(v, v2, out hLineTest, lines))
						{
							hLineTest = new Line(v, v2);
							break;
						}
						hLineTest = new Line(bad1, bad2);
					}

					Line vLineTest = new Line();
					foreach (Vertex v2 in vLines.Select(vLine => new Vertex(vLine.V1.X, v.Y)))
					{
						if (TestLineIntersection(v, v2, out vLineTest, lines))
						{
							vLineTest = new Line(v, v2);
							break;
						}
						vLineTest = new Line(bad1, bad3);
					}

					int vLineDist1 = Math.Abs(v.X - vLineTest.V1.X);
					int vLineDist2 = Math.Abs(v.X - vLineTest.V2.X);
					int vLineDist = vLineDist1 < vLineDist2 ? vLineDist2 : vLineDist1;

					int hLineDist1 = Math.Abs(v.Y - hLineTest.V1.Y);
					int hLineDist2 = Math.Abs(v.Y - hLineTest.V2.Y);
					int hLineDist = hLineDist1 < hLineDist2 ? hLineDist2 : hLineDist1;

					Line slicer = vLineDist < hLineDist ? vLineTest : hLineTest;
					
					List<Cell> newCells = newArea.ToList();
					Slice(newCells, slicer);
					var possibles = MapCellsToAreas(newCells);
					foreach (var possible in possibles)
					{
						if (_showStatus)
						{
							Console.WriteLine("Enqueueing....");
						}
						areasQueue.Enqueue(possible.Value);
					}
				}
			}
		}

		/// <summary>
		/// I had some noise starting to build up in the Cell class at one point,
		/// so this was coded to reinforce the structure.  I believe it's not
		/// needed anymore, but I'll save that for a later commit after I finish the
		/// output code.
		/// </summary>
		private static List<Cell> ResetArea(List<Cell> newArea)
		{
			Func<int, int, List<Cell>, Cell> findCellFunc =
				(x, y, list) =>
					list.FirstOrDefault(l => l.X == x && l.Y == y);

			List<Cell> res =
				newArea
					.Select(
					        cell =>
						        new Cell {X = cell.X, Y = cell.Y, Occupied = cell.Occupied}
					)
					.OrderBy(c => c.Y).ThenBy(c => c.X).ToList();

			foreach (Cell cell in res)
			{
				cell.CellAbove = findCellFunc(cell.X, cell.Y + 1, res);
				cell.CellBelow = findCellFunc(cell.X, cell.Y - 1, res);
				cell.CellLeft = findCellFunc(cell.X - 1, cell.Y, res);
				cell.CellRight = findCellFunc(cell.X + 1, cell.Y, res);
				cell.CellAboveRight = findCellFunc(cell.X + 1, cell.Y + 1, res);
				cell.CellAboveLeft = findCellFunc(cell.X - 1, cell.Y + 1, res);
				cell.CellBelowRight = findCellFunc(cell.X + 1, cell.Y - 1, res);
				cell.CellBelowLeft = findCellFunc(cell.X - 1, cell.Y - 1, res);
			}

			foreach (Cell cell in res)
			{
				cell.WallLeft = findCellFunc(cell.X, cell.Y, newArea).WallLeft;
				cell.WallRight = findCellFunc(cell.X, cell.Y, newArea).WallRight;
				cell.WallTop = findCellFunc(cell.X, cell.Y, newArea).WallTop;
				cell.WallBottom = findCellFunc(cell.X, cell.Y, newArea).WallBottom;
			}

			return res;
		}

		/// <summary>
		/// Find the edges to the polygon
		/// </summary>
		private static List<Line> GetEdgeLines(IEnumerable<Cell> cells)
		{
			Dictionary<Vertex, int> test = new Dictionary<Vertex, int>();
			foreach (var cell in cells)
			{
				var ve = cell.AllVertices;
				foreach (var vertex in ve)
				{
					if (!test.ContainsKey(vertex))
					{
						test[ vertex ] = 0;
					}
					test[ vertex ]++;
				}
			}

			//	Get horizontals
			List<int> ySpots = test.Keys.Select(v => v.Y).Distinct().ToList();
			ySpots.Sort();

			List<Vertex> horizontals = new List<Vertex>();
			foreach (
				var node in
					ySpots
						.Select(
						        val => test.Where(t => t.Key.Y == val && t.Value %2 != 0)
							        .OrderBy(t => t.Key.X)
							        .ToList()))
			{
				horizontals.AddRange(node.Select(keyValuePair => keyValuePair.Key));
			}

			//	Get verticals
			List<int> xSpots = test.Keys.Select(v => v.X).Distinct().ToList();
			ySpots.Sort();

			List<Vertex> verticals = new List<Vertex>();
			foreach (
				var node in
					xSpots
						.Select(
						        val => test.Where(t => t.Key.X == val && t.Value %2 != 0)
							        .OrderBy(t => t.Key.Y)
							        .ToList()))
			{
				verticals.AddRange(node.Select(keyValuePair => keyValuePair.Key));
			}

			List<Line> lines = new List<Line>();
			List<Line> horizontalEdges = new List<Line>();
			List<Line> verticalEdges = new List<Line>();
			for (int i = 0; i < horizontals.Count; i += 2)
			{
				lines.Add(new Line(horizontals[ i ], horizontals[ i + 1 ]));
				horizontalEdges.Add(new Line(horizontals[ i ], horizontals[ i + 1 ]));
			}
			for (int i = 0; i < verticals.Count; i += 2)
			{
				lines.Add(new Line(verticals[ i ], verticals[ i + 1 ]));
				verticalEdges.Add(new Line(verticals[ i ], verticals[ i + 1 ]));
			}
			return lines;
		}

		/// <summary>
		/// Find the lines, if any, that make up the maximum independent set
		/// of intersecting diagonals for determining where to slice our
		/// initial polygon.
		/// </summary>
		private static Dictionary<Line, int> GetSliceMap(Dictionary<Line, List<Line>> intersectionGraph)
		{
			Dictionary<Line, int> sliceMap = new Dictionary<Line, int>();
			int index = 1;
			foreach (
				var pair in intersectionGraph
					.Where(pair => pair.Value.Count != 0)
					.Where(pair => !sliceMap.ContainsKey(pair.Key))
				)
			{
				sliceMap[ pair.Key ] = index++;
			}

			var vc = new VertexCover(sliceMap.Count);
			foreach (var pair in intersectionGraph)
			{
				if (pair.Value.Count <= 0)
				{
					continue;
				}
				foreach (Line line in pair.Value)
				{
					int u = sliceMap[ pair.Key ];
					int v = sliceMap[ line ];
					vc.Add(u, v);
				}
			}

			var vcData = vc.FindCover(sliceMap.Count);
			if (vcData.Count <= 0)
			{
				return sliceMap;
			}
			
			int smallest = vcData.Min(s => s.Size);
			var set = vcData.FirstOrDefault(v => v.Size == smallest);
			if (set == null)
			{
				return sliceMap;
			}

			for (int i = 0; i < set.Size; i++)
			{
				var pair = sliceMap.FirstOrDefault(n => n.Value == set[ i ]);
				sliceMap.Remove(pair.Key);
			}
			return sliceMap;
		}

		/// <summary>
		/// Find concave vertices that are diagonal to each other
		/// in the polygon connected and can be connected to find
		/// our maximum independent set.
		/// </summary>
		private static Dictionary<Line, List<Line>> GetIntersectionGraph(IEnumerable<Line> diagonals)
		{
			Dictionary<Line, List<Line>> intersectionGraph = new Dictionary<Line, List<Line>>();
			List<Line> diags = diagonals.ToList();
			foreach (Line line in diags)
			{
				intersectionGraph[ line ] = new List<Line>();
				List<Line> crossLines = line.Horizontal
					? diags.Where(cl => cl.Vertical).ToList()
					: diags.Where(cl => cl.Horizontal).ToList();

				Line checkLine = line;
				foreach (
					Line cross in crossLines
						.Where(cross => cross.Intersects(checkLine))
						.Where(
						       cross => checkLine.V1 != cross.V1 &&
						                checkLine.V2 != cross.V2 &&
						                checkLine.V1 != cross.V2 &&
						                checkLine.V2 != cross.V1
						)
					)
				{
					intersectionGraph[ line ].Add(cross);
				}
			}
			return intersectionGraph;
		}

		/// <summary>
		/// After a slice, a polygon will be in 1 or 2 pieces.  This maps the cells and
		/// their connectivity into the appropriate areas.
		/// </summary>
		private static Dictionary<int, List<Cell>> MapCellsToAreas(List<Cell> cells)
		{
			Dictionary<int, List<Cell>> res = new Dictionary<int, List<Cell>>();
			foreach (Cell cell in cells)
			{
				cell.AreaIndex = 0;
			}

			Cell start = cells.FirstOrDefault(c=>c.Occupied && c.AreaIndex==0);
			HashSet<Cell> visited = new HashSet<Cell>();

			int index = 0;
			while (start != null)
			{
				index++;
				FloodfFill(start, ref visited, index);
				start = cells.FirstOrDefault(c => c.Occupied && c.AreaIndex == 0);
			}

			var indices = visited.ToList().Select(i => i.AreaIndex).Distinct();
			foreach (int i in indices)
			{
				int i1 = i;
				res[ i ] = visited
					.ToList()
					.Where(v => v.AreaIndex == i1)
					.OrderBy(v => v.Y)
					.ThenBy(v => v.X)
					.ToList();
			}

			return res;
		}

		/// <summary>
		/// Recursive searching of neighbors for an area.
		/// </summary>
		private static void FloodfFill(Cell start, ref HashSet<Cell> visited, int index)
		{
			start.AreaIndex = index;
			visited.Add(start);
			var neighbors = start.Neighbors.Where(n => n.AreaIndex == 0).ToList();
			HashSet<Cell> set = visited;
			neighbors.RemoveAll(set.Contains);
			foreach (Cell neighbor in neighbors)
			{
				if (!set.Contains(neighbor))
				{
					FloodfFill(neighbor, ref visited, index);
				}
			}
		}

		#region Slicing code
		/// <summary>
		/// Determine if we're slicing vertically or horizontally, and follow
		/// the path down.  It's really simple code, but it's verbose, so it
		/// was split up into a series of branching functions.
		/// </summary>
		private static void Slice(List<Cell> cells, Line key)
		{
			if (key.Vertical)
			{
				SliceVertical(cells, key);
			}
			else
			{
				SliceHorizontal(cells, key);
			}
		}

		/// <summary>
		/// Places a slice on cells left and right of a line.
		/// </summary>
		private static void SliceVertical(List<Cell> cells, Line key)
		{
			SliceLeft(cells, key);
			SliceRight(cells, key);
		}

		/// <summary>
		/// Places a slice on cells to the left of a line by removing references to neighbors right.
		/// </summary>
		private static void SliceLeft(List<Cell> cells, Line key)
		{
			int start = key.V1.Y;
			int end = key.V2.Y;
			int x = key.V1.X;

			List<Cell> targetsLeft =
				cells.Where(c => c.Y >= start && c.Y < end && c.X == x - 1)
					.ToList();

			if (targetsLeft.Count == 0)
			{
				return;
			}

			var topCell = cells.FirstOrDefault(c => c.X==x-1 && c.Y == start - 1);
			if (topCell != null)
			{
				if (topCell.CellBelowRight != null)
				{
					topCell.CellBelowRight.CellAboveLeft = null;
					topCell.CellBelowRight = null;
					topCell.WallRight = true;
				}
			}

			foreach (Cell cell in targetsLeft)
			{
				cell.WallRight = true;
				if (cell.CellAboveRight != null)
				{
					cell.CellAboveRight.CellBelowLeft = null;
					cell.CellAboveRight = null;
				}
				if (cell.CellRight != null)
				{
					cell.CellRight.CellLeft = null;
					cell.CellRight = null;
				}
				if (cell.CellBelowRight == null)
				{
					continue;
				}
				cell.CellBelowRight.CellAboveLeft = null;
				cell.CellBelowRight = null;
			}

			var bottomCell = cells.FirstOrDefault(c => c.X == x-1 && c.Y == end);
			if (bottomCell == null)
			{
				return;
			}
			if (bottomCell.CellAboveRight == null)
			{
				return;
			}
			bottomCell.CellAboveRight.CellBelowLeft = null;
			bottomCell.CellAboveRight = null;
			bottomCell.WallRight = true;
		}

		/// <summary>
		/// Places a slice on cells to the right of a line by removing references to neighbors left.
		/// </summary>
		private static void SliceRight(List<Cell> cells, Line key)
		{
			int start = key.V1.Y;
			int end = key.V2.Y;
			int x = key.V1.X;

			List<Cell> targetsRight =
				cells.Where(c => c.Y >= start && c.Y < end && c.X == x)
					.ToList();

			if (targetsRight.Count == 0)
			{
				return;
			}

			var topCell = cells.FirstOrDefault(c => c.X == x && c.Y == start - 1);
			if (topCell != null)
			{
				if (topCell.CellBelowLeft != null)
				{
					topCell.CellBelowLeft.CellAboveRight = null;
					topCell.CellBelowLeft = null;
					topCell.WallLeft = true;
				}
			}

			foreach (Cell cell in targetsRight)
			{
				cell.WallLeft = true;
				if (cell.CellAboveLeft != null)
				{
					cell.CellAboveLeft.CellBelowRight = null;
					cell.CellAboveLeft = null;
				}
				if (cell.CellLeft != null)
				{
					cell.CellLeft.CellRight = null;
					cell.CellLeft = null;
				}
				if (cell.CellBelowLeft == null)
				{
					continue;
				}
				cell.CellBelowLeft.CellAboveRight = null;
				cell.CellBelowLeft = null;
			}

			var bottomCell = cells.FirstOrDefault(c => c.X == x && c.Y == end);
			if (bottomCell == null)
			{
				return;
			}
			if (bottomCell.CellAboveLeft == null)
			{
				return;
			}
			bottomCell.CellAboveLeft.CellBelowRight = null;
			bottomCell.CellAboveLeft = null;
			bottomCell.WallLeft = true;
		}

		/// <summary>
		/// Places a slice on cells above and below a line.
		/// </summary>
		private static void SliceHorizontal(List<Cell> cells, Line key)
		{
			SliceAbove(cells, key);
			SliceBelow(cells, key);
		}

		/// <summary>
		/// Places a slice on cells above a line by removing references to neighbors below.
		/// </summary>
		private static void SliceAbove(List<Cell> cells, Line key)
		{
			int start = key.V1.X;
			int end = key.V2.X;
			int y = key.V1.Y;

			var targetsAbove =
				cells.Where(c => c.X >= start && c.X < end && c.Y == y)
					.OrderBy(c => c.X).ToList();

			if (targetsAbove.Count == 0)
			{
				return;
			}

			var leftCell = cells.FirstOrDefault(c => c.Y == y && c.X == start - 1);
			if (leftCell != null)
			{
				if (leftCell.CellBelowRight != null)
				{
					leftCell.CellBelowRight.CellAboveLeft = null;
					leftCell.CellBelowRight = null;
					leftCell.WallBottom = true;
				}
			}

			foreach (Cell cell in targetsAbove)
			{
				cell.WallBottom = true;
				if (cell.CellBelowLeft != null)
				{
					cell.CellBelowLeft.CellAboveRight = null;
					cell.CellBelowLeft = null;
				}
				if (cell.CellBelow != null)
				{
					cell.CellBelow.CellAbove = null;
					cell.CellBelow = null;
				}
				if (cell.CellBelowRight == null)
				{
					continue;
				}
				cell.CellBelowRight.CellAboveLeft = null;
				cell.CellBelowRight = null;
			}

			var rightCell = cells.FirstOrDefault(c => c.Y == y && c.X == end);
			if (rightCell == null)
			{
				return;
			}
			if (rightCell.CellBelowLeft == null)
			{
				return;
			}
			rightCell.CellBelowLeft.CellAboveRight = null;
			rightCell.CellBelowLeft = null;
			rightCell.WallBottom = true;
		}

		/// <summary>
		/// Places a slice on cells below a line by removing references to neighbors above.
		/// </summary>
		private static void SliceBelow(List<Cell> cells, Line key)
		{
			int start = key.V1.X;
			int end = key.V2.X;
			int y = key.V1.Y;

			var targetsBelow =
				cells.Where(c => c.X >= start && c.X < end && c.Y == y - 1)
					.OrderBy(c => c.X).ToList();

			if (targetsBelow.Count == 0)
			{
				return;
			}

			var leftCell = cells.FirstOrDefault(c => c.Y == y-1 && c.X == start - 1);
			if (leftCell != null)
			{
				if (leftCell.CellAboveRight != null)
				{
					leftCell.CellAboveRight.CellBelowLeft = null;
					leftCell.CellAboveRight = null;
					leftCell.WallTop = true;
				}
			}

			foreach (Cell cell in targetsBelow)
			{
				cell.WallTop = true;
				if (cell.CellAboveLeft != null)
				{
					cell.CellAboveLeft.CellBelowRight = null;
					cell.CellAboveLeft = null;
				}
				if (cell.CellAbove != null)
				{
					cell.CellAbove.CellBelow = null;
					cell.CellAbove = null;
				}
				if (cell.CellAboveRight == null)
				{
					continue;
				}
				cell.CellAboveRight.CellBelowLeft = null;
				cell.CellAboveRight = null;
			}

			var rightCell = cells.FirstOrDefault(c => c.Y == y-1 && c.X == end);
			if (rightCell == null)
			{
				return;
			}
			if (rightCell.CellAboveLeft == null)
			{
				return;
			}
			rightCell.CellAboveLeft.CellBelowRight = null;
			rightCell.CellAboveLeft = null;
			rightCell.WallTop = true;
		}
		#endregion

		#region Line Intersection
		/// <summary>
		/// This maps all the polygon "diagonals" between concave vertices.
		/// </summary>
		private static HashSet<Line> FindDiagonals(List<Vertex> allConcaves, List<Line> lines)
		{
			HashSet<Line> diagonals = new HashSet<Line>();
			foreach (Vertex concave in allConcaves)
			{
				Vertex reference = concave;
				List<Vertex> axisNeighbors =
					allConcaves.Where(cv => cv.X == reference.X || cv.Y == reference.Y).ToList();
				axisNeighbors.Remove(reference);

				var leftNeighbors = axisNeighbors.Where(an => an.X < reference.X).OrderBy(an => an.X);
				Vertex leftVertex = new Vertex(Int32.MinValue, Int32.MinValue);
				if (leftNeighbors.Any())
				{
					leftVertex = leftNeighbors.Last();
				}

				Vertex rightVertex = new Vertex(Int32.MinValue, Int32.MinValue);
				var rightNeighbors = axisNeighbors.Where(an => an.X > reference.X).OrderBy(an => an.X);
				if (rightNeighbors.Any())
				{
					rightVertex = rightNeighbors.First();
				}

				var topNeighbors = axisNeighbors.Where(an => an.Y > reference.Y).OrderBy(an => an.Y);
				Vertex topVertex = new Vertex(Int32.MinValue, Int32.MinValue);
				if (topNeighbors.Any())
				{
					topVertex = topNeighbors.First();
				}

				var bottomNeighbors = axisNeighbors.Where(an => an.Y < reference.Y).OrderBy(an => an.Y);
				Vertex bottomVertex = new Vertex(Int32.MinValue, Int32.MinValue);
				if (bottomNeighbors.Any())
				{
					bottomVertex = bottomNeighbors.Last();
				}

				Line testLine;
				if (TestLineIntersection(reference, leftVertex, out testLine, lines))
				{
					diagonals.Add(testLine);
				}
				if (TestLineIntersection(reference, rightVertex, out testLine, lines))
				{
					diagonals.Add(testLine);
				}
				if (TestLineIntersection(reference, topVertex, out testLine, lines))
				{
					diagonals.Add(testLine);
				}
				if (TestLineIntersection(reference, bottomVertex, out testLine, lines))
				{
					diagonals.Add(testLine);
				}
			}
			return diagonals;
		}

		/// <summary>
		/// Checks to see if a theoretical line between two vertices will intersect with a list of lines.
		/// </summary>
		/// <param name="reference">Starting vertex</param>
		/// <param name="testVertex">Ending vertex</param>
		/// <param name="test">Line to modify in return</param>
		/// <param name="lines">List of lines to test</param>
		/// <returns>True if intersections.</returns>
		private static bool TestLineIntersection(Vertex reference, Vertex testVertex, out Line test, List<Line> lines)
		{
			test = default ( Line );
			if (testVertex.X == Int32.MinValue && testVertex.Y == Int32.MinValue)
			{
				return false;
			}

			Line testing = new Line(reference, testVertex);
			if (testing.Point)
			{
				return false;
			}

			//	Find lines that may contain the test line
			List<Line> testOverLines = testing.Horizontal
				? lines.Where(li => li.Horizontal && li.V1.Y == testing.V1.Y).ToList()
				: lines.Where(li => li.Vertical && li.V1.X == testing.V1.X).ToList();

			if (testOverLines.Any(tol => tol.Contains(testing)))
			{
				return false;
			}

			//	Test to see if the line contains any edge lines
			if (testOverLines.Any(testing.Contains))
			{
				return false;
			}

			//	Find lines that may intersect
			List<Line> testCrossLines = testing.Horizontal
				? lines.Where(li => li.Vertical && li.V1.X > testing.V1.X && li.V1.X < testing.V2.X).ToList()
				: lines.Where(li => li.Horizontal && li.V1.Y > testing.V1.Y && li.V1.Y < testing.V2.Y).ToList();

			//	No lines between.  We pass.
			if (testCrossLines.Count == 0)
			{
				test = testing;
				return true;
			}

			//	Nothing intersects.  We pass.
			if (testCrossLines.Any(tcl => tcl.Intersects(testing)))
			{
				return false;
			}

			test = testing;
			return true;
		}
		#endregion

		#region Setup
		/// <summary>
		/// Reads the text representation of the map.
		/// </summary>
		private static void ReadMap()
		{
			string s = ConfigurationManager.AppSettings[ "mapName" ];
			if (string.IsNullOrEmpty(s))
			{
				s = "Map.txt";
			}

			if (!File.Exists(s))
			{
				Error(string.Format("Could not find file 's'!"));
				throw new ConfigurationErrorsException();
			}

			string map = File.ReadAllText(s);
			var mapParts = map.Split(Splitters, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < mapParts.Length; i++)
			{
				mapParts[ i ] = mapParts[ i ].Trim();
			}

			int k = mapParts[ 0 ].Length;
			if (mapParts.Any(l => l.Length != k))
			{
				Error("The map is uneven in width.");
				throw new Exception();
			}

			int height = mapParts.Length;
			int width = mapParts[ 0 ].Length;

			for(int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					MapCells.Add(new Cell {X = x, Y = ( height - y ) - 1, Occupied = mapParts[ y ][ x ] == '#'});
				}
			}
		}

		/// <summary>
		/// Links cells to their neighbors.
		/// </summary>
		private static void LinkMap()
		{
			int height = MapCells.Max(c => c.Y);
			int width = MapCells.Max(c => c.X);

			for (int y = 0; y <= height; y++)
			{
				for (int x = 0; x <= width; x++)
				{
					Cell cell = MapCells.FirstOrDefault(c => c.X == x && c.Y == y);
					if (cell == null)
					{
						continue;
					}

					LinkNode(cell, -1, 1, LocationType.AboveLeft, height+1, width+1);
					LinkNode(cell, 0, 1, LocationType.Above, height + 1, width + 1);
					LinkNode(cell, 1, 1, LocationType.AboveRight, height + 1, width + 1);
					LinkNode(cell, -1, 0, LocationType.Left, height + 1, width + 1);
					LinkNode(cell, 1, 0, LocationType.Right, height + 1, width + 1);
					LinkNode(cell, -1, -1, LocationType.BelowLeft, height + 1, width + 1);
					LinkNode(cell, 0, -1, LocationType.Below, height + 1, width + 1);
					LinkNode(cell, 1, -1, LocationType.BelowRight, height + 1, width + 1);
				}
			}
		}

		/// <summary>
		/// Links a specific node to a specified neighbor if it exists.
		/// </summary>
		private static void LinkNode(Cell cell, int xOffset, int yOffset, LocationType locationName, int height, int width)
		{
			int newX = cell.X + xOffset;
			int newY = cell.Y + yOffset;

			if (newX < 0 || newX >= width || newY < 0 || newY >= height)
			{
				return;
			}

			var link = MapCells.FirstOrDefault(c => c.X == newX && c.Y == newY);
			if (link == null)
			{
				return;
			}

			switch (locationName)
			{
				case LocationType.AboveLeft:
					cell.CellAboveLeft = link;
					break;
				case LocationType.Above:
					cell.CellAbove = link;
					break;
				case LocationType.AboveRight:
					cell.CellAboveRight = link;
					break;
				case LocationType.Left:
					cell.CellLeft = link;
					break;
				case LocationType.Right:
					cell.CellRight = link;
					break;
				case LocationType.BelowLeft:
					cell.CellBelowLeft = link;
					break;
				case LocationType.Below:
					cell.CellBelow = link;
					break;
				case LocationType.BelowRight:
					cell.CellBelowRight = link;
					break;
			}
		}

		private static void SetOutput()
		{
			string s = ConfigurationManager.AppSettings["outputType"];
			if (string.IsNullOrEmpty(s))
			{
				s = default(OutputType).ToString();
			}

			if (
				Enum.GetNames(typeof(OutputType))
					.Any(e => String.Equals(e.Trim(), s.Trim(), StringComparison.InvariantCultureIgnoreCase)))
			{
				_output = (OutputType)Enum.Parse(typeof(OutputType), s.Trim().ToUpperInvariant(), true);
			}
			else
			{
				_output = default(OutputType);
			}
		}

		#endregion

		/// <summary>
		/// Output an error message.
		/// </summary>
		private static void Error(string s)
		{
			ConsoleColor fg = Console.ForegroundColor;
			ConsoleColor bg = Console.BackgroundColor;
			Console.ForegroundColor = ConsoleColor.White;
			Console.BackgroundColor = ConsoleColor.Red;
			Console.WriteLine(s);
			Console.ForegroundColor = fg;
			Console.BackgroundColor = bg;
		}

		/// <summary>
		/// Output an error message.
		/// </summary>
		private static void Notice(string s)
		{
			ConsoleColor fg = Console.ForegroundColor;
			ConsoleColor bg = Console.BackgroundColor;
			Console.ForegroundColor = ConsoleColor.Green;
			Console.BackgroundColor = ConsoleColor.Black;
			Console.WriteLine(s);
			Console.ForegroundColor = fg;
			Console.BackgroundColor = bg;
		}

		private static void Important(string s)
		{
			ConsoleColor fg = Console.ForegroundColor;
			ConsoleColor bg = Console.BackgroundColor;
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.BackgroundColor = ConsoleColor.Black;
			Console.WriteLine(s);
			Console.ForegroundColor = fg;
			Console.BackgroundColor = bg;
		}

	}
}
