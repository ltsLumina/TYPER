using System;
using System.Collections;
using DG.Tweening;
using JetBrains.Annotations;
using Lumina.Essentials.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VInspector;

public partial class ShardManager : MonoBehaviour
{
	[Tab("Shards")]
	[Min(0), Tooltip("Current number of shards the player has. A shard is a currency used to purchase upgrades and items.")]
	[SerializeField] int shards;
	[Tooltip("The required amount of shards to reach the quota. The quota is the minimum amount of shards required to enter the shop.")]
	[Range(1, 10_000)]
	[SerializeField] int quota = 1000;
	[Tooltip("Multiplier applied to shards earned beyond the quota. For example, if set to 1.25, players earn 25% more shards for each shard earned beyond the quota.")]
	[Range(1, 2.5f)]
	[SerializeField] float excessMultiplier = 1.25f;

	[Space(10)]
	
	[Tab("References")]
	[SerializeField] Image quotaSlider;
	[SerializeField] TMP_Text shardText;
	[SerializeField] TMP_Text quotaText;
	
	[Tab("Settings"), Header("Debug")]
	[Tooltip("Whether to enforce the shard quota. If true, players cannot earn more shards once they reach the quota. (Debug purposes only)")]
	[SerializeField] bool enforceQuota;
	[Tooltip("Whether the player has reached or exceeded the shard quota."), UsedImplicitly]
	[SerializeField, ReadOnly] bool atQuota;
	[Tooltip("Shards needed to reach quota. Negative if over quota."), UsedImplicitly]
	[SerializeField, ReadOnly] int shardsNeededForQuota;

	public static ShardManager Instance { get; private set; }

	void Awake()
	{
		if (Instance != null && Instance != this) Destroy(this);
		else Instance = this;
	}

	void Start()
	{
		Debug.Assert(excessMultiplier >= 1f, "Excess multiplier must be at least 1.");
		
		// Initial UI update
		quotaSlider.fillAmount = QuotaProgress;
		shardText.text = shards.ToString();
		quotaText.text = $"{shardsNeededForQuota} / {quota}";
	}

	void Update()
	{
		if (Keyboard.current.digit5Key.wasPressedThisFrame) AddShards(100);
		if (Keyboard.current.digit6Key.wasPressedThisFrame) SpendShards(100);
		if (Keyboard.current.digit7Key.wasPressedThisFrame) AddShards(500);

		if (QuotaReached && Math.Abs(quotaSlider.fillAmount - 1) < 0.1f) 
			quotaSlider.color = Color.green;
		else quotaSlider.color = new (0.43f, 1f, 0.9f);
	}

	public void AddShards(int amount)
	{
		int bonus = QuotaReached ? Mathf.RoundToInt(amount * (excessMultiplier - 1f)) : 0;
		
		shards += amount + bonus;
		Debug.Log($"Added {amount} shards. Total shards: {shards}");

		shardsNeededForQuota = quota - shards;
		
		quotaSlider.DOFillAmount(QuotaProgress, 1.5f).SetEase(Ease.OutCubic);
		LerpShardText(shardText, shards);
		quotaText.text = $"+{Mathf.Abs(shardsNeededForQuota)} / {quota}"; // show positive if over quota
	}

	public bool SpendShards(int amount)
	{
		if (shards >= amount)
		{
			shards -= amount;
			Debug.Log($"Spent {amount} shards. Remaining shards: {shards}");

			shardsNeededForQuota = quota - shards;

			quotaSlider.DOFillAmount(QuotaProgress, 1.5f).SetEase(Ease.OutCubic);
			LerpShardText(shardText, shards);
			quotaText.text = $"{shardsNeededForQuota} / {quota}";
			return true;
		}

		Debug.LogWarning($"Not enough shards to spend {amount}. Current shards: {shards}");
		return false;
	}

	void LerpShardText(TMP_Text text, int targetValue, float duration = 1.5f)
	{
		int startValue = int.TryParse(text.text, out var val) ? val : 0;

		DOTween.To
		        (() => startValue, x =>
		        {
			        startValue = x;
			        text.text = x.ToString();
		        }, targetValue, duration)
		       .SetEase(Ease.OutCubic);
	}
}

public partial class ShardManager // Properties
{
	public int Shards => shards;
	public int Quota => quota;
	
	public float ExcessMultiplier => excessMultiplier;
	public int ShardsNeededForQuota => shardsNeededForQuota = quota - shards;

	/// <summary> From 0 to 1, representing progress towards the shard quota. </summary>
	public float QuotaProgress => Mathf.Clamp01((float) shards / quota);
	public bool QuotaReached => atQuota = shards >= quota;
}
