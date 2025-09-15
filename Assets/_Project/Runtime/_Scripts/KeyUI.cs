using UnityEngine.EventSystems;

public partial class Key : IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler // IPointer events
{
	public void OnPointerEnter(PointerEventData eventData)
	{
		//Logger.Log($"Key {this} hovered.");
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		//Logger.Log($"Key {this} unhovered.");
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		var currentObject = eventData.pointerCurrentRaycast.gameObject;
		var currentKey = currentObject?.GetComponent<Key>();
		
		switch (eventData.button)
		{
			case PointerEventData.InputButton.Left:
				//Logger.Log($"Key {this} clicked.");
				currentKey?.Activate();
				break;

			case PointerEventData.InputButton.Right:
				//Logger.Log($"Key {this} right-clicked.");
				// Right-click functionality can be added here if needed
				break;
		}
	}
}
