using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    public void PlayStart()
    {
        SceneManager.LoadScene("GameScene");
    }
}
