using TMPro;
using UnityEngine;

public class ChangeTextOnClick : MonoBehaviour
{
    [SerializeField] string[] text;
    int actual_text;
    int total_texts;

    TextMeshProUGUI m_TextMeshPro;
    private void Start()
    {
        actual_text = text.Length - 1;
        total_texts = text.Length;
        m_TextMeshPro = GetComponent<TextMeshProUGUI>();
    }

    public void NextText()
    {
        actual_text++;
        if (actual_text >= total_texts) actual_text = 0;
        m_TextMeshPro.text = text[actual_text];
    }
}
