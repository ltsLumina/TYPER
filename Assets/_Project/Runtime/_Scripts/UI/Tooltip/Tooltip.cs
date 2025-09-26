using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class Tooltip : MonoBehaviour
{
	[SerializeField] protected Image background;
	[SerializeField] protected TMP_Text title;
	[SerializeField] protected TMP_Text description;
	[Range(0, 1)]
	[SerializeField] protected float opacity = 0.85f;

	public virtual void Start() => SetOpacity(opacity);
	
	public virtual void SetText(string newTitle, string newDescription)
	{
		title.text = newTitle;
		description.text = newDescription;
	}

	public virtual void SetOpacity(float opacity)
	{
		var color = background.color;
		color.a = opacity;
		background.color = color;
	}
}
