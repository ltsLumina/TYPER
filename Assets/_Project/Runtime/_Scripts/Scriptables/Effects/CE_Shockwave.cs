using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shockwave", menuName = "Combos/New Shockwave", order = 2)]
public class CE_Shockwave : ComboEffect
{
	protected override void Invoke(KeyCode keyCode, Key key, (bool byKey, Key key) trigger) { key.StartCoroutine(Shockwave(keyCode, key)); }

	IEnumerator Shockwave(KeyCode keyCode, Key key)
	{
		GameManager.Instance.TriggerHitStop(0.1f, 0.1f);

		yield return new WaitForSecondsRealtime(0.1f);

		List<Key> surroundingKeys = KeyManager.Instance.GetSurroundingKeys(keyCode);

		foreach (Key k in surroundingKeys)
		{
			KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
			k.Activate(true, 0.5f, key);
			yield return new WaitForSecondsRealtime(0.02f);
		}
	}
}
