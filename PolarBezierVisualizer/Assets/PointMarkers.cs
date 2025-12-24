using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PolarBezierPointMarkers2D : MonoBehaviour
{
	public PolarBezierCurveVisualizer2D viz;

	[Header("Marker Look")]
	public float markerWorldSize = 0.18f;
	public Sprite markerSprite;
	public int sortingOrder = 10;

	[Header("Colors")]
	public Color firstColor = Color.green;
	public Color lastColor = Color.red;
	public Color middleColor = Color.yellow;
	public Color grabbedColor = Color.white;

	[Header("Optional: show control polygon in Play")]
	public bool drawControlPolygon = true;
	public float polygonWidth = 0.04f;

	readonly List<SpriteRenderer> renderers = new();
	LineRenderer polyLR;

	void Awake()
	{
		if (!viz) viz = GetComponent<PolarBezierCurveVisualizer2D>();
		EnsureResources();
		RebuildMarkers();
		EnsurePolygonRenderer();
	}

	void OnEnable()
	{
		EnsureResources();
		RebuildMarkers();
		EnsurePolygonRenderer();
	}

	void Update()
	{
		if (!viz || viz.points == null) return;

		if (renderers.Count != viz.points.Count)
			RebuildMarkers();

		UpdateMarkers();
		UpdatePolygon();
	}

	void EnsureResources()
	{
		if (markerSprite == null)
		{
			markerSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
		}
	}

	void RebuildMarkers()
	{
		for (int i = transform.childCount - 1; i >= 0; i--)
		{
			var ch = transform.GetChild(i);
			if (Application.isPlaying) Destroy(ch.gameObject);
			else DestroyImmediate(ch.gameObject);
		}
		renderers.Clear();

		if (!viz || viz.points == null) return;

		for (int i = 0; i < viz.points.Count; i++)
		{
			var go = new GameObject($"P{i}_Marker");
			go.transform.SetParent(transform, false);

			var sr = go.AddComponent<SpriteRenderer>();
			sr.sprite = markerSprite;
			sr.sortingOrder = sortingOrder;

			renderers.Add(sr);
		}
	}

	void UpdateMarkers()
	{
		Vector2 o = GetOrigin();
		for (int i = 0; i < viz.points.Count; i++)
		{
			var pp = viz.points[i];
			Vector2 w = PolarToCartesian(o, pp);

			var sr = renderers[i];
			sr.transform.position = new Vector3(w.x, w.y, 0f);

			float baseSize = sr.sprite ? sr.sprite.bounds.size.x : 1f;
			float scale = (baseSize > 0f) ? (markerWorldSize / baseSize) : markerWorldSize;
			sr.transform.localScale = new Vector3(scale, scale, 1f);

			if (i == 0) sr.color = firstColor;
			else if (i == viz.points.Count - 1) sr.color = lastColor;
			else sr.color = middleColor;
		}

		var dragger = GetComponent<PolarBezierRuntimeDragger2D>();
		if (dragger != null)
		{
			int gi = GetGrabbedIndex(dragger);
			if (gi >= 0 && gi < renderers.Count)
				renderers[gi].color = grabbedColor;
		}
	}

	void EnsurePolygonRenderer()
	{
		if (!drawControlPolygon) return;

		if (!polyLR)
		{
			var go = new GameObject("ControlPolygon");
			go.transform.SetParent(transform, false);
			polyLR = go.AddComponent<LineRenderer>();
			polyLR.useWorldSpace = true;
			polyLR.loop = false;
			polyLR.material = new Material(Shader.Find("Sprites/Default"));
			polyLR.sortingOrder = sortingOrder - 1;
		}

		polyLR.numCapVertices = 8;    
		polyLR.numCornerVertices = 8;
		polyLR.alignment = LineAlignment.View;
		polyLR.textureMode = LineTextureMode.Stretch;


		polyLR.startWidth = polygonWidth;
		polyLR.endWidth = polygonWidth;
	}

	void UpdatePolygon()
	{
		if (!drawControlPolygon)
		{
			if (polyLR) polyLR.enabled = false;
			return;
		}

		EnsurePolygonRenderer();
		if (!polyLR) return;

		if (!viz || viz.points == null || viz.points.Count < 2)
		{
			polyLR.positionCount = 0;
			return;
		}

		polyLR.enabled = true;
		polyLR.positionCount = viz.points.Count;

		Vector2 o = GetOrigin();
		for (int i = 0; i < viz.points.Count; i++)
		{
			Vector2 p = PolarToCartesian(o, viz.points[i]);
			polyLR.SetPosition(i, new Vector3(p.x, p.y, 0f));
		}
	}

	Vector2 GetOrigin()
	{
		Vector2 o = viz.origin ? (Vector2)viz.origin.position : (Vector2)viz.transform.position;
		return o + viz.originOffset;
	}

	static Vector2 PolarToCartesian(Vector2 origin, PolarBezierCurveVisualizer2D.PolarPoint pp)
	{
		float rad = pp.angleDeg * Mathf.Deg2Rad;
		return origin + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * pp.radius;
	}
	static int GetGrabbedIndex(PolarBezierRuntimeDragger2D dragger)
	{
		var f = typeof(PolarBezierRuntimeDragger2D).GetField("grabbedIndex",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		if (f == null) return -1;
		return (int)f.GetValue(dragger);
	}
}
