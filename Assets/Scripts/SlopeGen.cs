using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using CustomPhysics;

//[ExecuteInEditMode]
public class SlopeGen : MonoBehaviour
{
	[TestButton("Generate slope", "StartGen", isActiveInEditor = false)]
	[TestButton("Generate Meshes", "GenMesh", isActiveInEditor = false)]
	[TestButton("Clear", "ClearScene", 2, isActiveInEditor = false)]
	[ProgressBar(hideWhenZero = true, label = "procGenFeedback")]
	public float procgenProgress = -1;

	[HideInInspector]
	public string procGenFeedback;

	[Header("References")]
	public Test test;
	public GameObject debugRefObject;
	public Transform previewCam;
	public MeshFilter slope;
	public MeshFilter lBerm;
	public MeshFilter rBerm;
	public ParticleSystem flora;

	private int length;
	private Vector3[] slopeVerts;	// Slope gen var
	private int slopeVertNum;
	private int[] slopeTris;
	private int slopeStep;

	private Vector3[] undersideVerts;
	private int undersideVertNum;
	private int[] undersideTris;
	private int undersideStep;

	private Vector3[] lBermVerts;
	private int lBermVertNum;
	private int[] lBermTris;
	private int lBermStep;

	private Vector3[] rBermVerts;
	private int rBermVertNum;
	private int[] rBermTris;
	private int rBermStep;

	private Vector3 coreRot;
	private Vector3 corePos;
	private float xVertical;
	private float yHorizontal;
	private float zCoreCamber;
	private float lCamber;	private float rCamber;
	private float lWidth;	private float rWidth;

	private float lFloraEmit;	private float rFloraEmit;

	private int next;	// Preview system var
	private float transition;

	private int slopeTri;

	void Start()
	{
		Selection.activeGameObject = this.gameObject;
		test.cleanScene = true;
	}

	void Update()
	{
		if (!test.cleanScene)
		{
			PreviewSlope();
		}
	}

	void ClearScene()
	{
		//EditorSceneManager.OpenScene("Assets/Scenes/Tech Demo.unity");
		SceneManager.LoadScene(0);
		procgenProgress = 0;
		test.cleanScene = true;
	}

