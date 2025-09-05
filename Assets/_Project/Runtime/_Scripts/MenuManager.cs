using UnityEngine;

public class MenuManager : MonoBehaviour
{
	KeyController keyController;
	
	void Start()
	{
		keyController = KeyController.Instance;
		
		// Disable the menu at start. Is shown at the end of the intro sequence
		gameObject.SetActive(false);
	}

	public void Highlight(string str) => keyController.HighlightKeys(str, true, true);
	public void EndHighlight(string str) => keyController.HighlightKeys(str, false, false);
}
