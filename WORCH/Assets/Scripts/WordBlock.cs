using TMPro;
using UnityEngine;

public class WordBlock : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI wordText;
    [SerializeField]
    private TextMeshProUGUI descriptionText;

    public void Setup(string word, string description)
    {
        wordText.text = word;
        descriptionText.text = description;
    }
}
