using UnityEngine;

[CreateAssetMenu(fileName = "Combo Effect", menuName = "Combos/New Combo Effect", order = 0)]
public class ComboEffect : ScriptableObject
{
	public void Invoke(KeyCode key)
	{
		Debug.Log("Combo effect triggered!");
	}

	public void Invoke(Key key)
	{
		Invoke(key.ToKeyCode());
	}
}
