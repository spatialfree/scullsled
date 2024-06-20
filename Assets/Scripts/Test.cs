using UnityEngine;
using CustomPhysics;

[CreateAssetMenu]
public class Test : ScriptableObject
{
	[Header("Seed")]
	public int seed;
	public bool newSeed;

	[Header("Core")]
	public int minlength;
	public float stepDistance;
	public float scale;
	public Vector3[] centerVertices;
	public Quaternion[] verticeRotation;

	[Header("RNG | Vectors X[rate] Y[min] Z[max]")]
	public Vector3 vertical;
	public Vector3 horizontal;
	public Vector3 camber;
	public Vector3 sideCamber;
	public Vector3 width;

	[Header("Berms")]
	public float bermWidth;
	public float bermHeight;
	public float crestOffset;

	[Header("Flora")]
	public float floraOffset;
	public float floraEmitRate;

	[Header("Dont Touch")]
	public bool cleanScene;
	public MeshData[] slopeData;
}
