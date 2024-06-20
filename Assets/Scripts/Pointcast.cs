using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace CustomPhysics
{
	public struct MeshData
	{
		public Vector3 vert0;
		public Vector3 vert1;
		public Vector3 vert2;
		public Vector3 nextTo;
	}

	// set theTri return a bool value if off the mesh
	public struct PointCast
	{
		public MeshData[] meshData;
		public int theTri;
		public Vector3 casted;

		public PointCast(int startTri, MeshData[] meshData)
		{
			this.meshData = meshData;
			this.theTri = startTri;
			this.casted = Vector3.zero;
		}

		public void Step(Vector2 pointPos)
		{
			// So what I am doing is projecting, then ySnapping, then updating an  optional refPoint... i repeat this for three points
			Vector3 oppositePoint = CastFn.twoToThree(CastFn.projectToEdge(CastFn.threeToTwo(meshData[theTri].vert0), CastFn.threeToTwo(meshData[theTri].vert1), CastFn.threeToTwo(meshData[theTri].vert2)));
			Vector3 yOpposite = CastFn.ySnap(meshData[theTri].vert0, meshData[theTri].vert1, oppositePoint);

			Vector3 mirrorAxis0 = CastFn.twoToThree(CastFn.projectToEdge(CastFn.threeToTwo(meshData[theTri].vert0), CastFn.threeToTwo(meshData[theTri].vert1), pointPos));
			Vector3 yMirror0 = CastFn.ySnap(meshData[theTri].vert0, meshData[theTri].vert1, mirrorAxis0);

			Vector3 mirrorAxis1 = CastFn.twoToThree(CastFn.projectToEdge(CastFn.threeToTwo(oppositePoint), CastFn.threeToTwo(meshData[theTri].vert2), pointPos));
			Vector3 yMirror1 = CastFn.ySnap(yOpposite, meshData[theTri].vert2, mirrorAxis1);

			// Then I take those three points and use them to draw out an opposite point from the fourthPoint between the two adjustedPoints
			Vector3 mirrorPoint = Vector3.Lerp(yMirror0, yMirror1, 0.5f);

			Vector3 thePoint = Vector3.LerpUnclamped(yOpposite, mirrorPoint, 2);

			int edgeDetection = CastFn.edgeCheck(CastFn.threeToTwo(meshData[theTri].vert0), CastFn.threeToTwo(meshData[theTri].vert1), CastFn.threeToTwo(meshData[theTri].vert2), CastFn.threeToTwo(thePoint));
			float face = 0;

			switch (edgeDetection)
			{
				case 1:
					face = meshData[theTri].nextTo.x;
					break;
				case 2:
					face = meshData[theTri].nextTo.y;
					break;
				case 3:
					face = meshData[theTri].nextTo.z;
					break;
			}

			if (face > 0)
				theTri = Mathf.RoundToInt(face);

			casted = thePoint;
		}

		public static implicit operator Vector3(PointCast m)
		{
			return m.casted;
		}

	}

	struct CastFn
	{
		public static Vector3 ySnap(Vector3 v, Vector3 w, Vector3 p)
		{
			float lineLength = Vector2.Distance(threeToTwo(v), threeToTwo(w));
			float pointDist = Vector2.Distance(threeToTwo(v), threeToTwo(p));
			float factor = pointDist / lineLength;

			float lineLength2 = Vector2.Distance(threeToTwo(w), threeToTwo(v));
			float pointDist2 = Vector2.Distance(threeToTwo(w), threeToTwo(p));
			float factor2 = pointDist2 / lineLength2;

			if (factor > 1)
			{
				return p + (Vector3.up * (v.y - ((v.y - w.y) * factor)));
			}
			else
			{
				return p + (Vector3.up * (w.y - ((w.y - v.y) * factor2)));
			}
		}

		public static Vector2 projectToEdge(Vector2 v, Vector2 w, Vector2 p)
		{
			// Return the minimum distance point on line segment vw from point p
			float l2 = Mathf.Pow(Vector2.Distance(v, w), 2);
			if (l2 == 0.0) return p;
			float t = Vector2.Dot(p - v, w - v) / l2;
			// We clamp t from [0,1] to handle points outside the segment vw.
			// t = Mathf.Clamp01(t);
			Vector2 projection = v + t * (w - v);  // Projection falls on the segment
			return projection;
		}

		// Input face and point -> return with if on face or outside along what relative edge
		public static int edgeCheck(Vector2 v, Vector2 w, Vector2 f, Vector2 p)
		{
			Vector2 proj0 = projectToEdge(v, w, p);
			Vector2 proj1 = projectToEdge(w, f, p);
			Vector2 proj2 = projectToEdge(f, v, p);

			// else if? ~ when checking the return value start fallback to if on or face or not 0 or >
			// compare distances to find which is more true?
			int edge = 0;

			if (outsideEdge(p, proj0, f))
			{
				edge = 1;
			}
			else if (outsideEdge(p, proj1, v))
			{
				edge = 2;
			}
			else if (outsideEdge(p, proj2, w))
			{
				edge = 3;
			}

			return edge;
		}

		public static bool outsideEdge(Vector2 p, Vector2 edge, Vector2 vertex)
		{
			bool outEdge = false;

			if (Vector2.Distance(p, vertex) > Vector2.Distance(edge, vertex) + 0.01f)
				outEdge = true;

			return outEdge;
		}

		public static Vector2 threeToTwo(Vector3 v3)
		{
			return new Vector2(v3.x, v3.z);
		}

		public static Vector3 twoToThree(Vector2 v2)
		{
			return new Vector3(v2.x, 0, v2.y);
		}
	}
}
