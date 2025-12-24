using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class PolarBezierCurveVisualizer2D : MonoBehaviour
{
	[Header("Polar Origin (anchor)")]
	public Transform origin;
	public Vector2 originOffset = Vector2.zero;

	[System.Serializable]
	public class PolarPoint
	{
		[Min(0f)] public float radius = 2f;
		[Tooltip("Degrees")] public float angleDeg = 0f;
		public PolarPoint() { }
		public PolarPoint(float r, float aDeg) { radius = r; angleDeg = aDeg; }
	}

	[Header("Control Points (N points => Bezier degree N-1)")]
	public List<PolarPoint> points = new List<PolarPoint>()
	{
		new PolarPoint(2f, 180f),
		new PolarPoint(2f, 135f),
		new PolarPoint(2f, 45f),
		new PolarPoint(2f, 0f),
	};

	[Header("Rendering")]
	[Header("Curve Resolution")]
	public int minSegments = 32;
	public int maxSegments = 1024;
	public float pixelsPerSegment = 6f;

	public bool drawControlPolygon = true;

	[Header("Quality Boost")]
	[Range(0f, 2f)] public float curvatureBoost = 0.6f;
	[Range(16, 256)] public int lengthSamples = 64;

	[Header("Gizmos")]
	public float handleSize = 0.08f;

	[Header("Sorting")]
	public string sortingLayerName = "Default";
	public int curveSortingOrder = 10;

	private LineRenderer lr;

	void OnEnable()
	{
		lr = GetComponent<LineRenderer>();
		lr.useWorldSpace = true;
		lr.sortingLayerName = sortingLayerName;
		lr.sortingOrder = curveSortingOrder;


		if (points == null) points = new List<PolarPoint>();

		if (points.Count < 2)
		{
			points.Clear();
			points.Add(new PolarPoint(2f, 180f));
			points.Add(new PolarPoint(2f, 0f));
		}
	}

	void Update()
	{
		Draw();
	}

	Vector2 GetOrigin()
	{
		Vector2 o = origin ? (Vector2)origin.position : (Vector2)transform.position;
		return o + originOffset;
	}

	static Vector2 PolarToCartesian(Vector2 origin, PolarPoint pp)
	{
		float rad = pp.angleDeg * Mathf.Deg2Rad;
		return origin + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * pp.radius;
	}

	static Vector2 BezierN(IReadOnlyList<Vector2> cps, float t)
	{
		int n = cps.Count;
		if (n == 1) return cps[0];

		Vector2[] tmp = new Vector2[n];
		for (int i = 0; i < n; i++) tmp[i] = cps[i];

		for (int k = n - 1; k > 0; k--)
		{
			for (int i = 0; i < k; i++)
				tmp[i] = Vector2.Lerp(tmp[i], tmp[i + 1], t);
		}
		return tmp[0];
	}

	void Draw()
	{
		if (lr == null) lr = GetComponent<LineRenderer>();
		if (points == null || points.Count < 2) return;

		var cam = Camera.main;
		if (!cam || !cam.orthographic) return;

		Vector2 o = GetOrigin();

		var cps = new List<Vector2>(points.Count);
		for (int i = 0; i < points.Count; i++)
			cps.Add(PolarToCartesian(o, points[i]));

		float length = EstimateCurveLength(cps);

		float worldUnitsPerPixel = (2f * cam.orthographicSize) / Screen.height;
		float pixelLength = length / Mathf.Max(1e-6f, worldUnitsPerPixel);

		int segs = Mathf.Clamp(
			Mathf.CeilToInt(pixelLength / Mathf.Max(0.5f, pixelsPerSegment)),
			minSegments,
			maxSegments
		);

		float k = EstimateCurvatureFactor(cps);
		segs = Mathf.Clamp(Mathf.RoundToInt(segs * (1f + curvatureBoost * k)), minSegments, maxSegments);

		lr.positionCount = segs + 1;

		for (int i = 0; i <= segs; i++)
		{
			float t = i / (float)segs;
			lr.SetPosition(i, BezierN(cps, t));
		}
	}

	float EstimateCurvatureFactor(List<Vector2> cps)
	{
		int samples = 64;
		Vector2 p0 = BezierN(cps, 0f);
		Vector2 p1 = BezierN(cps, 1f / samples);

		float sum = 0f;
		for (int i = 2; i <= samples; i++)
		{
			float t = i / (float)samples;
			Vector2 p2 = BezierN(cps, t);

			Vector2 a = (p1 - p0);
			Vector2 b = (p2 - p1);

			float la = a.magnitude;
			float lb = b.magnitude;
			if (la > 1e-6f && lb > 1e-6f)
			{
				float cos = Mathf.Clamp(Vector2.Dot(a, b) / (la * lb), -1f, 1f);
				float angle = Mathf.Acos(cos);
				sum += angle;
			}

			p0 = p1;
			p1 = p2;
		}

		return Mathf.Clamp01(sum / 20f);
	}

	float EstimateCurveLength(List<Vector2> cps)
	{
		const int samples = 32;
		Vector2 prev = BezierN(cps, 0f);
		float sum = 0f;

		for (int i = 1; i <= samples; i++)
		{
			float t = i / (float)samples;
			Vector2 p = BezierN(cps, t);
			sum += Vector2.Distance(prev, p);
			prev = p;
		}

		return sum;
	}

	void OnDrawGizmos()
	{
		if (points == null || points.Count == 0) return;

		Vector2 o = GetOrigin();
		Gizmos.color = Color.white;
		Gizmos.DrawWireSphere(o, handleSize);

		// control polygon
		if (drawControlPolygon && points.Count >= 2)
		{
			Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
			Vector2 prev = PolarToCartesian(o, points[0]);
			for (int i = 1; i < points.Count; i++)
			{
				Vector2 cur = PolarToCartesian(o, points[i]);
				Gizmos.DrawLine(prev, cur);
				prev = cur;
			}
		}

		// points
		for (int i = 0; i < points.Count; i++)
		{
			Vector2 p = PolarToCartesian(o, points[i]);
			Gizmos.color = (i == 0) ? Color.green : (i == points.Count - 1 ? Color.red : Color.yellow);
			Gizmos.DrawSphere(p, handleSize * 0.9f);
		}
	}
}
