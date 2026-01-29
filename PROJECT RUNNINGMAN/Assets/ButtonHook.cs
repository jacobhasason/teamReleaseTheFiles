// ButtonHook.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ButtonHook : MonoBehaviour
{
    public MenuNavigation menuManager; // drag MenuNavigation here in Inspector

    [Header("UI bits")]
    public TextMeshProUGUI itemText;
    public TextMeshProUGUI costText;
    public Image spriteImage;

    // --- existing click handlers ---
    public void OnOption0() => menuManager.OnOptionSelected(0);
    public void OnOption1() => menuManager.OnOptionSelected(1);
    public void OnOption2() => menuManager.OnOptionSelected(2);
    public void OnSkip()
    {
        Debug.Log("Skipped!");
        menuManager.OnSkip();
    }

    // --- new: let MenuNavigation update visuals here ---
    public void SetData(string name, int cost, Sprite icon)
    {
        if (itemText) itemText.text = name;
        if (costText) costText.text = $"Cost: {cost}";
        if (spriteImage) spriteImage.sprite = icon;
    }

}
