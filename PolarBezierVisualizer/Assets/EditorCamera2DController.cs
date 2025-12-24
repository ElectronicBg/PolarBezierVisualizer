using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EditorCamera2DController : MonoBehaviour
{
	[Header("References")]
	public Camera cam;
	public PolarBezierCurveVisualizer2D viz;

	[Header("Pan")]
	public bool middleMousePan = true;
	public bool rightMousePan = true;
	public bool spaceLeftMousePan = true;
	public float panSpeed = 1f;
	[Tooltip("Колко бързо камерата догонва target позицията. По-голямо = по-стегнато.")]
	public float panDamping = 14f;

	[Header("Zoom (Orthographic)")]
	public float zoomSpeed = 0.8f;
	public float minOrthoSize = 0.5f;
	public float maxOrthoSize = 50f;
	public bool zoomToCursor = true;
	[Tooltip("Колко бързо camera size догонва target. По-голямо = по-стегнато.")]
	public float zoomDamping = 14f;

	[Header("Frame Curve")]
	public float framePadding = 1.2f;
	public int frameSampleCount = 128;
	public KeyCode frameHotkey = KeyCode.F;

	// smooth targets
	Vector3 targetCamPos;
	float targetOrthoSize;

	// drag state
	bool isPanning = false;
	Vector3 lastMouseWorld;

	void Awake()
	{
		if (!cam) cam = GetComponent<Camera>();
		if (!cam) cam = Camera.main;
		if (!viz) viz = FindFirstObjectByType<PolarBezierCurveVisualizer2D>();

		targetCamPos = cam.transform.position;
		if (cam.orthographic) targetOrthoSize = cam.orthographicSize;
	}

	void Update()
	{
		if (!cam) return;

		HandlePanInput();
		HandleZoomInput();

		if (Input.GetKeyDown(frameHotkey))
			FrameCurve();

		ApplySmoothing();
	}

	void HandlePanInput()
	{
		bool wantPan =
			(middleMousePan && Input.GetMouseButton(2)) ||
			(rightMousePan && Input.GetMouseButton(1)) ||
			(spaceLeftMousePan && Input.GetKey(KeyCode.Space) && Input.GetMouseButton(0));

		if (wantPan)
		{
			Vector3 mouseWorld = ScreenToWorldOnZ0(Input.mousePosition);

			if (!isPanning)
			{
				isPanning = true;
				lastMouseWorld = mouseWorld;
			}

			Vector3 delta = lastMouseWorld - mouseWorld;
			targetCamPos += delta * panSpeed;

			lastMouseWorld = mouseWorld;
		}
		else
		{
			isPanning = false;
		}
	}

	void HandleZoomInput()
	{
		if (!cam.orthographic) return;

		float scroll = Input.mouseScrollDelta.y;
		if (Mathf.Approximately(scroll, 0f)) return;

		float oldSize = targetOrthoSize;
		float newSize = oldSize * Mathf.Pow(1f - zoomSpeed, scroll);
		newSize = Mathf.Clamp(newSize, minOrthoSize, maxOrthoSize);

		if (zoomToCursor)
		{
			Vector3 before = ScreenToWorldOnZ0(Input.mousePosition);

			float currentRealSize = cam.orthographicSize;
			cam.orthographicSize = newSize;
			Vector3 after = ScreenToWorldOnZ0(Input.mousePosition);
			cam.orthographicSize = currentRealSize;

			Vector3 diff = before - after;
			targetCamPos += diff;
		}

		targetOrthoSize = newSize;
	}

	void ApplySmoothing()
	{
		// Smooth pan
		if (panDamping <= 0f)
		{
			cam.transform.position = targetCamPos;
		}
		else
		{
			float a = 1f - Mathf.Exp(-panDamping * Time.unscaledDeltaTime);
			cam.transform.position = Vector3.Lerp(cam.transform.position, targetCamPos, a);
		}

		// Smooth zoom
		if (cam.orthographic)
		{
			if (zoomDamping <= 0f)
			{
				cam.orthographicSize = targetOrthoSize;
			}
			else
			{
				float a = 1f - Mathf.Exp(-zoomDamping * Time.unscaledDeltaTime);
				cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, a);
			}
		}
	}

	Vector3 ScreenToWorldOnZ0(Vector3 screenPos)
	{
		float z = Mathf.Abs(cam.transform.position.z);
		return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
	}

	// ---------- Frame Curve ----------

	public void FrameCurve()
	{
		if (!cam || !viz || viz.points == null || viz.points.Count < 2) return;

		Vector2 origin = GetOrigin(viz);
		var cps = new List<Vector2>(viz.points.Count);
		for (int i = 0; i < viz.points.Count; i++)
			cps.Add(PolarToCartesian(origin, viz.points[i]));

		Bounds b = new Bounds(BezierN(cps, 0f), Vector3.zero);
		for (int i = 1; i <= frameSampleCount; i++)
		{
			float t = i / (float)frameSampleCount;
			Vector2 p = BezierN(cps, t);
			b.Encapsulate(new Vector3(p.x, p.y, 0f));
		}


		targetCamPos = cam.transform.position;
		targetCamPos.x = b.center.x;
		targetCamPos.y = b.center.y;

		if (!cam.orthographic) return;

		float halfHeight = b.extents.y * framePadding;
		float halfWidth = b.extents.x * framePadding;

		float sizeByHeight = halfHeight;
		float sizeByWidth = halfWidth / Mathf.Max(0.0001f, cam.aspect);

		float needed = Mathf.Max(sizeByHeight, sizeByWidth, minOrthoSize);
		targetOrthoSize = Mathf.Clamp(needed, minOrthoSize, maxOrthoSize);
	}

	static Vector2 GetOrigin(PolarBezierCurveVisualizer2D v)
	{
		Vector2 o = v.origin ? (Vector2)v.origin.position : (Vector2)v.transform.position;
		return o + v.originOffset;
	}

	static Vector2 PolarToCartesian(Vector2 origin, PolarBezierCurveVisualizer2D.PolarPoint pp)
	{
		float rad = pp.angleDeg * Mathf.Deg2Rad;
		return origin + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * pp.radius;
	}

	static Vector2 BezierN(IReadOnlyList<Vector2> cps, float t)
	{
		int n = cps.Count;
		Vector2[] tmp = new Vector2[n];
		for (int i = 0; i < n; i++) tmp[i] = cps[i];

		for (int k = n - 1; k > 0; k--)
			for (int i = 0; i < k; i++)
				tmp[i] = Vector2.Lerp(tmp[i], tmp[i + 1], t);

		return tmp[0];
	}
}
