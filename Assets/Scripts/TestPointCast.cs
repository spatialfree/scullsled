using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using CustomPhysics;

[RequireComponent(typeof(MeshFilter))]
public class TestPointCast: MonoBehaviour
{
	public MeshData[] meshData;
	PointCast firstCast;
	public int theTri;
	public Vector2 pointPos;
	public int refCount;
	public GameObject refPoint;
	public GameObject pointCastRef;
	public GameObject camAnchor;
	public GameObject pointMover;
	public Transform pointOffset;
	public Buttons buttons;

	private bool testCast;
	private bool movePoint;
	private bool moveCamera;
	private GameObject[] refPoints;
	private float randDelay;
	private Vector3 randPos;

	Mesh mesh;
	private Vector3[] genVerts;
	private int[] triangles;

	void PlacePoint()
	{
		testCast = !testCast;
	}

	void MovePoint()
	{
		movePoint = !movePoint;
	}

	void MoveCamera()
	{
		moveCamera = !moveCamera;
	}

	void Awake()
	{
		genVerts = new Vector3[6];
		meshData = new MeshData[100];
		theTri = 1;
		mesh = GetComponent<MeshFilter>().mesh;
		testCast = false;
		firstCast = new PointCast(theTri, meshData);
	}

	void Start()
	{
		refPoints = new GameObject[refCount];

		for (int i = 0; i < refCount; i++)
		{
			refPoints[i] = GameObject.Instantiate(refPoint, transform);
		}
	}

	// Test Mesh Gen
	void MeshGen()
	{
		Random.InitState(Mathf.CeilToInt(Time.time));

		// gen vertices
		genVerts[0] = new Vector3((Random.value - 0.5f) * 0.1f, Random.value * 0.5f, randomValue(0.5f));
		genVerts[1] = new Vector3(randomValue(0.5f), Random.value * 0.5f, (Random.value - 0.5f) * 0.1f);
		genVerts[2] = new Vector3(0, Random.value * 0.5f, (Random.value - 0.5f) * 0.1f);
		genVerts[3] = new Vector3(-randomValue(0.5f), Random.value * 0.5f, (Random.value - 0.5f) * 0.1f);
		genVerts[4] = new Vector3((Random.value - 0.5f) * 0.1f, Random.value * 0.5f, -randomValue(0.5f));

		triangles = new int[] { 0, 1, 2, 0, 2, 3, 2, 1, 4, 2, 4, 3 };

		mesh.Clear();
		mesh.vertices = genVerts;
		mesh.triangles = triangles;

		// input meshData (this is the manual version of that, automated to come later)
		meshData[1].vert0 = genVerts[0];
		meshData[1].vert1 = genVerts[1];
		meshData[1].vert2 = genVerts[2];
		meshData[1].nextTo = new Vector3(0, 3, 2);

		meshData[2].vert0 = genVerts[0];
		meshData[2].vert1 = genVerts[2];
		meshData[2].vert2 = genVerts[3];
		meshData[2].nextTo = new Vector3(1, 4, 0);

		meshData[3].vert0 = genVerts[2];
		meshData[3].vert1 = genVerts[1];
		meshData[3].vert2 = genVerts[4];
		meshData[3].nextTo = new Vector3(1, 0, 4);

		meshData[4].vert0 = genVerts[2];
		meshData[4].vert1 = genVerts[4];
		meshData[4].vert2 = genVerts[3];
		meshData[4].nextTo = new Vector3(3, 0, 2);
	}

	float randomValue(float min)
	{
		float randomValue = Mathf.Clamp(Random.value, min, 1);
		return randomValue;
	}

	void Update()
	{
		pointCastRef.SetActive(testCast);

		// prime the stopwatch
		Stopwatch watch = new Stopwatch();
		watch.Start();

		if (testCast)
		{
			firstCast.Step(pointPos);
			Vector3 castedPoint = firstCast;
			pointCastRef.transform.position = castedPoint;

			// Implement Edge case
			if (testCast)
			{
				ReferencePoint(1, castedPoint);
			}
			else
			{
				refPoints[1].SetActive(false);
			}
		}

		watch.Stop();
		buttons.castFeedback = "DONE in " + watch.Elapsed.TotalSeconds.ToString("F6") + " seconds";

		// Cam + point cast rotation
		if (moveCamera)
			camAnchor.transform.rotation *= Quaternion.Euler(Vector3.up * Time.deltaTime * buttons.cameraSpeed);

		if (movePoint)
		{
			//pointMover.transform.rotation *= Quaternion.Euler(Vector3.up * Time.deltaTime * -buttons.pointSpeed);

			if (Time.time > randDelay)
			{
				randPos = new Vector3(Random.value - 0.5f, 0, Random.value - 0.5f);
				randDelay = Time.time + (Random.value * 1.5f);
			}

			pointOffset.localPosition = Vector3.Lerp(pointOffset.localPosition, randPos * 1f, 0.5f * Time.deltaTime);
			pointPos = CastFn.threeToTwo(pointMover.transform.GetChild(0).transform.position);
		}
	}

	void ReferencePoint (int id, Vector3 position)
	{
		refPoints[id].SetActive(true);
		refPoints[id].transform.position = position;
	}
}