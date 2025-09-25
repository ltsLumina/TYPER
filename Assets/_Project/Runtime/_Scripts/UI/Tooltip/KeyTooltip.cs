using UnityEngine;

public class KeyTooltip : Tooltip // derivative of MonoBehaviour
{
    void Start() => SetOpacity(opacity);

    public override void SetText(string newTitle, string newDescription)
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