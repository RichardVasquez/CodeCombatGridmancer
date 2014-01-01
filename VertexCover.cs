using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CodeCombatGridmancer
{
	/// <summary>
	/// Base C++ code from http://www.dharwadker.org/vertex_cover/
	/// </summary>
	class VertexCover
	{
		private readonly List<List<int>> _graph = new List<List<int>>();
		private List<List<int>> _neighbors = new List<List<int>>();

		public int Size { get; private set; }
		private const int MaxSize = 20;

		public VertexCover(int size)
		{
			if (size <= 0)
			{
				throw new ArgumentException("No empty or negative sizes.");
			}
			if (size > MaxSize)
			{
				throw new ArgumentException(string.Format( "No graphs larger than {0}",MaxSize));
			}

			for (int i = 0; i < size; i++)
			{
				_graph.Add(new List<int>());
				for (int k = 0; k < size; k++)
				{
					_graph[i].Add(0);
				}
			}
			Size = size;
		}

		/// <summary>
		/// Adds a node connection.  Nodes are 1 based for indexing purposes.
		/// </summary>
		public void Add(int u, int v)
		{
			if (u < 1 || u > Size)
			{
				throw new ArgumentException(
					string.Format("Outside of the range 1-{0}", Size),
					"u");
			}
			if (v < 1 || v > Size)
			{
				throw new ArgumentException(
					string.Format("Outside of the range 1-{0}", Size),
					"v");
			}

			u = u - 1;
			v = v - 1;

			_graph[ u ][ v ] = 1;
			_graph[ v ][ u ] = 1;
		}

		public List<VertexCoverData> FindCover(int k)
		{
			FindNeighbors();
			List<VertexCoverData> res = new List<VertexCoverData>();
			int min = Size + 1;
			List<List<int>> covers = new List<List<int>>();
			List<int> allCover = new List<int>();
			for (int i = 0; i < Size; i++)
			{
				allCover.Add(1);
			}

			bool found = FindInitial(k, allCover, false, res, covers, ref min);
			FindPairs(k, covers, found, allCover, min, res);

			//	This code will duplicate its results, so we strip out the duplicates.
			HashSet<BigInteger> hsbi = new HashSet<BigInteger>();
			foreach (var data in res)
			{
				hsbi.Add(data.BigHash);
			}

			return hsbi
				.Select(bigInteger => res.FirstOrDefault(r => r.BigHash == bigInteger))
				.Where(set => set != null)
				.ToList();
		}

		private bool FindInitial(int k, List<int> allCover, bool found, ICollection<VertexCoverData> res, ICollection<List<int>> covers, ref int min)
		{
			for (int i = 0; i < allCover.Count; i++)
			{
				if (found)
				{
					break;
				}

				List<int> cover = allCover.ToList();
				cover[ i ] = 0;
				cover = Procedure1(_neighbors, cover);
				int s = CoverSize(cover);
				if (s < min)
				{
					min = s;
				}
				if (s <= k)
				{
					res.Add(MakeData(cover));
					covers.Add(cover);
					found = true;
					break;
				}

				for (int j = 0; j < Size - k; j++)
				{
					cover = Procedure2(_neighbors, cover, j);
				}
				s = CoverSize(cover);
				if (s < min)
				{
					min = s;
				}

				res.Add(MakeData(cover));
				covers.Add(cover);
				if (s > k)
				{
					continue;
				}
				found = true;
				break;
			}
			return found;
		}

		private void FindPairs(int k, IList<List<int>> covers, bool found, List<int> allCover, int min, List<VertexCoverData> res)
		{
			for (int p = 0; p < covers.Count; p++)
			{
				if (found)
				{
					break;
				}

				for (int q = p + 1; q < covers.Count; q++)
				{
					List<int> cover = allCover.ToList();
					for (int r = 0; r < cover.Count; r++)
					{
						if (covers[p][r] == 0 && covers[q][r] == 0)
						{
							cover[r] = 0;
						}
					}

					cover = Procedure1(_neighbors, cover);
					int s = CoverSize(cover);
					if (s < min)
					{
						min = s;
					}
					if (s <= k)
					{
						res.Add(MakeData(cover));
						covers.Add(cover);
						found = true;
						break;
					}
					for (int j = 0; j < k; j++)
					{
						cover = Procedure2(_neighbors, cover, j);
					}
					s = CoverSize(cover);
					if (s < min)
					{
						min = s;
					}
					res.Add(MakeData(cover));
					if (s > k)
					{
						continue;
					}
					found = true;
					break;
				}
			}
		}

		private static VertexCoverData MakeData(List<int> cover)
		{
			var tmp = new List<int>();
			for (int j = 0; j < cover.Count; j++)
			{
				if (cover[j] == 1)
				{
					tmp.Add(j + 1);
				}
			}
			return new VertexCoverData(tmp.Count, tmp);
		}

		private void FindNeighbors()
		{
			_neighbors=new List<List<int>>();
			for (int i = 0; i < Size; i++)
			{
				_neighbors.Add(new List<int>());
				for (int k = 0; k < Size; k++)
				{
					if (_graph[ i ][ k ] == 1)
					{
						_neighbors[i].Add(k);
					}
				}
			}
		}

		private static bool IsRemovable(IEnumerable<int> neighbor, List<int> cover)
		{
			if (neighbor == null)
			{
				throw new ArgumentNullException("neighbor");
			}
			return neighbor.All(t => cover[ t ] != 0);
		}

		private static int MaxRemovable(List<List<int>> neighbors, List<int> cover)
		{
			int r = -1, max = -1;

			for (int i = 0; i < cover.Count; i++)
			{
				if (cover[ i ] != 1 || !IsRemovable(neighbors[ i ], cover))
				{
					continue;
				}
				List<int> tempCover = cover.ToList();
				tempCover[ 1 ] = 0;
				int sum = 0;
				for (int j = 0; j < tempCover.Count; j++)
				{
					if (tempCover[ j ] == 1 && IsRemovable(neighbors[ j ], tempCover))
					{
						sum++;
					}
					if (sum > max)
					{
						if (r != -1)
						{
							continue;
						}
						max = sum;
						r = 1;
					}
					else
					{
						if (neighbors[ r ].Count < neighbors[ i ].Count)
						{
							continue;
						}
						max = sum;
						r = i;
					}
				}
			}
			return r;
		}

		private static List<int> Procedure1(List<List<int>> neighbors, IEnumerable<int> cover)
		{
			List<int> tempCover = cover.ToList();
			int r = 0;
			while (r != -1)
			{
				r = MaxRemovable(neighbors, tempCover);
				if (r != -1)
				{
					tempCover[ r ] = 0;
				}
			}
			return tempCover;
		}

		private static List<int> Procedure2(List<List<int>> neighbors, List<int> cover, int k)
		{
			int count = 0;
			List<int> tempCover = cover.ToList();
			for (int i = 0; i < tempCover.Count; i++)
			{
				if (tempCover[ i ] != 1)
				{
					continue;
				}
				int sum = 0, index = 0;
				for (int j = 0; j < neighbors[ i ].Count; j++)
				{
					if (tempCover[ neighbors[ i ][ j ] ] != 0)
					{
						continue;
					}
					index = j;
					sum++;
				}
				if (sum == 1 && cover[ neighbors[ i ][ index ] ] == 0)
				{
					tempCover[ neighbors[ i ][ index ] ] = 1;
					tempCover[ i ] = 0;
					tempCover = Procedure1(neighbors, tempCover);
					count++;
				}
				if (count > k)
				{
					break;
				}
			}
			return tempCover;
		}

		private static int CoverSize(IEnumerable<int> cover)
		{
			return cover.Count(t => t == 1);
		}
	}
}
