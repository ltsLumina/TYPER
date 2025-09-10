using UnityEngine;

public partial class Key // Properties
{
	public KeyCode KeyboardLetter
	{
		get => keyboardLetter;
		set => keyboardLetter = value;
	}
	public int Row
	{
		get => row;
		set => row = value;
	}
	public int IndexInRow
	{
		get => indexInRow;
		set => indexInRow = value;
	}
	public int IndexGlobal
	{
		get => indexKey;
		set => indexKey = value;
	}
	public bool OffGlobalCooldown
	{
		get => offGlobalCooldown;
		set => offGlobalCooldown = value;
	}
	public bool Combo
	{
		get => combo;
		set => combo = value;
	}
	public bool Mash
	{
		get => mash;
		set => mash = value;
	}
	public bool Chained
	{
		get => chained;
		set => chained = value;
	}
	public bool Loose
	{
		get => loose;
		set => loose = value;
	}
	public bool Thorned
	{
		get => thorned;
		set => thorned = value;
	}
	public float CooldownTime
	{
		get => remainingCooldown;
		set => remainingCooldown = value;
	}
}
