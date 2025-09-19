using System.Collections;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "Frozen", menuName = "Modifiers/Frozen", order = 5)]
public class KE_Frozen : KeyModifier
{
	public override void OnEffectAdded(Key key) => key.FrozenMarker.SetActive(true);

	public override void OnEffectRemoved(Key key) => key.FrozenMarker.SetActive(false);

	protected override void Invoke(Key key, (bool byKey, Key key) trigger)
	{
		key.StartCoroutine(Freeze());

		//Debug.Log($"Key Effect: {name} applied to {key.name}");

		return;

		IEnumerator Freeze()
		{
			key.FrozenMarker.SetActive(true);
			key.Disable(false);

			if (DOTween.IsTweening("Loose")) DOTween.Pause("Loose");

			yield return new WaitUntil(() => !key.IsFrozen);

			// Immediately re-enable the key to prevent player confusion
			key.Enable(false);

			// fade out frozen marker alpha then reset the alpha to 1
			var spriteRenderer = key.FrozenMarker.GetComponent<SpriteRenderer>();
			var originalAlpha = spriteRenderer.color.a;

			spriteRenderer.DOFade(0f, 0.5f)
			              .OnComplete
			               (() =>
			               {
				               Color color = spriteRenderer.color;
				               color.a = originalAlpha;
				               spriteRenderer.color = color;

				               key.FrozenMarker.SetActive(false);

				               DOTween.Play("Loose");
			               });
		}
	}
}
