using UnityEngine;

[CreateAssetMenu(fileName = "Wave", menuName = "Combos/New Wave", order = 3)]
public class KE_Wave : KeyEffect
{
	protected override void Invoke(KeyCode keyCode, Key key, bool triggeredByKey) { KeyManager.Instance.Wave(1, 1, 30f, 0.25f); }
}
