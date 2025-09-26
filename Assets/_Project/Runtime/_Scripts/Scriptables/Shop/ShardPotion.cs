using UnityEngine;

[CreateAssetMenu(fileName = "Shard Potion", menuName = "Shop/Shard Potion")]
public class ShardPotion : PotionItem
{
	[Tooltip("Amount of shards the potion gives when purchased.")]
	[Range(100, 1000)]
	public int shardAmount;

	void OnEnable()
	{
		itemName = "Shard Potion";
		description = $"Grants {shardAmount} Shards. (Allows you to enter the shop sooner, since you have more shards, but you will have a net loss of shards overall.)";
	}

	public override void UsePotion() => ShardManager.Instance.AddShards(shardAmount);
}
