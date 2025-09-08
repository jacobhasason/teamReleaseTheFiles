using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using Unity.FPS.UI;
using TMPro;

public class Results : MonoBehaviour
{
    [Header("UI References")]
    public CurrencyManager currencyManager;        // Tracks all currency
    public WaveSpawner waveSpawner;
    public TextMeshProUGUI WavesSurvived;          // UI Text for Waves Survived (TMP)
    public TextMeshProUGUI EnemiesKilled;           // UI Text for Enemies Killed (TMP)
    public TextMeshProUGUI AudienceFavorText;      // UI Text for Audience Favor (TMP)
    public TextMeshProUGUI CorporateFavorText;     // UI Text for Corporate Favor (TMP)
    public TextMeshProUGUI TotalScore;     // UI Text for Corporate Favor (TMP)



    // Start is called before the first frame update
    void Start()
    {
        if (waveSpawner == null) 
            waveSpawner = FindObjectOfType<WaveSpawner>();

        if (currencyManager == null) 
            currencyManager = FindObjectOfType<CurrencyManager>();
  
        if (waveSpawner == null )
        {
            Debug.Log("Cannot Find Wave Spawner!");
        }

        if (currencyManager == null)
            Debug.Log("Cannot Find Currency Manager!");

        int waveCount = waveSpawner.waveCount - 1;

        WavesSurvived.text = $"Waves Survived: {waveCount}";
        EnemiesKilled.text = $"Enemies Killed: {waveSpawner.enemiesKilled}";
        AudienceFavorText.text = $"Audience Favor: {currencyManager.GetCurrency("AudienceFavor")}";
        CorporateFavorText.text = $"Corporate Favor: {currencyManager.GetCurrency("CorporateFavor")}";
        TotalScore.text = $"Total Score: {CalculateTotalScore()}";
    }

    private int CalculateTotalScore()
    {
        return ((waveSpawner.waveCount - 1) * 10) + ((waveSpawner.enemiesKilled) * 10) + currencyManager.GetCurrency("AudienceFavor")
            + currencyManager.GetCurrency("CorporateFavor");
    }
}
