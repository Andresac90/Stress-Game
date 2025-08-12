// UIManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;

//Simple UI bridge for menu buttons.
public class UIManager : MonoBehaviour
{
    public void StartGameButtonClicked()
    {
        SceneManager.LoadScene("Test_PlayerMovement");
    }
}