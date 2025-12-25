using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
	[Header("UI")]
	public GameObject panel;
	public Button resumeButton;
	public Button exitButton;

	[Header("Behavior")]
	public bool pauseTime = true;

	bool isOpen;

	void Awake()
	{
		if (!panel) panel = gameObject;

		SetOpen(false);

		if (resumeButton) resumeButton.onClick.AddListener(Resume);
		if (exitButton) exitButton.onClick.AddListener(Exit);
	}

	void OnDestroy()
	{
		if (resumeButton) resumeButton.onClick.RemoveListener(Resume);
		if (exitButton) exitButton.onClick.RemoveListener(Exit);
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
			Toggle();
	}

	public void Toggle()
	{
		SetOpen(!isOpen);
	}

	public void Resume()
	{
		SetOpen(false);
	}

	void SetOpen(bool open)
	{
		isOpen = open;
		panel.SetActive(open);

		if (pauseTime)
			Time.timeScale = open ? 0f : 1f;
	}

	public void Exit()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
	}
}
