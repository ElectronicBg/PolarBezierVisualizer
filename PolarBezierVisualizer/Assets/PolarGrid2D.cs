using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PolarGrid2D : MonoBehaviour
{
	[Header("Grid Shape")]
	[Min(1)] public int circles = 6;
	[Min(1)] public int radialLines = 12;
	[Min(0.01f)] public float maxRadius = 6f;
	[Min(12)] public int circleSegments = 96;

	[Header("Style")]
	public Color gridColor = new Color(1f, 1f, 1f, 0.08f);
	[Min(0.001f)] public float lineWidth = 0.02f;
	public string sortingLayerName = "Default";
	public int sortingOrder = 0;

	[Header("Optional offset")]
	public Vector2 centerOffset = Vector2.zero;

	[Header("Lifecycle")]
	public bool cleanupOnDisable = true;

	readonly List<LineRenderer> circleLRs = new();
	readonly List<LineRenderer> radialLRs = new();

	[SerializeField] Transform gridRoot;
	const string GridRootName = "__PolarGrid__";

	void OnEnable()
	{
		ValidateFields();
		EnsureRoot();

		CollectExisting();

		Rebuild();
		Draw();
	}

	void OnDisable()
	{
		if (cleanupOnDisable)
			CleanupGenerated();
	}

	void OnValidate()
	{
		ValidateFields();
		EnsureRoot();
		CollectExisting();

		Rebuild();
		Draw();
	}

	void Update()
	{
		Draw();
	}

	void ValidateFields()
	{
		circles = Mathf.Max(1, circles);
		radialLines = Mathf.Max(1, radialLines);
		maxRadius = Mathf.Max(0.01f, maxRadius);
		circleSegments = Mathf.Max(12, circleSegments);
		lineWidth = Mathf.Max(0.001f, lineWidth);
		if (string.IsNullOrEmpty(sortingLayerName)) sortingLayerName = "Default";
	}

	void EnsureRoot()
	{
		if (gridRoot != null) return;

		var existing = transform.Find(GridRootName);
		if (existing != null)
		{
			gridRoot = existing;
			return;
		}

		var go = new GameObject(GridRootName);
		go.hideFlags = HideFlags.DontSaveInBuild;
		go.transform.SetParent(transform, false);
		gridRoot = go.transform;
	}

	void CollectExisting()
	{
		circleLRs.Clear();
		radialLRs.Clear();

		if (gridRoot == null) return;

		for (int i = 0; i < gridRoot.childCount; i++)
		{
			var child = gridRoot.GetChild(i);
			var lr = child.GetComponent<LineRenderer>();
			if (!lr) continue;

			if (child.name.StartsWith("Circle_"))
				circleLRs.Add(lr);
			else if (child.name.StartsWith("Radial_"))
				radialLRs.Add(lr);
		}

		circleLRs.Sort((a, b) => ExtractIndex(a.name).CompareTo(ExtractIndex(b.name)));
		radialLRs.Sort((a, b) => ExtractIndex(a.name).CompareTo(ExtractIndex(b.name)));
	}

	static int ExtractIndex(string name)
	{
		int underscore = name.LastIndexOf('_');
		if (underscore < 0) return 0;
		if (int.TryParse(name.Substring(underscore + 1), out int idx)) return idx;
		return 0;
	}

	void Rebuild()
	{
		EnsureCount(circleLRs, circles, "Circle");
		EnsureCount(radialLRs, radialLines, "Radial");

		CleanupOrphans();
	}

	void EnsureCount(List<LineRenderer> list, int target, string prefix)
	{
		// remove extra
		for (int i = list.Count - 1; i >= target; i--)
		{
			if (list[i] != null)
			{
				var go = list[i].gameObject;
				if (Application.isPlaying) Destroy(go);
				else DestroyImmediate(go);
			}
			list.RemoveAt(i);
		}

		// add missing
		while (list.Count < target)
		{
			var go = new GameObject($"{prefix}_{list.Count}");
			go.transform.SetParent(gridRoot, false);

			var lr = go.AddComponent<LineRenderer>();
			SetupLineRenderer(lr);

			list.Add(lr);
		}

		// refresh style
		for (int i = 0; i < list.Count; i++)
			SetupLineRenderer(list[i]);
	}

	void SetupLineRenderer(LineRenderer lr)
	{
		if (!lr) return;
		lr.useWorldSpace = false;

		if (lr.sharedMaterial == null)
			lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

		lr.startWidth = lr.endWidth = lineWidth;
		lr.startColor = lr.endColor = gridColor;
		lr.numCapVertices = 8;
		lr.numCornerVertices = 8;
		lr.sortingLayerName = sortingLayerName;
		lr.sortingOrder = sortingOrder;
	}

	void CleanupOrphans()
	{
		if (!gridRoot) return;

		var keep = new HashSet<GameObject>();
		for (int i = 0; i < circleLRs.Count; i++) if (circleLRs[i]) keep.Add(circleLRs[i].gameObject);
		for (int i = 0; i < radialLRs.Count; i++) if (radialLRs[i]) keep.Add(radialLRs[i].gameObject);

		for (int i = gridRoot.childCount - 1; i >= 0; i--)
		{
			var ch = gridRoot.GetChild(i).gameObject;
			if (!keep.Contains(ch))
			{
				if (Application.isPlaying) Destroy(ch);
				else DestroyImmediate(ch);
			}
		}
	}

	void CleanupGenerated()
	{
		if (!gridRoot) return;

		for (int i = gridRoot.childCount - 1; i >= 0; i--)
		{
			var ch = gridRoot.GetChild(i).gameObject;
			if (Application.isPlaying) Destroy(ch);
			else DestroyImmediate(ch);
		}

		circleLRs.Clear();
		radialLRs.Clear();
	}

	void Draw()
	{
		if (circleLRs.Count < circles || radialLRs.Count < radialLines) return;

		// circles
		for (int c = 0; c < circles; c++)
		{
			float r = maxRadius * (c + 1) / circles;
			var lr = circleLRs[c];
			if (!lr) continue;

			lr.loop = true;
			lr.positionCount = circleSegments;

			for (int i = 0; i < circleSegments; i++)
			{
				float a = (i / (float)circleSegments) * Mathf.PI * 2f;
				float x = Mathf.Cos(a) * r + centerOffset.x;
				float y = Mathf.Sin(a) * r + centerOffset.y;
				lr.SetPosition(i, new Vector3(x, y, 0f));
			}
		}

		// radial lines
		for (int i = 0; i < radialLines; i++)
		{
			float a = (i / (float)radialLines) * Mathf.PI * 2f;
			Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));

			var lr = radialLRs[i];
			if (!lr) continue;

			lr.loop = false;
			lr.positionCount = 2;
			lr.SetPosition(0, new Vector3(centerOffset.x, centerOffset.y, 0f));
			lr.SetPosition(1, new Vector3(centerOffset.x + dir.x * maxRadius, centerOffset.y + dir.y * maxRadius, 0f));
		}
	}
}
