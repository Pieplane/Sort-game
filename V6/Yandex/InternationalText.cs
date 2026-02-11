using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InternationalText : MonoBehaviour
{
    [SerializeField] string _ru;
    [SerializeField] string _en;
    [SerializeField] string _fr;
    [SerializeField] string _de;
    

    private void Start()
    {
        string lang = Language.Instance?.CurrentLanguage ?? "en";
        //string lang = "fr";
        switch (lang)
        {
            case "ru":
                GetComponent<TextMeshProUGUI>().text = _ru;
                break;
            case "en":
                GetComponent<TextMeshProUGUI>().text = _en;
                break;
            case "fr":
                GetComponent<TextMeshProUGUI>().text = _fr;
                break;
            case "de":
                GetComponent<TextMeshProUGUI>().text = _de;
                break;
            default:
                GetComponent<TextMeshProUGUI>().text = _en;
                break;

        }
    }
}
