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

	public abstract void SetText(string newTitle, string newDescription);
}
