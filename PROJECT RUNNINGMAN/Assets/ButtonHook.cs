// Attached to the button itself
using UnityEngine;


public class ButtonHook : MonoBehaviour
{
    public MenuNavigation menuManager; // drag the main menu manager here

    public void OnOption0() => menuManager.OnOptionSelected(0);
    public void OnOption1() => menuManager.OnOptionSelected(1);
    public void OnOption2() => menuManager.OnOptionSelected(2);
    public void OnSkip()
    {
        Debug.Log("Skipped!");
        menuManager.OnSkip();
    }
}
