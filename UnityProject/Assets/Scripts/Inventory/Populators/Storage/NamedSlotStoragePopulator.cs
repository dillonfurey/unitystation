using UnityEngine;


/// <summary>
/// Populates each named slot using a particular prefab, cloth, or slot populator.
/// </summary>
[CreateAssetMenu(fileName = "NamedSlotStoragePopulator", menuName = "Inventory/Populators/Storage/NamedSlotStoragePopulator", order = 2)]
public class NamedSlotStoragePopulator : ItemStoragePopulator
{
	[SerializeField]
	[Tooltip("What to use to populate each named slot")]
	[ArrayElementTitle("NamedSlot")]
	private NamedSlotPopulatorEntry[] Entries;

	public override void PopulateItemStorage(ItemStorage toPopulate, PopulationContext context)
	{
		Logger.LogTraceFormat("Populating item storage {0}", Category.Inventory, toPopulate.name);
		foreach (var entry in Entries)
		{
			var slot = toPopulate.GetNamedItemSlot(entry.NamedSlot);
			if (slot == null)
			{
				Logger.LogTraceFormat("Skipping populating slot {0} because it doesn't exist in this itemstorage {1}.",
					Category.Inventory, entry.NamedSlot, toPopulate.name);
				continue;
			}

			if (entry.SlotPopulator == null)
			{
				Logger.LogTraceFormat("Skipping populating slot {0} because Slot Populator was empty for this entry.",
					Category.Inventory, entry.NamedSlot);
				continue;
			}

			entry.SlotPopulator.PopulateSlot(slot, context);
		}
	}
}

[System.Serializable]
public class NamedSlotPopulatorEntry
{
	[Tooltip("Named slot being populated. A NamedSlot should not appear" +
	         " more than once in these entries.")]
	public NamedSlot NamedSlot;

	[Tooltip("Populator to use to populate this slot.")]
	public SlotPopulator SlotPopulator;
}