	void StartGen()
	{
		// PREP -----------------------------------------------------------------------------------
		// prime the stopwatch
		Stopwatch watch = new Stopwatch();
		watch.Start();

		// Set the seed
		if (test.newSeed == true)
			test.seed = System.Environment.TickCount;

		Random.InitState(test.seed);

		// Determine array sizes
		length = Random.Range(test.minlength, test.minlength * 2);
		slopeVerts = new Vector3[length * 3];	slopeTris = new int[length * 12];
		lBermVerts = new Vector3[length * 3];	lBermTris = new int[length * 12];
		rBermVerts = new Vector3[length * 3]; rBermTris = new int[length * 12];

		test.slopeData = new MeshData[length * 12];
		test.centerVertices = new Vector3[length];
		test.verticeRotation = new Quaternion[length];

		for (int i = 0; i < length; i++)
		{
			// CORE -------------------------------------------------------------------------------
			// Adjust direction with clamped persistent var
			xVertical += (Random.value - 0.5f) * test.vertical.x;	xVertical = Mathf.Clamp(xVertical, test.vertical.y, test.vertical.z);
			yHorizontal += (Random.value - 0.5f) * test.horizontal.x;	yHorizontal = Mathf.Clamp(yHorizontal, test.horizontal.y, test.horizontal.z);
			zCoreCamber += (Random.value - 0.5f) * test.camber.x;	zCoreCamber = Mathf.Clamp(zCoreCamber, test.camber.y, test.camber.z);
			coreRot = new Vector3(xVertical, yHorizontal + coreRot.y, zCoreCamber);

			// Inst reference and set position
			GameObject refInst = GameObject.Instantiate(debugRefObject, corePos, Quaternion.Euler(coreRot), transform);
			corePos += refInst.transform.TransformDirection(Vector3.forward * test.stepDistance * test.scale);
			refInst.transform.position = corePos;

			// SIDES ------------------------------------------------------------------------------
			// Adjust camber and width with clamped persistent var
			lCamber += (Random.value - 0.5f) * test.sideCamber.x;	lCamber = Mathf.Clamp(lCamber, test.sideCamber.y, test.sideCamber.z);
			rCamber += (Random.value - 0.5f) * test.sideCamber.x;	rCamber = Mathf.Clamp(rCamber, test.sideCamber.y, test.sideCamber.z);

			lWidth += (Random.value - 0.5f) * test.width.x;	lWidth = Mathf.Clamp(lWidth, test.width.y, test.width.z);
			refInst.transform.rotation = Quaternion.Euler(coreRot + (Vector3.forward * lCamber));
			Vector3 lSide = refInst.transform.position + refInst.transform.TransformDirection(Vector3.right * -lWidth * test.scale);

			rWidth += (Random.value - 0.5f) * test.width.x;	rWidth = Mathf.Clamp(rWidth, test.width.y, test.width.z);
			refInst.transform.rotation = Quaternion.Euler(coreRot + (Vector3.forward * rCamber));
			Vector3 rSide = refInst.transform.position + refInst.transform.TransformDirection(Vector3.right * rWidth * test.scale);
			
			// Inst optional debug references
			// Instantiate(debugRefObject, leftPos, Quaternion.identity, transform);	Instantiate(debugRefObject, rightPos, Quaternion.identity, transform);

			// SLOPE MESH -------------------------------------------------------------------------
			// Store verts in an array (0, 1, 2) clockwise
			slopeVerts[slopeVertNum++] = lSide;	slopeVerts[slopeVertNum++] = corePos;	slopeVerts[slopeVertNum++] = rSide;

			// Set tri draw order for a single section
			// I am drawing the tris clockwise while going left to right across the section (asymetric)
			// Starting points are all along the bottom row with the middle vertice being shared

			if (slopeVertNum > 3)
			{
				// Must be a better way than this mess lol
				slopeTris[slopeStep++] = slopeVertNum - 6;	slopeTris[slopeStep++] = slopeVertNum - 3;	slopeTris[slopeStep++] = slopeVertNum - 5;
				slopeTris[slopeStep++] = slopeVertNum - 5;	slopeTris[slopeStep++] = slopeVertNum - 3;	slopeTris[slopeStep++] = slopeVertNum - 2;
				slopeTris[slopeStep++] = slopeVertNum - 5;	slopeTris[slopeStep++] = slopeVertNum - 2;	slopeTris[slopeStep++] = slopeVertNum - 4;
				slopeTris[slopeStep++] = slopeVertNum - 4;	slopeTris[slopeStep++] = slopeVertNum - 2;	slopeTris[slopeStep++] = slopeVertNum - 1;

				// Store MeshData
				test.slopeData[++slopeTri].vert0 = slopeVerts[slopeVertNum - 6];
				test.slopeData[slopeTri].vert1 = slopeVerts[slopeVertNum - 3];
				test.slopeData[slopeTri].vert2 = slopeVerts[slopeVertNum - 5];
				NextToTri(new Vector3(0, slopeTri + 1, slopeTri - 3));

				test.slopeData[++slopeTri].vert0 = slopeVerts[slopeVertNum - 5];
				test.slopeData[slopeTri].vert1 = slopeVerts[slopeVertNum - 3];
				test.slopeData[slopeTri].vert2 = slopeVerts[slopeVertNum - 2];
				NextToTri(new Vector3(slopeTri - 1, slopeTri + 3, slopeTri + 1));

				test.slopeData[++slopeTri].vert0 = slopeVerts[slopeVertNum - 5];
				test.slopeData[slopeTri].vert1 = slopeVerts[slopeVertNum - 2];
				test.slopeData[slopeTri].vert2 = slopeVerts[slopeVertNum - 4];
				NextToTri(new Vector3(slopeTri - 1, slopeTri + 1, slopeTri - 3));

				test.slopeData[++slopeTri].vert0 = slopeVerts[slopeVertNum - 4];
				test.slopeData[slopeTri].vert1 = slopeVerts[slopeVertNum - 2];
				test.slopeData[slopeTri].vert2 = slopeVerts[slopeVertNum - 1];
				NextToTri(new Vector3(slopeTri - 1, slopeTri + 3, 0));
			}

			// BERMS ------------------------------------------------------------------------------
			// Left & Right vert gen
			refInst.transform.rotation = Quaternion.Euler(coreRot + (Vector3.forward * lCamber));
			lWidth -= test.bermWidth;
			Vector3 lBermInner = refInst.transform.position + refInst.transform.TransformDirection(Vector3.right * -lWidth * test.scale);
			lWidth += test.bermWidth * test.crestOffset;
			Vector3 lBermCrest = refInst.transform.position + refInst.transform.TransformDirection(Vector3.right * -lWidth * test.scale);
			lBermCrest += refInst.transform.TransformDirection(Vector3.up * test.bermHeight * test.scale); // Height offset (local up) FIX

			refInst.transform.rotation = Quaternion.Euler(coreRot + (Vector3.forward * rCamber));
			rWidth -= test.bermWidth;
			Vector3 rBermInner = refInst.transform.position + refInst.transform.TransformDirection(Vector3.right * rWidth * test.scale);
			rWidth += test.bermWidth * test.crestOffset;
			Vector3 rBermCrest = refInst.transform.position + refInst.transform.TransformDirection(Vector3.right * rWidth * test.scale);
			rBermCrest += refInst.transform.TransformDirection(Vector3.up * test.bermHeight * test.scale); // Height offset (local up) FIX

			// Store verts in an array (0, 1, 2) clockwise
			lBermVerts[lBermVertNum++] = lSide;	lBermVerts[lBermVertNum++] = lBermCrest;	lBermVerts[lBermVertNum++] = lBermInner;
			rBermVerts[rBermVertNum++] = rBermInner;	rBermVerts[rBermVertNum++] = rBermCrest;	rBermVerts[rBermVertNum++] = rSide;

			// Set tri draw order
			if (lBermVertNum > 3)
			{
				lBermTris[lBermStep++] = lBermVertNum - 6; lBermTris[lBermStep++] = lBermVertNum - 3; lBermTris[lBermStep++] = lBermVertNum - 5;
				lBermTris[lBermStep++] = lBermVertNum - 5; lBermTris[lBermStep++] = lBermVertNum - 3; lBermTris[lBermStep++] = lBermVertNum - 2;
				lBermTris[lBermStep++] = lBermVertNum - 5; lBermTris[lBermStep++] = lBermVertNum - 2; lBermTris[lBermStep++] = lBermVertNum - 4;
				lBermTris[lBermStep++] = lBermVertNum - 4; lBermTris[lBermStep++] = lBermVertNum - 2; lBermTris[lBermStep++] = lBermVertNum - 1;
			}

			if (rBermVertNum > 3)
			{
				rBermTris[rBermStep++] = rBermVertNum - 6; rBermTris[rBermStep++] = rBermVertNum - 3; rBermTris[rBermStep++] = rBermVertNum - 5;
				rBermTris[rBermStep++] = rBermVertNum - 5; rBermTris[rBermStep++] = rBermVertNum - 3; rBermTris[rBermStep++] = rBermVertNum - 2;
				rBermTris[rBermStep++] = rBermVertNum - 5; rBermTris[rBermStep++] = rBermVertNum - 2; rBermTris[rBermStep++] = rBermVertNum - 4;
				rBermTris[rBermStep++] = rBermVertNum - 4; rBermTris[rBermStep++] = rBermVertNum - 2; rBermTris[rBermStep++] = rBermVertNum - 1;
			}

			// Store preview system arrays
			test.centerVertices[i] = refInst.transform.position;
			test.verticeRotation[i] = Quaternion.Euler(coreRot);

			// Gen banks

			// Gen flora!
			// Likelyhood of Emission
			lFloraEmit += (Random.value - 0.5f) * test.floraEmitRate;	lFloraEmit = Mathf.Clamp(lFloraEmit, 0, 1);
			if (lFloraEmit > 0.5f)
			{
				refInst.transform.rotation = Quaternion.Euler(coreRot + (Vector3.forward * lCamber));
				lWidth += test.floraOffset; // float from Scriptable Object !width var is sullied by berm gen!
				Vector3 lFloraPos = refInst.transform.position + refInst.transform.TransformDirection(Vector3.right * -lWidth * test.scale);
				flora.transform.position = lFloraPos;
				var floraMain = flora.main;
				floraMain.startSizeYMultiplier = 6;
				flora.Emit(1);

				if (Random.value > 0.5f)
				{
					lFloraPos += refInst.transform.TransformDirection(Vector3.right * -test.floraOffset * 2 * test.scale);
					flora.transform.position = lFloraPos;
					floraMain.startSizeYMultiplier = 1.5f;
					flora.Emit(1);
				}
			}

			// Likelyhood of Emission
			rFloraEmit += (Random.value - 0.5f) * test.floraEmitRate;	rFloraEmit = Mathf.Clamp(rFloraEmit, 0, 1);
			if (rFloraEmit > 0.5f)
			{
				refInst.transform.rotation = Quaternion.Euler(coreRot + (Vector3.forward * rCamber));
				rWidth += test.floraOffset; // float from Scriptable Object
				Vector3 rFloraPos = refInst.transform.position + refInst.transform.TransformDirection(Vector3.right * rWidth * test.scale);
				flora.transform.position = rFloraPos;
				var floraMain = flora.main;
				floraMain.startSizeYMultiplier = 6;
				flora.Emit(1);

				if (Random.value > 0.5f)
				{
					rFloraPos += refInst.transform.TransformDirection(Vector3.right * test.floraOffset  * 2 * test.scale);
					flora.transform.position = rFloraPos;
					floraMain.startSizeYMultiplier = 1.5f;
					flora.Emit(1);
				}
			}
		}

		// FINISH ---------------------------------------------------------------------------------
		// Draw Slope and Berm meshes
		Mesh slopeMesh = new Mesh {vertices = slopeVerts, triangles = slopeTris};
		slopeMesh.RecalculateNormals();
		slope.mesh = slopeMesh;

		Mesh lBermMesh = new Mesh {vertices = lBermVerts, triangles = lBermTris};
		lBermMesh.RecalculateNormals();
		lBerm.mesh = lBermMesh;

		Mesh rBermMesh = new Mesh{vertices = rBermVerts, triangles = rBermTris};
		rBermMesh.RecalculateNormals();
		rBerm.mesh = rBermMesh;

		watch.Stop();

		procGenFeedback = "DONE in " + watch.Elapsed.TotalSeconds.ToString("F6") + " seconds";
		procgenProgress = 1f;

		// Close with a populated scene declaration
		test.cleanScene = false;

		// Clear refInstances
		foreach (Transform child in transform)
		{
			GameObject.Destroy(child.gameObject);
		}

		// Log performance stats
		UnityEngine.Debug.Log("Slope Vertices: " + slopeMesh.vertices.Length);
		UnityEngine.Debug.Log("Slope Triangles: " + slopeMesh.triangles.Length);

		UnityEngine.Debug.Log("Berm Vertices: " + (lBermMesh.vertices.Length * 2));
		UnityEngine.Debug.Log("Berm Triangles: " + (lBermMesh.triangles.Length * 2));
	}

