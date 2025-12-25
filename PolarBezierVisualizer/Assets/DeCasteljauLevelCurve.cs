using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DeCasteljauLevelCurve2D : MonoBehaviour
{
	[Header("Marker Pool")]
	public bool destroyExcessMarkers = true;

	[Header("References")]
	public PolarBezierCurveVisualizer2D viz;
	public Camera cam;

	[Header("Parameter t (0..1)")]
	[Range(0f, 1f)] public float t = 0.5f;

	[Header("Render (secondary Bezier from P^(1)_i(t))")]
	public int segments = 96;
	public Material curveMaterial;
	public float curveWidth = 0.05f;
	public string sortingLayerName = "Default";
	public int sortingOrder = 9;

	[Header("Show intermediate points")]
	public bool showPoints = true;
	public float pointSize = 0.18f;
	public int pointSortingOrder = 20;

	LineRenderer lr;
	readonly List<SpriteRenderer> pointMarkers = new();

	[SerializeField] Transform markersRoot;
	const string MarkersRootName = "__DeCasteljauMarkers__";

	void OnEnable()
	{
		if (!viz) viz = FindFirstObjectByType<PolarBezierCurveVisualizer2D>();
		if (!cam) cam = Camera.main;

		EnsureLineRenderer();
		EnsureMarkersRoot();

		CleanupGeneratedMarkers();
	}

	void OnDisable()
	{
		CleanupGeneratedMarkers();
	}

	void Update()
	{
		if (!viz || viz.points == null || viz.points.Count < 2)
		{
			if (lr) lr.enabled = false;
			SetPointsActive(false);
			return;
		}

		EnsureLineRenderer();
		lr.enabled = true;

		Vector2 origin = GetOrigin(viz);
		var P = new List<Vector2>(viz.points.Count);
		for (int i = 0; i < viz.points.Count; i++)
			P.Add(PolarToCartesian(origin, viz.points[i]));

		int m = P.Count - 1;
		var P1 = new List<Vector2>(m);
		for (int i = 0; i < m; i++)
			P1.Add(Vector2.Lerp(P[i], P[i + 1], t));

		lr.positionCount = segments + 1;
		for (int i = 0; i <= segments; i++)
		{
			float u = i / (float)segments;
			Vector2 c = BezierN(P1, u);
			lr.SetPosition(i, new Vector3(c.x, c.y, 0f));
		}

		if (showPoints)
		{
			EnsurePointMarkers(P1.Count);
			for (int i = 0; i < P1.Count; i++)
			{
				var sr = pointMarkers[i];
				sr.transform.position = new Vector3(P1[i].x, P1[i].y, 0f);
				sr.transform.localScale = Vector3.one * pointSize;
			}
		}
		else
		{
			SetPointsActive(false);
		}
	}

	void EnsureLineRenderer()
	{
		if (!lr)
		{
			lr = GetComponent<LineRenderer>();
			if (!lr) lr = gameObject.AddComponent<LineRenderer>();
		}

		lr.useWorldSpace = true;
		lr.startWidth = lr.endWidth = curveWidth;
		lr.numCapVertices = 8;
		lr.numCornerVertices = 8;

		if (!curveMaterial)
			curveMaterial = new Material(Shader.Find("Sprites/Default"));
		lr.material = curveMaterial;

		lr.sortingLayerName = sortingLayerName;
		lr.sortingOrder = sortingOrder;
	}

	void EnsureMarkersRoot()
	{
		if (markersRoot != null) return;

		var existing = transform.Find(MarkersRootName);
		if (existing != null)
		{
			markersRoot = existing;
			return;
		}

		var go = new GameObject(MarkersRootName);

		go.hideFlags = HideFlags.DontSaveInBuild;
		go.transform.SetParent(transform, false);
		markersRoot = go.transform;
	}

	void CleanupGeneratedMarkers()
	{
		EnsureMarkersRoot();

		pointMarkers.Clear();

		for (int i = markersRoot.childCount - 1; i >= 0; i--)
		{
			var ch = markersRoot.GetChild(i).gameObject;
			if (Application.isPlaying) Destroy(ch);
			else DestroyImmediate(ch);
		}
	}

	void EnsurePointMarkers(int needed)
	{
		EnsureMarkersRoot();

		while (pointMarkers.Count < needed)
		{
			var go = new GameObject($"P1_{pointMarkers.Count}");
			go.transform.SetParent(markersRoot, false);

			var sr = go.AddComponent<SpriteRenderer>();
			sr.sprite = CreateCircleSprite(64);
			sr.sortingLayerName = sortingLayerName;
			sr.sortingOrder = pointSortingOrder;
			sr.color = Color.white;

			pointMarkers.Add(sr);
		}

		for (int i = 0; i < pointMarkers.Count; i++)
		{
			if (!pointMarkers[i]) continue;
			bool on = i < needed;

			pointMarkers[i].gameObject.SetActive(on);

			if (!on)
			{
				pointMarkers[i].transform.localPosition = Vector3.zero;
			}
			else
			{
				pointMarkers[i].sortingLayerName = sortingLayerName;
				pointMarkers[i].sortingOrder = pointSortingOrder;
			}
		}

		if (destroyExcessMarkers)
			TrimMarkersTo(needed);
	}

	void TrimMarkersTo(int needed)
	{
		for (int i = pointMarkers.Count - 1; i >= needed; i--)
		{
			var sr = pointMarkers[i];
			if (sr)
			{
				if (Application.isPlaying) Destroy(sr.gameObject);
				else DestroyImmediate(sr.gameObject);
			}
			pointMarkers.RemoveAt(i);
		}
	}

	void SetPointsActive(bool on)
	{
		for (int i = 0; i < pointMarkers.Count; i++)
			if (pointMarkers[i]) pointMarkers[i].gameObject.SetActive(on);
	}

	// --------- Helpers ---------

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

	static Vector2 BezierN(IReadOnlyList<Vector2> cps, float u)
	{
		int n = cps.Count;
		Vector2[] tmp = new Vector2[n];
		for (int i = 0; i < n; i++) tmp[i] = cps[i];

		for (int k = n - 1; k > 0; k--)
			for (int i = 0; i < k; i++)
				tmp[i] = Vector2.Lerp(tmp[i], tmp[i + 1], u);

		return tmp[0];
	}

	static Sprite CreateCircleSprite(int size)
	{
		var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
		tex.filterMode = FilterMode.Bilinear;
		tex.wrapMode = TextureWrapMode.Clamp;

		float r = (size - 1) * 0.5f;
		Vector2 c = new Vector2(r, r);
		float r2 = r * r;

		for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				Vector2 p = new Vector2(x, y) - c;
				float d2 = p.sqrMagnitude;

				float d = Mathf.Sqrt(d2);
				float edge = r - d;
				float feather = 1.5f;
				float a = (d2 <= r2) ? Mathf.Clamp01(edge / feather) : 0f;

				tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
			}

		tex.Apply();
		return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
	}
}
