using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuNavigation : MonoBehaviour
{
    // Data to be passed in
    public static int AudienceCurrency;
    public static int CorporateCurrency;
    public static int waveCount;


    void Start()
    {
      
        // Show cursor so the player can click
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("Started");

    }   

 /*   // These methods are all parameterless, so they appear in the inspector
    public void OnOption0() {
        Debug.Log("Clicks");
        OnOptionSelected(0); 
    }

    public void OnOption1() { 
        OnOptionSelected(1); 
    }

    public void OnOption2() { 
        OnOptionSelected(2); 
    }
    public void OnSkipButton() {
        Debug.Log("Clicks");
        OnSkip(); 
    }
*/
    // Core logic
    public void OnOptionSelected(int index)
    {
        Debug.Log("Option " + index + " selected!");
        ReturnToGame();
    }

    public void OnSkip()
    {
        Debug.Log("Skipped reward!");
        ReturnToGame();
    }

    void ReturnToGame()
    {
        
        // Hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Unload the reward menu scene and resume gameplay
        SceneManager.UnloadSceneAsync("LoadoutMenu");
    }
}
