
using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

/// <summary>
/// Describes (but does not actually perform) an attempt to things.
/// This is used to perform the described movement (in Spawn) as well as pass the information to
/// lifecycle hook interface implementers
/// </summary>
public class SpawnInfo
{
	/// <summary>
	/// Type of thing to spawn.
	/// </summary>
	public readonly SpawnableType SpawnableType;

	/// <summary>
	/// Type of spawn being performed. Based on this value, extra information
	/// will be available in this spawn info to describe the details of the spawn,
	/// </summary>
	public readonly SpawnType SpawnType;

	/// <summary>
	/// Prefab to spawn, or prefab override if spawning a cloth.
	/// </summary>
	public readonly GameObject PrefabUsed;

	/// <summary>
	/// GameObject to clone if SpawnableType.Clone
	/// </summary>
	public readonly GameObject ClonedFrom;

	/// <summary>
	/// Cloth data if spawning a cloth. Will be a subclass of BaseClothData.
	/// </summary>
	public readonly BaseClothData ClothData;

	/// <summary>
	/// Clothing variant type to use if spawning a cloth.
	/// </summary>
	public readonly ClothingVariantType ClothingVariantType;

	/// <summary>
	/// Index of clothing variant to use if spawning a cloth.
	/// </summary>
	public readonly int ClothingVariantIndex;

	/// <summary>
	/// World position to spawn at. Defaults to HiddenPos.
	/// </summary>
	public readonly Vector3 WorldPosition;

	/// <summary>
	/// Parent transform to spawn under. This does not usually need to be specified because the RegisterTile
	/// automatically figures out the correct parent.
	/// </summary>
	public readonly Transform Parent;

	/// <summary>
	/// Rotation to spawn with. Defaults to Quaterion.identity.
	/// </summary>
	public readonly Quaternion Rotation;

	/// <summary>
	/// If spawning not hidden, applies this slight amount of random scattering to the spawned objects. Null
	/// if no scattering should be done.
	/// </summary>
	public readonly float? ScatterRadius;

	/// <summary>
	/// Number of instances to spawn.
	/// </summary>
	public readonly int Count;

	/// <summary>
	/// If true, the spawn will be cancelled if the location being spawned into is totally impassable.
	/// </summary>
	public readonly bool CancelIfImpassable;

	/// <summary>
	/// If SpawnType.Player, occupation the player is being spawned with.
	/// </summary>
	public readonly Occupation Occupation;

	/// <summary>
	/// If SpawnType.Player, character settings the player is being spawned with.
	/// </summary>
	/// <returns></returns>
	public readonly CharacterSettings CharacterSettings;

	/// <summary>
	/// If SpawnType.Player, whether character should be spawned naked or populated with their default loadout.
	/// </summary>
	public readonly bool Naked;

	private SpawnInfo(SpawnableType spawnableType, SpawnType spawnType, GameObject prefab, BaseClothData clothData,
		ClothingVariantType clothingVariantType, int clothingVariantIndex, Vector3 worldPosition, Transform parent,
		Quaternion rotation, float? scatterRadius, int count, Occupation occupation, GameObject clonedFrom = null,
		CharacterSettings characterSettings = null, bool naked = false, bool cancelIfImpassable = false)
	{
		SpawnableType = spawnableType;
		SpawnType = spawnType;
		PrefabUsed = prefab;
		ClothData = clothData;
		ClothingVariantType = clothingVariantType;
		ClothingVariantIndex = clothingVariantIndex;
		WorldPosition = worldPosition;
		Parent = parent;
		Rotation = rotation;
		ScatterRadius = scatterRadius;
		Count = count;
		Occupation = occupation;
		ClonedFrom = clonedFrom;
		CharacterSettings = characterSettings;
		Naked = naked;
		CancelIfImpassable = cancelIfImpassable;
	}

	/// <summary>
	/// Spawn a player with the specified occupation
	/// </summary>
	/// <param name="occupation">Occupation details to use to spawn this player</param>
	/// <param name="characterSettings">settings to use for this player</param>
	/// <param name="playerPrefab">Prefab to use to spawn this player</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to the matrix at the indicated spawn position, which
	/// is the correct behavior for most things. </param>
	/// <param name="naked">whether player should spawn naked or with their default loadout</param>
	/// <returns>the newly created GameObject</returns>
	/// <returns></returns>
	public static SpawnInfo Player(Occupation occupation, CharacterSettings characterSettings, GameObject playerPrefab, Vector3? worldPosition = null, Transform parent = null, Quaternion? rotation = null,
		bool naked = false)
	{
		return new SpawnInfo(SpawnableType.Prefab, SpawnType.Player, playerPrefab, null, ClothingVariantType.Default, -1,
			worldPosition.GetValueOrDefault(TransformState.HiddenPos),
			DefaultParent(parent, worldPosition), rotation.GetValueOrDefault(Quaternion.identity), null, 1, occupation, characterSettings: characterSettings, naked: naked);
	}

