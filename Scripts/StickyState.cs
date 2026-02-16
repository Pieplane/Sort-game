using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickyState : MonoBehaviour
{
    IEnumerator Start()
    {
        // дождаться PlayerProgress (и чтобы NoAds подтянулся из сохранения)
        while (PlayerProgress.Instance == null)
            yield return null;

        RefreshSticky();
    }

    void RefreshSticky()
    {
        if (PlayerProgress.Instance.NoAds) StickyAds.Hide();
        else StickyAds.Show();
    }
}
