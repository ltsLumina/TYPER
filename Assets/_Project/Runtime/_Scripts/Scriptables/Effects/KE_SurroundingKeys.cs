using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Surrounding Keys", menuName = "Combos/New Surrounding Keys", order = 2)]
public class KE_SurroundingKeys : KeyEffect
{
	protected override void Invoke(KeyCode keyCode, Key key, bool triggeredByKey)
	{
		if (!key) keyCode.ToKey().StartCoroutine(InvokeWithDelay(keyCode, keyCode.ToKey()));
		else key.StartCoroutine(InvokeWithDelay(keyCode, key));
	}

	IEnumerator InvokeWithDelay(KeyCode keyCode, Key key)
	{
		GameManager.Instance.TriggerHitStop(0.1f, 0.1f);

		yield return new WaitForSecondsRealtime(0.1f);

		var comboVFX = Resources.Load<ParticleSystem>("PREFABS/Combo VFX");
		ObjectPool comboPool = ObjectPoolManager.FindObjectPool(comboVFX.gameObject);

		List<Key> surroundingKeys = KeyManager.Instance.GetSurroundingKeys(keyCode);

		foreach (Key k in surroundingKeys)
		{
			var vfx = comboPool.GetPooledObject<ParticleSystem>(true, k.transform.position);
			ParticleSystem.MainModule main = vfx.main;
			main.startColor = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
			k.Activate(true, 0.5f, key);
			yield return new WaitForSecondsRealtime(0.02f);
		}
	}
}
