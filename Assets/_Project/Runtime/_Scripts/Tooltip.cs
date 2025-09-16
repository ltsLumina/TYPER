using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text description;
    [Range(0,1)]
    [SerializeField] float opacity = 0.85f;

    void Start()
    {
        SetOpacity(opacity);
    }

    public void SetText(string newTitle, string newDescription)
    {
        title.text = newTitle;
        description.text = newDescription;

        // if description overflows the box, resize the background horizontally
        Vector2 textSize = description.GetPreferredValues(newDescription, 0, 0);
        if (textSize.x > background.rectTransform.sizeDelta.x - 20) // 20 for padding
        {
            Vector2 newSize = background.rectTransform.sizeDelta;
            newSize.x = textSize.x + 200; // 200 for padding
            background.rectTransform.sizeDelta = newSize;
        }
    }
    
    public void SetOpacity(float opacity)
    {
        var color = background.color;
        color.a = opacity;
        background.color = color;
    }
}
