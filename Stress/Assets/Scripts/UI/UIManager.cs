using UnityEngine;

public class UIManager : MonoBehaviour
{
    public void StartGameButtonClicked()
    {
        //Open the game scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("Test_PlayerMovement");
    }
}
