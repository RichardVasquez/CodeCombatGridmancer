using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CodeCombatGridmancer
{
	/// <summary>
	/// The cell used to store the map.
	/// </summary>
	[DebuggerDisplay("X:{X,nq} Y:{Y,nq} Occupied:{Occupied,nq}")]
	internal class Cell
	{
		public int X { get; set; }
		public int Y { get; set; }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Cell _cellAbove;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Cell _cellBelow;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Cell _cellLeft;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Cell _cellRight;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Cell _cellAboveLeft;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Cell _cellAboveRight;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Cell _cellBelowLeft;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Cell _cellBelowRight;
	
		public Cell CellAbove
		{
			get
			{
				if (WallTop || _cellAbove == null)
				{
					return null;
				}
				return _cellAbove.WallBottom ? null : _cellAbove;
			}
			set
			{
				if (value == null)
				{
					_cellAbove = null;
					return;
				}
				if (!value.Occupied)
				{
					_cellAbove = null;
					return;
				}

				_cellAbove = value;
			}
		}

		public Cell CellBelow {
			get
			{
				if (WallBottom ||_cellBelow == null)
				{
					return null;
				}
				return _cellBelow.WallTop ? null : _cellBelow;
			}
			set
			{
				if (value == null)
				{
					_cellBelow = null;
					return;
				}
				if (!value.Occupied)
				{
					_cellBelow = null;
					return;
				}
				_cellBelow = value;
			}
		}
		public Cell CellLeft
		{
			get
			{
				if (WallLeft || _cellLeft== null)
				{
					return null;
				}
				return _cellLeft.WallRight ? null : _cellLeft;
			}
			set
			{
				if (value == null)
				{
					_cellLeft = null;
					return;
				}
				if (!value.Occupied)
				{
					_cellLeft = null;
					return;
				}
				_cellLeft = value;
			}
		}
		public Cell CellRight
		{
			get
			{
				if (WallRight || _cellRight == null)
				{
					return null;
				}
				return _cellRight.WallLeft ? null : _cellRight;
			}
			set
			{
				if (value == null)
				{
					_cellRight = null;
					return;
				}
				if (!value.Occupied)
				{
					_cellRight = null;
					return;
				}
				_cellRight = value;
			}
		}
		public Cell CellAboveLeft
		{
			get
			{
				if (WallLeft || WallTop || _cellAboveLeft == null)
				{
					return null;
				}
				if (_cellAboveLeft.WallRight || _cellAboveLeft.WallBottom)
				{
					return null;
				}
				return _cellAboveLeft;
			}
			set
			{
				if (value == null)
				{
					_cellAboveLeft = null;
					return;
				}
				if (!value.Occupied)
				{
					_cellAboveLeft = null;
					return;
				}
				_cellAboveLeft = value;
			}
		}
		public Cell CellAboveRight
		{
			get
			{
				if (WallRight || WallTop || _cellAboveRight == null)
				{
					return null;
				}
				if (_cellAboveRight.WallLeft || _cellAboveRight.WallBottom)
				{
					return null;
				}
				return _cellAboveRight;
			}
			set
			{
				if (value == null)
				{
					_cellAboveRight = null;
					return;
				}
				if (!value.Occupied)
				{
					_cellAboveRight = null;
					return;
				}
				_cellAboveRight = value;
			}
		}
		public Cell CellBelowLeft
		{
			get
			{
				if (WallLeft || WallBottom || _cellBelowLeft == null)
				{
					return null;
				}
				if (_cellBelowLeft.WallRight || _cellBelowLeft.WallTop)
				{
					return null;
				}
				return _cellBelowLeft;
			}
			set
			{
				if (value == null)
				{
					_cellBelowLeft = null;
					return;
				}
				if (!value.Occupied)
				{
					_cellBelowLeft = null;
					return;
				}
				_cellBelowLeft = value;
			}
		}
		public Cell CellBelowRight
		{
			get
			{
				if (WallRight || WallBottom || _cellBelowRight == null)
				{
					return null;
				}
				if (_cellBelowRight.WallLeft|| _cellBelowRight.WallTop)
				{
					return null;
				}
				return _cellBelowRight;
			}
			set
			{
				if (value == null)
				{
					_cellBelowRight = null;
					return;
				}
				if (!value.Occupied)
				{
					_cellBelowRight = null;
					return;
				}
				_cellBelowRight = value;
			}
		}

		public bool WallLeft { get; set; }
		public bool WallRight { get; set; }
		public bool WallTop { get; set; }
		public bool WallBottom { get; set; }

		public bool Occupied { get; set; }

		public List<Cell> Neighbors
		{
			get
			{
				List<Cell> res = new List<Cell>
				{
					CellAbove,
					CellBelow,
					CellLeft,
					CellRight,
					CellAboveLeft,
					CellAboveRight,
					CellBelowLeft,
					CellBelowRight
				};

				res.RemoveAll(c => c == null);

				if (WallBottom)
				{
					if (res.Contains(CellBelow))
					{
						res.Remove(CellBelow);
					}
				}

				if (WallTop)
				{
					if (res.Contains(CellAbove))
					{
						res.Remove(CellAbove);
					}
				}

				if (WallLeft)
				{
					if (res.Contains(CellLeft))
					{
						res.Remove(CellLeft);
					}
				}

				if (WallRight)
				{
					if (res.Contains(CellRight))
					{
						res.Remove(CellRight);
					}
				}

				if (WallTop || WallLeft)
				{
					if (res.Contains(CellAboveLeft))
					{
						res.Remove(CellAboveLeft);
					}
				}

				if (WallTop || WallRight)
				{
					if (res.Contains(CellAboveRight))
					{
						res.Remove(CellAboveRight);
					}
				}

				if (WallBottom || WallLeft)
				{
					if (res.Contains(CellBelowLeft))
					{
						res.Remove(CellBelowLeft);
					}
				}

				if (WallTop || WallLeft)
				{
					if (res.Contains(CellAboveLeft))
					{
						res.Remove(CellAboveLeft);
					}
				}


				return res;
			}
		}

		public bool CornerAboveLeftConcave
		{
			get
			{
				return !WallTop && !WallLeft && CellAbove != null && CellLeft != null && CellAboveLeft == null;
			}
		}

		public bool CornerAboveRightConcave {
			get
			{
				return !WallTop && !WallRight && CellAbove != null && CellRight != null && CellAboveRight == null;
			}
		}

		public bool CornerBelowLeftConcave {
			get
			{
				return !WallBottom && !WallLeft && CellBelow != null && CellLeft != null && CellBelowLeft == null;
			}
		}

		public bool CornerBelowRightConcave {
			get
			{
				return !WallBottom && !WallRight && CellBelow != null && CellRight != null && CellBelowRight == null;
			}
		}

		public int AreaIndex { get; set; }

		public List<Vertex> ConcaveVertices
		{
			get
			{
				HashSet<Vertex> hold = new HashSet<Vertex>();
				Vertex topLeft = new Vertex(X, Y + 1);
				Vertex bottomLeft = new Vertex(X, Y);
				Vertex bottomRight = new Vertex(X + 1, Y);
				Vertex topRight = new Vertex(X + 1, Y + 1);

				if (CornerAboveLeftConcave)
				{
					hold.Add(topLeft);
				}
				if (CornerAboveRightConcave)
				{
					hold.Add(topRight);
				}
				if (CornerBelowLeftConcave)
				{
					hold.Add(bottomLeft);
				}
				if (CornerBelowRightConcave)
				{
					hold.Add(bottomRight);
				}
				return hold.ToList().OrderBy(v => v.X).ThenBy(v => v.Y).ToList();
			}
		}

		public List<Vertex> AllVertices
		{
			get
			{
				List<Vertex> res = new List<Vertex>();
				HashSet<Vertex> hold = new HashSet<Vertex>();
				Vertex topLeft = new Vertex(X, Y + 1);
				Vertex bottomLeft = new Vertex(X, Y);
				Vertex bottomRight = new Vertex(X + 1, Y);
				Vertex topRight = new Vertex(X + 1, Y + 1);

				if (CellLeft == null)
				{
					hold.Add(topLeft);
					hold.Add(bottomLeft);
				}

				if (CellBelow == null)
				{
					hold.Add(bottomLeft);
					hold.Add(bottomRight);
				}

				if (CellRight == null)
				{
					hold.Add(topRight);
					hold.Add(bottomRight);
				}

				if (CellAbove == null)
				{
					hold.Add(topLeft);
					hold.Add(topRight);
				}

				bool sameAboveLeft = CellAboveLeft != null && CellAboveLeft.Occupied == Occupied;
				bool sameAbove = CellAbove != null && CellAbove.Occupied == Occupied;
				bool sameAboveRight = CellAboveRight != null && CellAboveRight.Occupied == Occupied;
				bool sameLeft = CellLeft != null && CellLeft.Occupied == Occupied;
				bool sameRight = CellRight != null && CellRight.Occupied == Occupied;
				bool sameBelowLeft = CellBelowLeft != null && CellBelowLeft.Occupied == Occupied;
				bool sameBelow = CellBelow != null && CellBelow.Occupied == Occupied;
				bool sameBelowRight = CellBelowRight != null && CellBelowRight.Occupied == Occupied;

				if (!( sameAboveLeft && sameAbove && sameLeft ))
				{
					hold.Add(topLeft);
				}

				if (!( sameAboveRight && sameAbove && sameRight ))
				{
					hold.Add(topRight);
				}

				if (!(sameBelowLeft && sameBelow && sameLeft))
				{
					hold.Add(bottomLeft);
				}

				if (!(sameBelowRight && sameBelow && sameRight))
				{
					hold.Add(bottomRight);
				}

				return hold.ToList();
			}
		}
	}
}