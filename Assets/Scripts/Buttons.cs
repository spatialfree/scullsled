using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buttons : MonoBehaviour
{
	[TestButton("Generate tri", "MeshGen", isActiveInEditor = false)]
	[TestButton("Place point", "PlacePoint", isActiveInEditor = false)]
	[TestButton("Move point", "MovePoint", isActiveInEditor = false)]
	[TestButton("Move camera", "MoveCamera", isActiveInEditor = false)]

	[ProgressBar(hideWhenZero = true, label = "castFeedback")]

	public float empty;
	public float cameraSpeed;
	public float pointSpeed;

	[HideInInspector]
	public string castFeedback;
}
