using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Box : MonoBehaviour
{
	[Header("References")]
	public GameObject playerFrontEnd;
	public Rigidbody rbBox;
	public GameObject head;
	public GameObject wind;
	public GameObject boxSled;
	public ParticleSystem snowSpray;

	[Header("Variables")]
	public Vector3 centerMass = Vector3.zero;
	public float expectedTime;

	[Header("Drag")]
	[Range(0, 1)]
	public float xDrag = 0.97f;
	[Range(0, 1)]
	public float backDrag = 0.97f;
	[Range(0, 1)]
	public float upDrag = 0.97f;
	
	[Header("Lean")]
	public float leanPower = 1.5f;
	public float speedToLean = 0.4f;
	public float boxLean = 30f;

	[Header("Wind")]
	public AnimationCurve windCurve;

	[Header("Particle Systems")]
	public float spraySpeed = 3;

	void Start()
	{
		// Set Center of Gravity
		rbBox.centerOfMass = centerMass;
	}

	void Update()
	{
		// Reset Scene
		if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.Active) == true)
		{
			SceneManager.LoadScene(0);
		}

		// playerFrontEnd Move with box (add if statement for times when you box isn't present)
		playerFrontEnd.transform.rotation = transform.rotation;
		playerFrontEnd.transform.position = transform.position;
	}

	void FixedUpdate()
	{
		// Position wind audioSource
		if (rbBox.velocity.magnitude > 0.1f)
		{
			Vector3 normalVelocity = Vector3.Normalize(rbBox.velocity);
			wind.transform.rotation = Quaternion.LookRotation(normalVelocity);
		}

		// Amplitude of said wind
		AudioSource windSound = wind.transform.GetComponentInChildren<AudioSource>();
		float windVolumeRaw = rbBox.velocity.magnitude/25;
		windSound.volume = Mathf.Clamp(windCurve.Evaluate(windVolumeRaw), 0, 1);

		// Lean to turn (faster you are going the easier you turn)
		Vector3 headPos = head.transform.localPosition * leanPower;
		rbBox.AddRelativeTorque(Vector3.up * (headPos.x * Mathf.Abs(headPos.x)) * (rbBox.velocity.magnitude * speedToLean));

		// Box Mesh leans with you
		boxSled.transform.localEulerAngles = new Vector3(0, 0, headPos.x * Mathf.Abs(headPos.x) * -boxLean);

		// Snow spray
		var sprayMain = snowSpray.main;
		sprayMain.startSpeed = spraySpeed * rbBox.velocity.magnitude / 25;

		// Set max speed
		rbBox.velocity = Vector3.ClampMagnitude(rbBox.velocity, 25f);
	}

	private void OnCollisionStay(Collision collision)
	{
		// Snow Spray
		var snowEmission = snowSpray.emission;

		if (snowEmission.enabled == false)
		{
			snowEmission.enabled = true;
		}

		// UnityPhysics drag dynamic variables
		rbBox.drag = Mathf.Clamp(2 - (rbBox.velocity.magnitude / 2.5f), 0, 2);
		rbBox.angularDrag = Mathf.Clamp(5 - (rbBox.angularVelocity.magnitude * 2f), 2, 5);

		// Manual drag
		Vector3 vel = transform.InverseTransformDirection(rbBox.velocity);
		vel.x *= xDrag;

		if (vel.z < 0)
		{
			vel.z *= backDrag;
		}

		if (vel.y > 0)
		{
			// vel.z *= upDrag;
		}

		rbBox.velocity = transform.TransformDirection(vel);

		// Reset Rotation <<Improve with failsafes and transition
		if (Vector3.Angle(Vector3.up, transform.TransformDirection(Vector3.up)) > 100)
		{
			transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
			transform.position += Vector3.up;
		}
	}

	private void OnCollisionExit(Collision collision)
	{
		// In air reduce drag
		rbBox.drag = 0;
		rbBox.angularDrag = 0;

		// Snow Spray
		var snowEmission = snowSpray.emission;
		snowEmission.enabled = false;
	}
}