	void NextToTri (Vector3 sides)
	{
		// handles the start of the slopes physics
		if (slopeVertNum <= 6)
		{
			test.slopeData[slopeTri].nextTo = new Vector3(Mathf.Clamp(sides.x, 0, 69), Mathf.Clamp(sides.y, 0, 69), Mathf.Clamp(sides.z, 0, 69));
		}
		else
		{
			// handles the end of the slope
			if (slopeVertNum/3 > length - 1)
			{
				// if a side is greater than slopeTri + 1 then set to 0...
				if (sides.x > slopeTri + 1)
					sides.x = 0;

				if (sides.y > slopeTri + 1)
					sides.y = 0;

				if (sides.z > slopeTri + 1)
					sides.z = 0;

				UnityEngine.Debug.Log("hey its being used");
			}

			test.slopeData[slopeTri].nextTo = sides;
		}
	}

	void PreviewSlope()
	{
		Vector3 fromPos = test.centerVertices[next] + Vector3.up / 4;	Quaternion fromRot = test.verticeRotation[next];
		Vector3 toPos = test.centerVertices[next + 1] + Vector3.up / 4;	Quaternion toRot = test.verticeRotation[next + 1];

		previewCam.position = Vector3.Lerp(fromPos, toPos, transition);
		previewCam.rotation = Quaternion.Lerp(fromRot, toRot, transition);
		transition += Time.deltaTime * 3;

		if (transition > 1)
		{
			transition = 0;
			next++;
		}

		if (next >= test.centerVertices.Length)
			next = 0;
		
	}
}
