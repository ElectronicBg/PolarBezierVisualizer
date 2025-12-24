using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BezierPointCountSliderUI : MonoBehaviour
{
	[Header("References")]
	public PolarBezierCurveVisualizer2D viz;
	public Slider slider;

	[Header("Optional label")]
	public TMP_Text label;

	[Header("Behavior")]
	public int minPoints = 2;
	public int maxPoints = 12;
	public bool keepEndpointsFixed = true;

	readonly Queue<PolarBezierCurveVisualizer2D.PolarPoint> addedQueue = new();

	void Reset()
	{
		slider = GetComponent<Slider>();
	}

	void Awake()
	{
		if (!slider) slider = GetComponent<Slider>();
		if (!viz) viz = FindFirstObjectByType<PolarBezierCurveVisualizer2D>();

		SetupSlider();
		EnsureMinPointsAndResetQueueIfNeeded();
		SyncSliderFromViz();

		slider.onValueChanged.AddListener(OnSliderChanged);
	}

	void OnDestroy()
	{
		if (slider) slider.onValueChanged.RemoveListener(OnSliderChanged);
	}

	void SetupSlider()
	{
		slider.wholeNumbers = true;
		slider.minValue = minPoints;
		slider.maxValue = maxPoints;
	}

	void SyncSliderFromViz()
	{
		if (!viz || viz.points == null) return;
		slider.SetValueWithoutNotify(Mathf.Clamp(viz.points.Count, minPoints, maxPoints));
		UpdateLabel(viz.points.Count);
	}

	void OnSliderChanged(float v)
	{
		if (!viz) return;
		int target = Mathf.RoundToInt(v);
		target = Mathf.Clamp(target, minPoints, maxPoints);

		SetPointCount(target);
		UpdateLabel(Mathf.Clamp(viz.points.Count, minPoints, maxPoints));
	}

	void UpdateLabel(int count)
	{
		if (label)
			label.text = $"Points: {count}";
	}

	void EnsureMinPointsAndResetQueueIfNeeded()
	{
		if (!viz) return;
		if (viz.points == null) viz.points = new List<PolarBezierCurveVisualizer2D.PolarPoint>();

		if (viz.points.Count < 2)
		{
			viz.points.Clear();
			viz.points.Add(new PolarBezierCurveVisualizer2D.PolarPoint(2f, 180f));
			viz.points.Add(new PolarBezierCurveVisualizer2D.PolarPoint(2f, 0f));
			addedQueue.Clear();
		}
	}

	void SetPointCount(int targetCount)
	{
		EnsureMinPointsAndResetQueueIfNeeded();

		int current = viz.points.Count;
		if (targetCount == current) return;

		if (targetCount > current)
			AddPoints(targetCount - current);
		else
			RemovePointsFIFO(current - targetCount);
	}

	void AddPoints(int howMany)
	{
		var pts = viz.points;

		for (int k = 0; k < howMany; k++)
		{
			int insertIndex = FindBestInsertIndexByLongestGap(pts);
			Vector2 worldMid = GetMidPointWorld(pts, insertIndex);

			Vector2 origin = GetOrigin();
			Vector2 d = worldMid - origin;

			float r = d.magnitude;
			float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

			var newPoint = new PolarBezierCurveVisualizer2D.PolarPoint(r, a);
			pts.Insert(insertIndex, newPoint);

			addedQueue.Enqueue(newPoint);
		}
	}

	void RemovePointsFIFO(int howMany)
	{
		var pts = viz.points;

		for (int k = 0; k < howMany; k++)
		{
			if (pts.Count <= 2) return;

			bool removed = TryRemoveOldestAdded(pts);

			if (removed) continue;

			if (keepEndpointsFixed)
			{
				int idx = pts.Count - 2;
				if (idx <= 0) return;
				pts.RemoveAt(idx);
			}
			else
			{
				pts.RemoveAt(pts.Count - 1);
			}
		}
	}

	bool TryRemoveOldestAdded(List<PolarBezierCurveVisualizer2D.PolarPoint> pts)
	{
		while (addedQueue.Count > 0)
		{
			var candidate = addedQueue.Peek();
			int idx = pts.IndexOf(candidate);

			if (idx < 0)
			{
				addedQueue.Dequeue();
				continue;
			}

			if (keepEndpointsFixed && (idx == 0 || idx == pts.Count - 1))
			{
				addedQueue.Dequeue();
				continue;
			}

			pts.RemoveAt(idx);
			addedQueue.Dequeue();
			return true;
		}

		return false;
	}

	int FindBestInsertIndexByLongestGap(List<PolarBezierCurveVisualizer2D.PolarPoint> pts)
	{
		Vector2 origin = GetOrigin();

		int start = 0;
		int end = pts.Count - 1;

		float bestD2 = -1f;
		int bestInsert = 1;

		for (int i = start; i < end; i++)
		{
			Vector2 a = PolarToCartesian(origin, pts[i]);
			Vector2 b = PolarToCartesian(origin, pts[i + 1]);

			float d2 = (b - a).sqrMagnitude;
			if (d2 > bestD2)
			{
				bestD2 = d2;
				bestInsert = i + 1;
			}
		}

		return Mathf.Clamp(bestInsert, 1, pts.Count);
	}

	Vector2 GetMidPointWorld(List<PolarBezierCurveVisualizer2D.PolarPoint> pts, int insertIndex)
	{
		Vector2 origin = GetOrigin();

		int left = Mathf.Clamp(insertIndex - 1, 0, pts.Count - 1);
		int right = Mathf.Clamp(insertIndex, 0, pts.Count - 1);

		Vector2 a = PolarToCartesian(origin, pts[left]);
		Vector2 b = PolarToCartesian(origin, pts[right]);

		return (a + b) * 0.5f;
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
}
