using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hands : MonoBehaviour
{
	[Header("References")]
    public GameObject box;
	public Rigidbody rbBox;
	public ParticleSystem snowParticles;

	[Header("Variables")]
	public bool right = true;

	[Header("Reference Variables")]
	public float depth;

	// Adjustable variables (put in box script?)
	private float pushForce = 25;
	private float posDrag = 0.0025f;
	private float handVelocityDrag = 2;
	private float snowHaptics = 100;
	private float particleSize = 0.1f;
	private float particleRate = 1;

	private Vector3 boxPos;
    private Vector3 TouchPos;
    private Vector3 TouchVel;
	private Vector3 pastPos;
	
	// Oculus specific variables
	OVRInput.Controller whichHand;
	OVRHaptics.OVRHapticsChannel whichChannel;

	void Awake ()
	{
		if (right == true)
		{
			whichHand = OVRInput.Controller.RTouch;
			whichChannel = OVRHaptics.RightChannel;
		}
		else
		{
			whichHand = OVRInput.Controller.LTouch;
			whichChannel = OVRHaptics.LeftChannel;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		// Prime reference variables
		Cache();
	}

	private void OnTriggerStay(Collider other)
    {
		Vector3 TouchCurrentPos = Vector3.Scale(OVRInput.GetLocalControllerPosition(whichHand), new Vector3(1, 0, 1));

		// Determine if hand is outside of box
		if ((TouchCurrentPos.x > 0.25f || TouchCurrentPos.x < -0.25f) || (TouchCurrentPos.z > 0.35f || TouchCurrentPos.z < -0.35f))
		{
			// Hand depth in snow collider
			depth = Mathf.Clamp(Vector3.Distance(other.ClosestPoint(transform.position), transform.position) * -10 + 1, 0, 1);

			// Hand moving in relation to snow
			float snowMove = Mathf.Clamp(Vector3.Distance(pastPos, transform.position) * snowHaptics, 0, 1);

			// Snow Particle Effects
			snowParticles.transform.position = other.ClosestPointOnBounds(transform.position);
			var particlesMain = snowParticles.main;
			particlesMain.startSize = particleSize * depth;
			particlesMain.startRotationX = box.transform.eulerAngles.x * Mathf.Deg2Rad;
			particlesMain.startRotationY = box.transform.eulerAngles.y * Mathf.Deg2Rad;
			particlesMain.startRotationZ = box.transform.eulerAngles.z * Mathf.Deg2Rad;
			snowParticles.Emit(System.Convert.ToInt32(particleRate * snowMove));

			// Oculus Touch Haptics
			byte theByte = System.Convert.ToByte(Mathf.Clamp(Random.Range(150, 175) * snowMove * depth, 0, 255));
			byte halfByte = System.Convert.ToByte(Mathf.Clamp(theByte / 2, 0, 255));
			byte[] bigByte = new byte[] {theByte, halfByte, theByte, halfByte, theByte, halfByte, theByte, halfByte, theByte, halfByte};

			OVRHapticsClip aClip = new OVRHapticsClip(bigByte, 10);
			whichChannel.Preempt(aClip);

			// Angular force and drag
			Vector3 offset = Vector3.Scale(box.transform.position - boxPos, new Vector3(1, 0, 1) * 0.25f);

			float angleTouch = Vector3.Angle(Vector3.Cross(-TouchPos, TouchCurrentPos).normalized, Vector3.Scale(TouchPos, new Vector3(1, 0, 1)));
			angleTouch -= Vector3.Angle(Vector3.Cross(-TouchPos, TouchCurrentPos).normalized, TouchCurrentPos + box.transform.InverseTransformDirection(offset/2));
			rbBox.AddRelativeTorque(Vector3.up * angleTouch * depth);

			// Positional force
			rbBox.AddRelativeForce(Vector3.Normalize(TouchVel) * Vector3.Magnitude(TouchVel) * -pushForce * depth);

			// Positional drag
			float velOffset = Mathf.Clamp(posDrag * (Vector3.Magnitude(TouchVel) * handVelocityDrag * depth), 0f, posDrag);
			rbBox.velocity = new Vector3(rbBox.velocity.x * (0.995f + velOffset), rbBox.velocity.y, rbBox.velocity.z * (0.995f + velOffset));
		}

		Cache();
	}

	void Cache ()
	{
		// Get variable references to use next frame
		boxPos = box.transform.position;

		TouchVel = Vector3.Scale(OVRInput.GetLocalControllerVelocity(whichHand), new Vector3(1, 0, 1));
		TouchPos = OVRInput.GetLocalControllerPosition(whichHand);
		pastPos = transform.position;
	}
}
