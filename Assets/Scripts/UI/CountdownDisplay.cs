using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CountdownDisplay : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI countdownText;
    [SerializeField] float countdown = 0f;

    private void Start()
    {
        countdownText.text = "00:00";
    }

    private void Update()
    {
        countdown = GameManager.Instance != null ? GameManager.Instance.GetRemainingTime() : 0f;
        int minutes = Mathf.FloorToInt(countdown / 60f);
        int seconds = Mathf.FloorToInt(countdown % 60f);
        countdownText.text = $"{minutes:00}:{seconds:00}";
    }
}
