using UnityEngine;

[DisallowMultipleComponent]
public class PolarBezierRuntimeDragger2D : MonoBehaviour
{
	public PolarBezierCurveVisualizer2D viz;

	[Header("Picking")]
	public Camera cam;
	[Tooltip("Max distance in world units to grab a point")]
	public float pickRadius = 0.35f;

	[Header("Drag")]
	public bool dragInXYPlane = true;
	public bool allowDraggingOriginIfNull = false;

	int grabbedIndex = -1;

	void Awake()
	{
		if (!viz) viz = GetComponent<PolarBezierCurveVisualizer2D>();
		if (!cam) cam = Camera.main;
	}

	void Update()
	{
		if (!viz || viz.points == null || viz.points.Count == 0) return;
		if (!cam) return;

		if (Input.touchCount > 0)
		{
			var t = Input.GetTouch(0);
			Vector2 w = ScreenToWorld2D(t.position);

			if (t.phase == TouchPhase.Began)
				TryGrab(w);
			else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
				DragTo(w);
			else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
				Release();

			return;
		}

		// Mouse
		Vector2 world = ScreenToWorld2D(Input.mousePosition);

		if (Input.GetMouseButtonDown(0))
			TryGrab(world);

		if (Input.GetMouseButton(0))
			DragTo(world);

		if (Input.GetMouseButtonUp(0))
			Release();
	}

	void TryGrab(Vector2 worldPos)
	{
		grabbedIndex = FindNearestPointIndex(worldPos, pickRadius);
	}

	void DragTo(Vector2 worldPos)
	{
		if (grabbedIndex < 0) return;

		Vector2 origin = GetOrigin();

		Vector2 d = worldPos - origin;
		var pp = viz.points[grabbedIndex];

		pp.radius = d.magnitude;
		pp.angleDeg = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

	}

	void Release()
	{
		grabbedIndex = -1;
	}

	int FindNearestPointIndex(Vector2 worldPos, float maxDist)
	{
		Vector2 origin = GetOrigin();
		float best = maxDist * maxDist;
		int bestIdx = -1;

		for (int i = 0; i < viz.points.Count; i++)
		{
			Vector2 p = PolarToCartesian(origin, viz.points[i]);
			float dsq = (p - worldPos).sqrMagnitude;
			if (dsq <= best)
			{
				best = dsq;
				bestIdx = i;
			}
		}
		return bestIdx;
	}

	Vector2 ScreenToWorld2D(Vector2 screen)
	{
		float z = 0f;
		if (dragInXYPlane)
		{
			var v = new Vector3(screen.x, screen.y, Mathf.Abs(cam.transform.position.z));
			Vector3 w = cam.ScreenToWorldPoint(v);
			return new Vector2(w.x, w.y);
		}
		else
		{
			Vector3 w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z));
			return new Vector2(w.x, w.y);
		}
	}

	Vector2 GetOrigin()
	{
		if (viz.origin) return (Vector2)viz.origin.position + viz.originOffset;
		if (allowDraggingOriginIfNull) return (Vector2)viz.transform.position + viz.originOffset;
		return (Vector2)viz.transform.position + viz.originOffset;
	}

	static Vector2 PolarToCartesian(Vector2 origin, PolarBezierCurveVisualizer2D.PolarPoint pp)
	{
		float rad = pp.angleDeg * Mathf.Deg2Rad;
		return origin + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * pp.radius;
	}
}
