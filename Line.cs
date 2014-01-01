using System;
using System.Diagnostics;

namespace CodeCombatGridmancer
{
	/// <summary>
	/// Basic line built with two <see cref="Vertex"/>
	/// and has an intersection test from
	/// http://community.topcoder.com/tc?module=Static&d1=tutorials&d2=geometry2
	/// </summary>
	[DebuggerDisplay("V1: ({V1.X,nq}, {V1.Y,nq}) V2: ({V2.X,nq}, {V2.Y,nq})")]
	internal struct Line
	{
		public Vertex V1 { get; private set; }
		public Vertex V2 { get; private set; }

		public int A{get { return V2.Y - V1.Y; }}
		public int B{get { return V1.X - V2.X; }}
		public int C{get { return A * V1.X + B * V1.Y; }}

		public bool Vertical{get { return V1.X == V2.X; }}
		public bool Horizontal{get { return V1.Y == V2.Y; }}
		public bool Point{get { return Vertical && Horizontal; }}
		public bool Orthogonal{get { return Vertical ^ Horizontal; }}

		public int Length
		{
			get
			{
				int xd = V1.X - V2.X;
				int yd = V1.Y - V2.Y;
				return (int) Math.Sqrt(xd * xd + yd * yd);
			}
		}

		public Line(Vertex v1, Vertex v2) : this()
		{
			V1 = v1;
			V2 = v2;

			if (Point)
			{
				return;
			}

			if (!Orthogonal)
			{
				throw new ArgumentException("Must be an orthogonal line.");
			}

			int x1 = V1.X, x2 = V2.X, y1 = V1.Y, y2 = V2.Y;
			if (Vertical && y1 > y2)
			{
				V1 = new Vertex(x1, y2);
				V2 = new Vertex(x2, y1);
				return;
			}

			if (!Horizontal || x1 <= x2)
			{
				return;
			}
			V1 = new Vertex(x2, y1);
			V2 = new Vertex(x1, y2);
		}

		public bool Intersects(Line l2)
		{
			int determinant = A * l2.B - l2.A * B;

			if (determinant == 0)
			{
				return false;
			}

			int x1 = ( l2.B * C - B * l2.C ) / determinant;
			int y1 = ( A * l2.C - l2.A * C ) / determinant;

			Vertex test = new Vertex(x1, y1);
			return Contains(test) && l2.Contains(test);
		}

		/// <summary>
		/// Checks to see if this line contains a <see cref="Vertex"/>
		/// within it.  Since we're dealing with integer coordinate
		/// orthogonal lines, this is pretty easy.
		/// </summary>
		public bool Contains(Vertex v)
		{
			int x1 = V1.X, x2 = V2.X, y1 = V1.Y, y2 = V2.Y;
			int swap;
			if (x1 > x2)
			{
				swap = x1;
				x1 = x2;
				x2 = swap;
			}

			if (y1 <= y2)
			{
				return v.X >= x1 && v.X <= x2 && v.Y >= y1 && v.Y <= y2;
			}

			swap = y1;
			y1 = y2;
			y2 = swap;

			return v.X >= x1 && v.X <= x2 && v.Y >= y1 && v.Y <= y2;
		}

		/// <summary>
		/// Checks to see if a reference line would be cotained within this line.
		/// This is not an intersection test.  This is to see if line l is
		/// a segment within this line segment.
		/// </summary>
		public bool Contains(Line l)
		{
			int x1 = V1.X, x2 = V2.X, y1 = V1.Y, y2 = V2.Y;
			int lx1 = l.V1.X, lx2 = l.V2.X, ly1 = l.V1.Y, ly2 = l.V2.Y;

			if (Point)
			{
				return x1 == lx1 && x2 == lx2 && y1 == ly1 && y2 == ly2;
			}

			if (!Orthogonal || !l.Orthogonal)
			{
				return false;
			}

			if (Vertical ^ l.Vertical || Horizontal ^ l.Horizontal)
			{
				return false;
			}

			if (Vertical)
			{
				if (x1 != lx1 && x2 != lx2)
				{
					return false;
				}

				return y1 <= ly1 && y2 >= ly2;
			}

			if (y1 != ly1 && y2 != ly2)
			{
				return false;
			}
			return x1 <= lx1 && x2 >= lx2;
		}
	}
}