using UnityEngine;
using UnityEngine.SceneManagement;

public class Manager : MonoBehaviour
{

    public void ToGame() => SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);

    public void QuitGame() => Application.Quit();
}
