#region
using System;
using System.Collections;
using Lumina.Essentials.Attributes;
using MelenitasDev.SoundsGood;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using VInspector;
using Random = System.Random;
#endregion

// game starts with nearly no post process effects, but when Frenzy mode is activated, the effects ramp up to a maximum intensity.
// the desired final intensity is set on the volume itself, rather than in this script/on this object
public class FrenzyManager : MonoBehaviour
{
	[Header("Frenzy"), Tooltip("\"Frenzy\" is a score that builds up. When it reaches the Frenzy Threshold, you enter Frenzy mode, which multiplies score gain and activates post-processing effects.")]
	[SerializeField] int frenzy;
	[SerializeField, ReadOnly] bool frenzied;
	[SerializeField, ReadOnly] float frenzyTime;
	[SerializeField, ReadOnly] float totalFrenzyTime;

	[Tab("Frenzy Settings")]
	[Tooltip("Frenzy threshold to enter a permanent Frenzy mode.")]
	[SerializeField] int frenzyThreshold = 500;
	[Tooltip("Frenzy multiplies XP gain by this amount.")]
	[SerializeField] float frenzyMultiplier = 1.2f;

	[Header("Effect Ramping")]
	[Tooltip(" How much faster the effects ramp up for each point above the Frenzy threshold. " + "E.g., if set to 0.05, then each point above the threshold increases the speed by 5%.")]
	[SerializeField] float speedMultiplier = 0.05f;
	[Tooltip("Base lerp multiplier at the threshold. Lower values make the initial ramp slower.")]
	[SerializeField] float baseLerpMultiplier = 0.15f;

	[Tab("Settings")]
	[SerializeField] Volume volume;
	[SerializeField] Image frenzySlider;

	Bloom bloom;
	ChromaticAberration chromaticAberration;
	LensDistortion lensDistortion;
	ScreenSpaceLensFlare lensFlare;
	DepthOfField depthOfField;
	Vignette vignette;

	Coroutine frenzyCoroutine;

	public static FrenzyManager Instance { get; private set; }

	#region Frenzy
	public int Frenzy
	{
		get => frenzy;
		private set => frenzy = Mathf.Clamp(value, 0, frenzyThreshold);
	}
	
	public bool Frenzied
	{
		get => frenzied;
		private set
		{
			switch (value)
			{
				// start or stop Frenzy
				case true when !frenzied:
					frenzied = true;
					frenzyTime = 0f;

					var sfx = new Sound(SFX.frenzyWoosh);
					sfx.SetOutput(Output.SFX);
					sfx.SetVolume(0.5f);
					sfx.Play();
					break;

				case false when frenzied:
					frenzied = false;
					frenzyTime = 0f;

					if (frenzyCoroutine != null) StopCoroutine(frenzyCoroutine);
					break;
			}
		}
	}

	/// <summary>
	/// Triggers Frenzy mode for the given duration.
	/// </summary>
	/// <param name="duration"> The duration of the Frenzy mode in seconds. </param>
	/// <param name="instant"> If true, the effects will ramp up instantly, otherwise they will ramp up over time, increasingly faster the higher the Frenzy score is above the threshold. </param>
	public void TriggerFrenzy(float duration, bool instant = false) => frenzyCoroutine = StartCoroutine(FrenzyRoutine(duration, instant));

	public void EndFrenzy() => Frenzied = false;

	IEnumerator FrenzyRoutine(float duration, bool instant = false)
	{
		(float speedMultiplier, float baseLerpMultiplier) 
				oldValues = new (speedMultiplier, baseLerpMultiplier);

		Frenzied = true;

		if (instant)
		{
			speedMultiplier = 1;
			baseLerpMultiplier = 1;
		}

		float startFrenzy = Frenzy;
		float elapsed = 0f;
		while (elapsed < duration && Frenzied)
		{
			elapsed += Time.deltaTime;
			Frenzy = Mathf.RoundToInt(Mathf.Lerp(startFrenzy, 0, elapsed / duration));
			frenzyFillAmount = Mathf.Lerp(frenzyFillAmount, 0, Time.deltaTime / duration);
			frenzySlider.fillAmount = frenzyFillAmount;
			AddFrenzy(0); // update score text
			yield return null;
		}
		
		Frenzy = 0;
		frenzyFillAmount = 0;
		frenzySlider.fillAmount = 0;
		AddFrenzy(0); // update score text

		yield return new WaitForSeconds(duration - duration);

		// if the score is above the Frenzy threshold, stay in Frenzy mode
		if (Frenzy >= frenzyThreshold) yield break;

		Frenzied = false;

		speedMultiplier = oldValues.speedMultiplier;
		baseLerpMultiplier = oldValues.baseLerpMultiplier;
	}

	/// <summary> A multiplier for XP gains. </summary>
	public float FrenzyMultiplier => frenzyMultiplier;
	#endregion

	void Awake()
	{
		if (Instance != null && Instance != this) Destroy(this);
		else Instance = this;
	}

