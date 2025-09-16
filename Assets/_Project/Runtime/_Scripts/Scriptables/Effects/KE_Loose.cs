using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "Loose", menuName = "Combos/New Loose", order = 5)]
public class KE_Loose : KeyEffect
{
	protected override void Invoke(KeyCode keyCode, Key key, (bool byKey, Key key) trigger)
	{
		ActivatedWhileLoose(key, trigger.byKey);
	}

	static void ActivatedWhileLoose(Key key, bool triggeredByKey)
	{
		if (triggeredByKey) return;
		
		DOTween.Kill("Loose"); // Stop the infinite shaking tween.

		key.Disable(false); // Disables the key while falling but doesn't change the colour.
		key.RemoveEffect(Key.Effects.Loose);
		Vector3 originalPos = key.transform.position;

		var rb = key.gameObject.AddComponent<Rigidbody2D>();
		rb.bodyType = RigidbodyType2D.Dynamic;
		const float FORCE = 1.5f;
		rb.AddForce(new Vector3(1f, 3f) * FORCE, ForceMode2D.Impulse);
		rb.AddTorque(5, ForceMode2D.Impulse);

		// Falling off animation
		key.transform.DOScale(Vector3.zero, 1.5f)
		         .SetDelay(1f)
		         .SetEase(Ease.InBack)
		         .OnComplete
		          (() =>
		          {
			          key.Disable();
			          key.SpriteRenderer.gameObject.SetActive(false);
			          key.transform.position = originalPos;
			          key.transform.rotation = Quaternion.identity;
			          key.transform.localScale = Vector3.one;
			          Destroy(rb);
		          });
	}
}
