using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private GameObject aboutPanel;
    [SerializeField] private GameObject gameplayPanel;
    [Header("Optional settings")]
    [SerializeField] private Slider volumeSlider;

    private void Start()
    {
        Show(mainPanel);
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(AudioListener.volume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        GameManager.Instance?.SetPaused(true);
    }

    public void ShowMain() => Show(mainPanel);
    public void ShowDifficulty() => Show(difficultyPanel);
    public void ShowSettings() => Show(settingsPanel);
    public void ShowHelp() => Show(helpPanel);
    public void ShowAbout() => Show(aboutPanel);

    public void StartEasy() => StartGame(0.5f);
    public void StartNormal() => StartGame(0.7f);
    public void StartHard() => StartGame(1f);

    private void StartGame(float multiplier)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("Add GameManager to the gameplay scene before starting.");
            return;
        }
        GameManager.Instance.SetDifficulty(multiplier);
        Show(gameplayPanel);
        GameManager.Instance.SetPaused(true);
    }

    public void SetVolume(float value) => AudioListener.volume = Mathf.Clamp01(value);

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void Show(GameObject panel)
    {
        GameObject[] panels = { mainPanel, difficultyPanel, settingsPanel, helpPanel, aboutPanel, gameplayPanel };
        foreach (GameObject item in panels)
            if (item != null) item.SetActive(item == panel);
    }
}
