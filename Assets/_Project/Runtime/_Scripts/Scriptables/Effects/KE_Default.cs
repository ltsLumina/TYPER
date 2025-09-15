using UnityEngine;

[CreateAssetMenu(fileName = "Default Key Effect (None)", menuName = "Combos/New Default Key Effect (None)", order = 0)]
public class KE_Default : KeyEffect
{
	protected override void Invoke(KeyCode keyCode, Key key = null) => Debug.LogWarning($"No Key Effect assigned to {keyCode}", key);
}
