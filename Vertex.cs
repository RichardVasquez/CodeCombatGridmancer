using System.Diagnostics;

namespace CodeCombatGridmancer
{
	/// <summary>
	/// A simple struct to map out vertices needed for lines.
	/// </summary>
	[DebuggerDisplay("X:{X,nq} Y:{Y,nq}")]
	internal struct Vertex
	{
		public int X { get; set; }
		public int Y { get; set; }

		public Vertex(int x, int y)
			: this()
		{
			X = x;
			Y = y;
		}

		#region Boilerplate for equalities and such.
		public bool Equals(Vertex other)
		{
			return X == other.X && Y == other.Y;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ( X * 397 ) ^ Y;
			}
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}
			return obj is Vertex && Equals((Vertex) obj);
		}

		public static bool operator ==(Vertex v1, Vertex v2)
		{
			return v1.Equals(v2);
		}

		public static bool operator !=(Vertex v1, Vertex v2)
		{
			return !( v1 == v2 );
		}
		#endregion
	}
}