	/// <summary>
	/// Spawn a ghost with the specified occupation
	/// </summary>
	/// <param name="occupation">Occupation details to use to spawn this ghost</param>
	/// <param name="characterSettings">settings to use for this ghost</param>
	/// <param name="ghostPrefab">Prefab to use to spawn this ghost</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to the matrix at the indicated spawn position, which
	/// is the correct behavior for most things. </param>
	/// <returns>the newly created GameObject</returns>
	/// <returns></returns>
	public static SpawnInfo Ghost(Occupation occupation, CharacterSettings characterSettings, GameObject ghostPrefab, Vector3? worldPosition = null, Transform parent = null, Quaternion? rotation = null)
	{
		return new SpawnInfo(SpawnableType.Prefab, SpawnType.Ghost, ghostPrefab, null, ClothingVariantType.Default, -1,
			worldPosition.GetValueOrDefault(TransformState.HiddenPos),
			DefaultParent(parent, worldPosition), rotation.GetValueOrDefault(Quaternion.identity), null, 1, occupation, characterSettings: characterSettings);
	}

	/// <summary>
	/// Spawn the specified prefab
	/// </summary>
	/// <param name="prefab">Prefab to spawn an instance of. This is intended to be made to work for pretty much any prefab, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which all have unique behavior for how they should spawn. If you are trying
	/// to instantiate something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented necessary lifecycle methods.</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to the matrix at the indicated spawn position, which
	/// is the correct behavior for most things. </param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <param name="cancelIfImpassable">If true, the spawn will be cancelled if the location being spawned into is totally impassable.</param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnInfo Prefab(GameObject prefab, Vector3? worldPosition = null, Transform parent = null, Quaternion? rotation = null, int count = 1, float? scatterRadius = null,
		bool cancelIfImpassable = false)
	{
		return new SpawnInfo(SpawnableType.Prefab, SpawnType.Default, prefab, null, ClothingVariantType.Default, -1,
			worldPosition.GetValueOrDefault(TransformState.HiddenPos),
			DefaultParent(parent, worldPosition), rotation.GetValueOrDefault(Quaternion.identity), scatterRadius, count, null, cancelIfImpassable: cancelIfImpassable);
	}


	/// <summary>
	/// Spawn the prefab with the specified name.
	/// </summary>
	/// <param name="prefabName">Name of prefab to spawn an instance of. This is intended to be made to work for pretty much any prefab, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which all have unique behavior for how they should spawn. If you are trying
	/// to instantiate something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented necessary lifecycle methods.</param>
	/// <param name="position">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to the matrix at the indicated spawn position, which
	/// is the correct behavior for most things. </param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <param name="cancelIfImpassable">If true, the spawn will be cancelled if the location being spawned into is totally impassable.</param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnInfo Prefab(string prefabName, Vector3? position = null, Transform parent = null, Quaternion? rotation = null, int count = 1, float? scatterRadius = null,
		bool cancelIfImpassable = false)
	{
		GameObject prefab = Spawn.GetPrefabByName(prefabName);
		if (prefab == null)
		{
			Logger.LogErrorFormat("Attempted to spawn prefab with name {0} which is either not an actual prefab name or" +
			                      " is a prefab which is not spawnable. Request to spawn will be ignored.", Category.ItemSpawn, prefabName);
			return null;
		}

		return Prefab(prefab, position, parent, rotation, count, scatterRadius, cancelIfImpassable: cancelIfImpassable);
	}

	/// <summary>
	/// Spawns the cloth with the specifeid cloth data
	/// </summary>
	/// <param name="clothData">cloth data describing the cloth, should be a subtype of BaseClothData</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="CVT">variant type to spawn this cloth as, defaults to Default</param>
	/// <param name="variantIndex">variant index to spawn this cloth as, defaults to -1</param>
	/// <param name="prefabOverride">prefab to use instead of this cloth's default</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to the matrix at the indicated spawn position, which
	/// is the correct behavior for most things. </param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <returns></returns>
	public static SpawnInfo Cloth(BaseClothData clothData, Vector3? worldPosition = null,
		ClothingVariantType CVT = ClothingVariantType.Default, int variantIndex = -1, GameObject prefabOverride = null,
		Transform parent = null, Quaternion? rotation = null,
		int count = 1, float? scatterRadius = null)
	{
		return new SpawnInfo(SpawnableType.Cloth, SpawnType.Default, prefabOverride, clothData, CVT, variantIndex,
			worldPosition.GetValueOrDefault(TransformState.HiddenPos),
			DefaultParent(parent, worldPosition), rotation.GetValueOrDefault(Quaternion.identity), scatterRadius, count, null);
	}

	/// <summary>
	/// Clone the item This only works if toClone has a PoolPrefabTracker
	/// attached or its name matches a prefab name, otherwise we don't know what prefab to create.
	/// </summary>
	/// <param name="toClone">GameObject to clone. This only works if toClone has a PoolPrefabTracker
	/// attached or its name matches a prefab name, otherwise we don't know what prefab to create.. Intended to work for any object, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which might need unique behavior for how they should spawn. If you are trying
	/// to clone something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented IOnStageServer or IOnStageClient when IsCloned = true</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to the matrix at the indicated spawn position, which
	/// is the correct behavior for most things. </param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnInfo Clone(GameObject toClone, Vector3? worldPosition = null, Transform parent = null,
		Quaternion? rotation = null)
	{
		return new SpawnInfo(SpawnableType.Clone, SpawnType.Default, null, null, ClothingVariantType.Default, -1,
			worldPosition.GetValueOrDefault(TransformState.HiddenPos),
			DefaultParent(parent, worldPosition), rotation.GetValueOrDefault(Quaternion.identity), null, 1, null, toClone);
	}

	/// <summary>
	/// Special type of spawn, performed on each object mapped in the scene once the scene is done loading.
	/// </summary>
	/// <param name="mappedObject">object which was mapped into the scene.</param>
	/// <returns></returns>
	public static SpawnInfo Mapped(GameObject mappedObject)
	{
		//is it a prefab or cloth?
		var clothing = mappedObject.GetComponent<Clothing>();
		if (clothing == null)
		{
			//assume prefab
			var prefab = Spawn.DeterminePrefab(mappedObject);
			return new SpawnInfo(SpawnableType.Prefab, SpawnType.Mapped, prefab, null, ClothingVariantType.Default, -1,
				mappedObject.WorldPosServer(),
				mappedObject.transform.parent, mappedObject.transform.rotation, null, 1, null);
		}
		else
		{
			//assume cloth
			return new SpawnInfo(SpawnableType.Cloth, SpawnType.Mapped, null, clothing.clothingData, clothing.Type,
				clothing.Variant,
				mappedObject.WorldPosServer(),
				mappedObject.transform.parent, mappedObject.transform.rotation, null, 1, null);
		}
	}

	private static Transform DefaultParent(Transform parent, Vector3? worldPos)
	{
		return parent != null ? parent : MatrixManager.GetDefaultParent(worldPos, true);
	}

	public override string ToString()
	{
		return $"{nameof(SpawnableType)}: {SpawnableType}, {nameof(SpawnType)}: {SpawnType}, {nameof(PrefabUsed)}: " +
		       $"{PrefabUsed}, {nameof(ClothData)}: {ClothData}, {nameof(ClothingVariantType)}: {ClothingVariantType}, " +
		       $"{nameof(ClothingVariantIndex)}: {ClothingVariantIndex}, {nameof(WorldPosition)}: {WorldPosition}, " +
		       $"{nameof(Parent)}: {Parent}, {nameof(Rotation)}: {Rotation}, {nameof(ScatterRadius)}: {ScatterRadius}, " +
		       $"{nameof(Count)}: {Count}, {nameof(Occupation)}: {Occupation}";
	}
}

/// <summary>
/// Type of spawn being performed. This enum helps users to know what data
/// will be available.
/// </summary>
public enum SpawnType
{
	/// <summary>
	/// Normal spawning, no extra data will be in this spawn info
	/// </summary>
	Default = 0,
	/// <summary>
	/// Spawning a player, extra info related to this will be populated.
	/// </summary>
	Player = 1,
	/// <summary>
	/// Object was already mapped into the scene and scene has loaded.
	/// </summary>
	Mapped = 2,
	/// <summary>
	/// Spawning an NPC
	/// </summary>
	Mob = 3,
	/// <summary>
	/// Spawning a ghost
	/// </summary>
	Ghost = 4
}

/// <summary>
/// Type of thing being spawned
/// </summary>
public enum SpawnableType
{
	Prefab = 0,
	Cloth = 1,
	//cloning an existing gameobject
	Clone = 2
}
