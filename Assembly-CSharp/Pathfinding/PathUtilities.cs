using Pathfinding.Util;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
	public static class PathUtilities
	{
		private static Queue<GraphNode> BFSQueue;

		private static Dictionary<GraphNode, int> BFSMap;

		public static bool IsPathPossible(GraphNode n1, GraphNode n2)
		{
			return n1.Walkable && n2.Walkable && n1.Area == n2.Area;
		}

		public static bool IsPathPossible(List<GraphNode> nodes)
		{
			if (nodes.Count == 0)
			{
				return true;
			}
			uint area = nodes[0].Area;
			for (int i = 0; i < nodes.Count; i++)
			{
				if (!nodes[i].Walkable || nodes[i].Area != area)
				{
					return false;
				}
			}
			return true;
		}

		public static bool IsPathPossible(List<GraphNode> nodes, int tagMask)
		{
			if (nodes.Count == 0)
			{
				return true;
			}
			if (((tagMask >> (int)nodes[0].Tag) & 1) == 0)
			{
				return false;
			}
			if (!IsPathPossible(nodes))
			{
				return false;
			}
			List<GraphNode> reachableNodes = GetReachableNodes(nodes[0], tagMask);
			bool result = true;
			for (int i = 1; i < nodes.Count; i++)
			{
				if (!reachableNodes.Contains(nodes[i]))
				{
					result = false;
					break;
				}
			}
			ListPool<GraphNode>.Release(reachableNodes);
			return result;
		}

		public static List<GraphNode> GetReachableNodes(GraphNode seed, int tagMask = -1)
		{
			Stack<GraphNode> stack = StackPool<GraphNode>.Claim();
			List<GraphNode> list = ListPool<GraphNode>.Claim();
			HashSet<GraphNode> map = new HashSet<GraphNode>();
			GraphNodeDelegate graphNodeDelegate = (tagMask != -1) ? ((GraphNodeDelegate)delegate(GraphNode node)
			{
				if (node.Walkable && ((tagMask >> (int)node.Tag) & 1) != 0 && map.Add(node))
				{
					list.Add(node);
					stack.Push(node);
				}
			}) : ((GraphNodeDelegate)delegate(GraphNode node)
			{
				if (node.Walkable && map.Add(node))
				{
					list.Add(node);
					stack.Push(node);
				}
			});
			graphNodeDelegate(seed);
			while (stack.Count > 0)
			{
				stack.Pop().GetConnections(graphNodeDelegate);
			}
			StackPool<GraphNode>.Release(stack);
			return list;
		}

		public static List<GraphNode> BFS(GraphNode seed, int depth, int tagMask = -1)
		{
			BFSQueue = (BFSQueue ?? new Queue<GraphNode>());
			Queue<GraphNode> que = BFSQueue;
			BFSMap = (BFSMap ?? new Dictionary<GraphNode, int>());
			Dictionary<GraphNode, int> map = BFSMap;
			que.Clear();
			map.Clear();
			List<GraphNode> result = ListPool<GraphNode>.Claim();
			int currentDist = -1;
			GraphNodeDelegate graphNodeDelegate = (tagMask != -1) ? ((GraphNodeDelegate)delegate(GraphNode node)
			{
				if (node.Walkable && ((tagMask >> (int)node.Tag) & 1) != 0 && !map.ContainsKey(node))
				{
					map.Add(node, currentDist + 1);
					result.Add(node);
					que.Enqueue(node);
				}
			}) : ((GraphNodeDelegate)delegate(GraphNode node)
			{
				if (node.Walkable && !map.ContainsKey(node))
				{
					map.Add(node, currentDist + 1);
					result.Add(node);
					que.Enqueue(node);
				}
			});
			graphNodeDelegate(seed);
			while (que.Count > 0)
			{
				GraphNode graphNode = que.Dequeue();
				currentDist = map[graphNode];
				if (currentDist >= depth)
				{
					break;
				}
				graphNode.GetConnections(graphNodeDelegate);
			}
			que.Clear();
			map.Clear();
			return result;
		}

		public static List<Vector3> GetSpiralPoints(int count, float clearance)
		{
			List<Vector3> list = ListPool<Vector3>.Claim(count);
			float num = clearance / (MathF.PI * 2f);
			float num2 = 0f;
			list.Add(InvoluteOfCircle(num, num2));
			for (int i = 0; i < count; i++)
			{
				Vector3 b = list[list.Count - 1];
				float num3 = (0f - num2) / 2f + Mathf.Sqrt(num2 * num2 / 4f + 2f * clearance / num);
				float num4 = num2 + num3;
				float num5 = num2 + 2f * num3;
				while (num5 - num4 > 0.01f)
				{
					float num6 = (num4 + num5) / 2f;
					Vector3 a = InvoluteOfCircle(num, num6);
					if ((a - b).sqrMagnitude < clearance * clearance)
					{
						num4 = num6;
					}
					else
					{
						num5 = num6;
					}
				}
				list.Add(InvoluteOfCircle(num, num5));
				num2 = num5;
			}
			return list;
		}

		private static Vector3 InvoluteOfCircle(float a, float t)
		{
			return new Vector3(a * (Mathf.Cos(t) + t * Mathf.Sin(t)), 0f, a * (Mathf.Sin(t) - t * Mathf.Cos(t)));
		}

		public static void GetPointsAroundPointWorld(Vector3 p, IRaycastableGraph g, List<Vector3> previousPoints, float radius, float clearanceRadius)
		{
			if (previousPoints.Count != 0)
			{
				Vector3 zero = Vector3.zero;
				for (int i = 0; i < previousPoints.Count; i++)
				{
					zero += previousPoints[i];
				}
				zero /= (float)previousPoints.Count;
				for (int j = 0; j < previousPoints.Count; j++)
				{
					List<Vector3> list;
					List<Vector3> list2 = list = previousPoints;
					int index;
					int index2 = index = j;
					Vector3 a = list[index];
					list2[index2] = a - zero;
				}
				GetPointsAroundPoint(p, g, previousPoints, radius, clearanceRadius);
			}
		}

		public static void GetPointsAroundPoint(Vector3 p, IRaycastableGraph g, List<Vector3> previousPoints, float radius, float clearanceRadius)
		{
			if (g == null)
			{
				throw new ArgumentNullException("g");
			}
			NavGraph navGraph = g as NavGraph;
			if (navGraph == null)
			{
				throw new ArgumentException("g is not a NavGraph");
			}
			NNInfoInternal nearestForce = navGraph.GetNearestForce(p, NNConstraint.Default);
			p = nearestForce.clampedPosition;
			if (nearestForce.node == null)
			{
				return;
			}
			radius = Mathf.Max(radius, 1.4142f * clearanceRadius * Mathf.Sqrt(previousPoints.Count));
			clearanceRadius *= clearanceRadius;
			for (int i = 0; i < previousPoints.Count; i++)
			{
				Vector3 b = previousPoints[i];
				float magnitude = b.magnitude;
				if (magnitude > 0f)
				{
					b /= magnitude;
				}
				float num = radius;
				b *= num;
				bool flag = false;
				int num2 = 0;
				do
				{
					Vector3 vector = p + b;
					if (g.Linecast(p, vector, nearestForce.node, out GraphHitInfo hit))
					{
						vector = hit.point;
					}
					for (float num3 = 0.1f; num3 <= 1f; num3 += 0.05f)
					{
						Vector3 vector2 = (vector - p) * num3 + p;
						flag = true;
						for (int j = 0; j < i; j++)
						{
							if ((previousPoints[j] - vector2).sqrMagnitude < clearanceRadius)
							{
								flag = false;
								break;
							}
						}
						if (flag)
						{
							previousPoints[i] = vector2;
							break;
						}
					}
					if (!flag)
					{
						if (num2 > 8)
						{
							flag = true;
							continue;
						}
						clearanceRadius *= 0.9f;
						b = UnityEngine.Random.onUnitSphere * Mathf.Lerp(num, radius, num2 / 5);
						b.y = 0f;
						num2++;
					}
				}
				while (!flag);
			}
		}

		public static List<Vector3> GetPointsOnNodes(List<GraphNode> nodes, int count, float clearanceRadius = 0f)
		{
			if (nodes == null)
			{
				throw new ArgumentNullException("nodes");
			}
			if (nodes.Count == 0)
			{
				throw new ArgumentException("no nodes passed");
			}
			List<Vector3> list = ListPool<Vector3>.Claim(count);
			clearanceRadius *= clearanceRadius;
			if (clearanceRadius > 0f || nodes[0] is TriangleMeshNode || nodes[0] is GridNode)
			{
				List<float> list2 = ListPool<float>.Claim(nodes.Count);
				float num = 0f;
				for (int i = 0; i < nodes.Count; i++)
				{
					float num2 = nodes[i].SurfaceArea();
					num2 += 0.001f;
					num += num2;
					list2.Add(num);
				}
				for (int j = 0; j < count; j++)
				{
					int num3 = 0;
					int num4 = 10;
					bool flag = false;
					while (!flag)
					{
						flag = true;
						if (num3 >= num4)
						{
							clearanceRadius *= 0.809999943f;
							num4 += 10;
							if (num4 > 100)
							{
								clearanceRadius = 0f;
							}
						}
						float item = UnityEngine.Random.value * num;
						int num5 = list2.BinarySearch(item);
						if (num5 < 0)
						{
							num5 = ~num5;
						}
						if (num5 >= nodes.Count)
						{
							flag = false;
							continue;
						}
						GraphNode graphNode = nodes[num5];
						Vector3 vector = graphNode.RandomPointOnSurface();
						if (clearanceRadius > 0f)
						{
							for (int k = 0; k < list.Count; k++)
							{
								if ((list[k] - vector).sqrMagnitude < clearanceRadius)
								{
									flag = false;
									break;
								}
							}
						}
						if (flag)
						{
							list.Add(vector);
							break;
						}
						num3++;
					}
				}
				ListPool<float>.Release(list2);
			}
			else
			{
				for (int l = 0; l < count; l++)
				{
					list.Add(nodes[UnityEngine.Random.Range(0, nodes.Count)].RandomPointOnSurface());
				}
			}
			return list;
		}
	}
}