	void Start()
	{
		volume.profile.TryGet(out bloom);
		volume.profile.TryGet(out chromaticAberration);
		volume.profile.TryGet(out lensDistortion);
		volume.profile.TryGet(out lensFlare);
		volume.profile.TryGet(out depthOfField);
		volume.profile.TryGet(out vignette);

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

	float frenzyFillAmount;

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha3)) AddFrenzy(25);
		if (Input.GetKeyDown(KeyCode.Alpha4)) AddFrenzy(frenzyThreshold);

		if (Frenzy >= frenzyThreshold)
		{
			TriggerFrenzy(10, true);
		}
		else
		{
			frenzyFillAmount = Mathf.Lerp(frenzyFillAmount, (float)Frenzy / frenzyThreshold, Time.deltaTime * 5f);
			frenzySlider.fillAmount = frenzyFillAmount;
		}
		
		// Slightly increase timescale when in Frenzy, up to a max of 2x at 60 seconds of Frenzy time.
		// Begins scaling after 1 minute to avoid making the game too fast quickly.
		// The check for > 0.9f prevents slowing down time if hit stop is active.
		if (Frenzied && Time.timeScale > 0.9f) Time.timeScale = 1f + Mathf.Clamp01((totalFrenzyTime - 30f) / 60f);

		if (Frenzied)
		{
			frenzyTime += Time.deltaTime;
			totalFrenzyTime += Time.deltaTime;

			float lerpSpeed = Time.deltaTime * (baseLerpMultiplier + Mathf.Max(0, Frenzy - frenzyThreshold) * speedMultiplier);

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
		else
		{
			// Ramp down effects to their default values when Frenzy ends
			float lerpSpeed = Time.deltaTime * 1f;

			#region Bloom
			bloom.threshold.value = Mathf.Lerp(bloom.threshold.value, 0f, lerpSpeed);
			bloom.intensity.value = Mathf.Lerp(bloom.intensity.value, 0f, lerpSpeed);
			bloom.scatter.value = Mathf.Lerp(bloom.scatter.value, 0f, lerpSpeed);

			// Note: idk doesnt work so just keep it on
			//bool bloomComplete = bloom.threshold.value < 0.01f;
			//SetOverrideState(Override.Bloom, bloomComplete);
			#endregion

			#region Chromatic Aberration
			chromaticAberration.intensity.value = Mathf.Lerp(chromaticAberration.intensity.value, 0f, lerpSpeed);
			bool CAComplete = chromaticAberration.intensity.value < 0.01f;
			SetOverrideState(Override.ChromaticAberration, CAComplete);
			#endregion

			#region Lens Distortion
			lensDistortion.intensity.value = Mathf.Lerp(lensDistortion.intensity.value, 0f, lerpSpeed);
			lensDistortion.scale.value = Mathf.Lerp(lensDistortion.scale.value, 1f, lerpSpeed);
			bool LDComplete = lensDistortion.intensity.value < 0.01f;
			SetOverrideState(Override.LensDistortion, LDComplete);
			#endregion

			#region Screen Space Lens Flare
			lensFlare.intensity.value = Mathf.Lerp(lensFlare.intensity.value, 0f, lerpSpeed);
			lensFlare.firstFlareIntensity.value = Mathf.Lerp(lensFlare.firstFlareIntensity.value, 0f, lerpSpeed);
			lensFlare.secondaryFlareIntensity.value = Mathf.Lerp(lensFlare.secondaryFlareIntensity.value, 0f, lerpSpeed);
			lensFlare.warpedFlareIntensity.value = Mathf.Lerp(lensFlare.warpedFlareIntensity.value, 0f, lerpSpeed);
			lensFlare.chromaticAbberationIntensity.value = Mathf.Lerp(lensFlare.chromaticAbberationIntensity.value, 0f, lerpSpeed);
			bool SSLComplete = lensFlare.intensity.value < 0.01f;
			SetOverrideState(Override.ScreenSpaceLensFlare, SSLComplete);
			#endregion

			#region Depth of Field
			depthOfField.mode = new (DepthOfFieldMode.Bokeh);
			depthOfField.focusDistance.value = Mathf.Lerp(depthOfField.focusDistance.value, 0f, lerpSpeed);
			depthOfField.focalLength.value = Mathf.Lerp(depthOfField.focalLength.value, 0f, lerpSpeed);
			depthOfField.aperture.value = Mathf.Lerp(depthOfField.aperture.value, 0f, lerpSpeed);
			bool DOFComplete = depthOfField.focusDistance.value < 0.01f;
			SetOverrideState(Override.DepthOfField, DOFComplete);
			#endregion

			#region Vignette
			vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, 0f, lerpSpeed);
			vignette.smoothness.value = Mathf.Lerp(vignette.smoothness.value, 0f, lerpSpeed);
			bool vignetteComplete = vignette.intensity.value < 0.01f;
			SetOverrideState(Override.Vignette, vignetteComplete);
			#endregion
		}
	}

	/// <summary>
	/// Add frenzy points. If in Frenzy mode, XP is multiplied by the Frenzy multiplier.
	/// </summary>
	/// <param name="points"> The points of frenzy points to add. </param>
	public void AddFrenzy(int points)
	{
		int pointsWithMult = Mathf.CeilToInt(points * FrenzyMultiplier);
		frenzy += Frenzied ? pointsWithMult : points;

		//TODO: very temporary way of doing this
		var scoreText = GameObject.FindWithTag("Canvas").transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
		string text = $"{frenzy}\npts" + (Frenzied ? $"\n({FrenzyMultiplier}x)" : string.Empty) + (Time.timeScale >= 1 ? $" ({Time.timeScale:F1}x speed)" : string.Empty);
		scoreText.text = text;
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
