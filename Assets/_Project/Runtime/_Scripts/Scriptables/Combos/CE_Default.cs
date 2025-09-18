using UnityEngine;

[CreateAssetMenu(fileName = "Default Key Effect (None)", menuName = "Combos/New Default Key Effect (None)", order = 0)]
public class CE_Default : ComboEffect
{
	protected override void Invoke(KeyCode keyCode, Key key, (bool byKey, Key key) trigger) => Debug.LogWarning($"No Key Effect assigned to {keyCode}", key);
}
