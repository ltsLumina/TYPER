using System;
using Lumina.Essentials.Attributes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// game starts with nearly no post process effects, but when frenzy mode is activated, the effects ramp up to a maximum intensity.
// the desired final intensity is set on the volume itself, rather than in this script/on this object
public class FrenzyManager : MonoBehaviour
{
	[SerializeField] int frenzyThreshold = 50;
	[SerializeField, ReadOnly] bool frenzied;
	[SerializeField] Volume volume;
	[Tooltip(" How much faster the effects ramp up for each point above the frenzy threshold. " +
	         "E.g., if set to 0.05, then each point above the threshold increases the speed by 5%.")]
	[SerializeField] float speedMultiplier = 0.05f;
	[Tooltip("Base lerp multiplier at the threshold. Lower values make the initial ramp slower.")]
	[SerializeField] float baseLerpMultiplier = 0.15f;

	Bloom bloom;
	ChromaticAberration chromaticAberration;
	LensDistortion lensDistortion;
	ScreenSpaceLensFlare lensFlare;
	DepthOfField depthOfField;
	Vignette vignette;
	
	public bool Frenzied => frenzied;

	void Start()
	{
		volume.profile.TryGet(out bloom);
		volume.profile.TryGet(out chromaticAberration);
		volume.profile.TryGet(out lensDistortion);
		volume.profile.TryGet(out lensFlare);
		volume.profile.TryGet(out depthOfField);
		volume.profile.TryGet(out vignette);
		
		// disable all effects at start
		#region Bloom
		bloom.threshold.value = 0f;
		bloom.intensity.value = 0f;
		bloom.scatter.value = 0f;
		SetOverrideState(Override.Bloom, false);
		#endregion
		
		#region Chromatic Aberration
		chromaticAberration.intensity.value = 0f;
		SetOverrideState(Override.ChromaticAberration, false);
		#endregion
		
		#region Lens Distortion
		lensDistortion.intensity.value = 0f;
		lensDistortion.scale.value = 1f;
		SetOverrideState(Override.LensDistortion, false);
		#endregion
		
		#region Screen Space Lens Flare
		lensFlare.intensity.value = 0f;
		lensFlare.firstFlareIntensity.value = 0f;
		lensFlare.secondaryFlareIntensity.value = 0f;
		lensFlare.warpedFlareIntensity.value = 0f;
		lensFlare.chromaticAbberationIntensity.value = 0f;
		SetOverrideState(Override.ScreenSpaceLensFlare, false);
		#endregion
		
		#region Depth of Field
		depthOfField.mode = new (DepthOfFieldMode.Bokeh);
		depthOfField.focusDistance.value = 0f;
		depthOfField.focalLength.value = 0f;
		depthOfField.aperture.value = 0f;
		SetOverrideState(Override.DepthOfField, false);
		#endregion
		
		#region Vignette
		vignette.intensity.value = 0f;
		vignette.smoothness.value = 0f;
		SetOverrideState(Override.Vignette, false);
		#endregion
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			GameManager.Instance.AddScore(25);
		}
		
		if (GameManager.Instance.Score >= frenzyThreshold)
		{
			// enter freny
			frenzied = true;
		}

		if (frenzied)
		{
			float lerpSpeed = Time.deltaTime * (baseLerpMultiplier + Mathf.Max(0, GameManager.Instance.Score - frenzyThreshold) * speedMultiplier);
			#region Bloom
			// set the three properties to defualt/zero
			bloom.threshold.value = Mathf.Lerp(bloom.threshold.value, 0.35f, lerpSpeed);
			bloom.intensity.value = Mathf.Lerp(bloom.intensity.value, 0.5f, lerpSpeed);
			bloom.scatter.value = Mathf.Lerp(bloom.scatter.value, 0.75f, lerpSpeed);
			SetOverrideState(Override.Bloom, true);
			#endregion
			
			#region Chromatic Aberration
			chromaticAberration.intensity.value = Mathf.Lerp(chromaticAberration.intensity.value, 1f, lerpSpeed);
			SetOverrideState(Override.ChromaticAberration, true);
			#endregion

			#region Lens Distortion
			lensDistortion.intensity.value = Mathf.Lerp(lensDistortion.intensity.value, -0.5f, lerpSpeed);
			lensDistortion.scale.value = Mathf.Lerp(lensDistortion.scale.value, 0.95f, lerpSpeed);
			SetOverrideState(Override.LensDistortion, true);
			#endregion

			#region Screen Space Lens Flare
			lensFlare.intensity.value = Mathf.Lerp(lensFlare.intensity.value, 0.5f, lerpSpeed);
			lensFlare.firstFlareIntensity.value = Mathf.Lerp(lensFlare.firstFlareIntensity.value, 2f, lerpSpeed);
			lensFlare.secondaryFlareIntensity.value = Mathf.Lerp(lensFlare.secondaryFlareIntensity.value, 2f, lerpSpeed);
			lensFlare.warpedFlareIntensity.value = Mathf.Lerp(lensFlare.warpedFlareIntensity.value, 2f, lerpSpeed);
			lensFlare.chromaticAbberationIntensity.value = Mathf.Lerp(lensFlare.chromaticAbberationIntensity.value, 1f, lerpSpeed);
			SetOverrideState(Override.ScreenSpaceLensFlare, true);
			#endregion

			#region Depth of Field
			depthOfField.mode = new (DepthOfFieldMode.Bokeh);
			depthOfField.focusDistance.value = Mathf.Lerp(depthOfField.focusDistance.value, 3f, lerpSpeed);
			depthOfField.focalLength.value = Mathf.Lerp(depthOfField.focalLength.value, 45f, lerpSpeed);
			depthOfField.aperture.value = Mathf.Lerp(depthOfField.aperture.value, 4f, lerpSpeed);
			SetOverrideState(Override.DepthOfField, true);
			#endregion

			#region Vignette
			vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, 0.4f, lerpSpeed);
			vignette.smoothness.value = Mathf.Lerp(vignette.smoothness.value, 0.2f, lerpSpeed);
			SetOverrideState(Override.Vignette, true);
			#endregion
		}
	}

	enum Override
	{
		None,
		Bloom,
		ChromaticAberration,
		LensDistortion,
		ScreenSpaceLensFlare,
		DepthOfField,
		Vignette
	}
	
	void SetOverrideState(Override overrideState, bool value)
	{
		switch (overrideState)
		{
			case Override.None:
				bloom.threshold.overrideState = value;
				bloom.intensity.overrideState = value;
				bloom.scatter.overrideState = value;
				chromaticAberration.intensity.overrideState = value;
				lensDistortion.intensity.overrideState = value;
				lensFlare.intensity.overrideState = value;
				depthOfField.focusDistance.overrideState = value;
				vignette.intensity.overrideState = value;
				break;
			case Override.Bloom:
				bloom.threshold.overrideState = value;
				bloom.intensity.overrideState = value;
				bloom.scatter.overrideState = value;
				break;
			case Override.ChromaticAberration:
				chromaticAberration.intensity.overrideState = value;
				break;
			case Override.LensDistortion:
				lensDistortion.intensity.overrideState = value;
				break;
			case Override.ScreenSpaceLensFlare:
				lensFlare.intensity.overrideState = value;
				break;
			case Override.DepthOfField:
				depthOfField.focusDistance.overrideState = value;
				break;
			case Override.Vignette:
				vignette.intensity.overrideState = value;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(overrideState), overrideState, null);
		}
	}
}
