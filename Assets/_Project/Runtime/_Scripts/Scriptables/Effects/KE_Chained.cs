using System;
using DG.Tweening;
using MelenitasDev.SoundsGood;
using UnityEngine;

[CreateAssetMenu(fileName = "Chained", menuName = "Combos/New Chained", order = 4)]
public class KE_Chained : KeyEffect
{
	protected override void Invoke(KeyCode keyCode, Key key, (bool byKey, Key key) trigger)
	{
		if (trigger.byKey)
		{
			key.RemoveEffect(Key.Effects.Chained);
			key.Enable();

			var chainedSFX = new Sound(SFX.unchained);
			chainedSFX.Play();

			// This animation is likely placeholder, to be replaced with an animation of breaking chains in the future.
			#region Falling off animation
			Vector3 originalPos = key.transform.position;

			GameObject marker = key.ChainedMarker;
			var rb = GetOrAddComponent<Rigidbody2D>(marker);
			rb.bodyType = RigidbodyType2D.Dynamic;
			const float FORCE = 1.5f;
			rb.AddForce(new Vector3(1f, 3f) * FORCE, ForceMode2D.Impulse);
			rb.AddTorque(5, ForceMode2D.Impulse);

			// Falling off animation
			marker.transform.DOScale(Vector3.zero, 1.5f)
			      .SetDelay(1f)
			      .SetEase(Ease.InBack)
			      .OnComplete
			       (() =>
			       {
				       marker.gameObject.SetActive(false);
				       marker.transform.position = originalPos;
				       marker.transform.rotation = Quaternion.identity;
				       marker.transform.localScale = Vector3.one;
				       Destroy(rb);
			       });
			#endregion
		}
		else
		{
			if (DOTween.IsTweening("Chained")) return; // Prevents re-triggering while the animation is playing.
			key.transform.DOPunchPosition(new (0.1f, 0f, 0f), 0.2f, 20).SetId("Chained");

			var chainedSFX = new Sound(SFX.chained);
			chainedSFX.Play();
		}
	}
	
	// get or add component
	static T GetOrAddComponent<T>(GameObject obj) where T : Component
	{
		var component = obj.GetComponent<T>();
		if (component == null)
		{
			component = obj.AddComponent<T>();
		}
		return component;
	}
}
