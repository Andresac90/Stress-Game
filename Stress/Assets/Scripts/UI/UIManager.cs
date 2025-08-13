// UIManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public void StartButtonClicked()
    {
        SceneManager.LoadScene("Level1");
    }

    public void QuitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; 
#else
        Application.Quit(); // Quit the app in a build
#endif
    }
}
