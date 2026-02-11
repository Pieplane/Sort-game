using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplierStopButton : MonoBehaviour
{
    [SerializeField] private MultiplierMeter meter;
    [SerializeField] private WinPanel winPanel;

    // сюда можно потом передать BoardManager, чтобы начислить финал
    public System.Action<int, int> OnFinalRewardsReady;

    public void OnStopButton()
    {
        int m = meter.StopAndGetMultiplier();

        var (coinsFinal, xpFinal) = winPanel.ApplyMultiplier(m);

        // сообщаем наружу (BoardManager начислит и/или запустит CoinFly)
        OnFinalRewardsReady?.Invoke(coinsFinal, xpFinal);
    }
}
