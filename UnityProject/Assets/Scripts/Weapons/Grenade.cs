using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Light2D;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

/// <summary>
///     shape of explosion that occurs
/// </summary>
public enum ExplosionType
{
	Square, // radius is equal in all directions from center []

	Diamond, // classic SS13 diagonals are reduced and angled <>
	Bomberman, // plus +
	Circle, // Diamond without tip
}

/// <summary>
///     Generic grenade base.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class Grenade : NetworkBehaviour, IInteractable<HandActivate>, IClientSpawn
{
	[TooltipAttribute("If the fuse is precise or has a degree of error equal to fuselength / 4")]
	public bool unstableFuse = false;
	[TooltipAttribute("If explosion radius has a degree of error equal to radius / 4")]
	public bool unstableRadius = false;
	[TooltipAttribute("Explosion Damage")]
	public int damage = 150;
	[TooltipAttribute("Explosion Radius in tiles")]
	public float radius = 4f;
	[TooltipAttribute("Shape of the explosion")]
	public ExplosionType explosionType;
	[TooltipAttribute("fuse timer in seconds")]
	public float fuseLength = 3;
	[TooltipAttribute("Distance multiplied from explosion that will still shake = shakeDistance * radius")]
	public float shakeDistance = 8;
	[TooltipAttribute("generally neccesary for smaller explosions = 1 - ((distance + distance) / ((radius + radius) + minDamage))")]
	public int minDamage = 2;
	[TooltipAttribute("Maximum duration grenade effects are visible depending on distance from center")]
	public float maxEffectDuration = .25f;
	[TooltipAttribute("Minimum duration grenade effects are visible depending on distance from center")]
	public float minEffectDuration = .05f;

	[Tooltip("Used for animation")]
	public SpriteHandler spriteHandler;
	// Zero and one reserved for hands
	private const int LOCKED_SPRITE = 2;
	private const int ARMED_SPRITE = 3;

	//LayerMask for obstructions which can block the explosion
	private int OBSTACLE_MASK;
	//arrays containing the list of things damaged by the explosion.
	private readonly List<LivingHealthBehaviour> damagedLivingThings = new List<LivingHealthBehaviour>();
	private readonly List<Integrity> damagedObjects = new List<Integrity>();

	//whether this object has exploded
	private bool hasExploded;
	//this object's registerObject
	[SyncVar(hook = nameof(UpdateTimer))]
	private bool timerRunning = false;
	private RegisterItem registerItem;

	private ObjectBehaviour objectBehaviour;
	private TileChangeManager tileChangeManager;

	private void Start()
	{
		OBSTACLE_MASK = LayerMask.GetMask("Walls", "Door Closed");

		registerItem = GetComponent<RegisterItem>();
		objectBehaviour = GetComponent<ObjectBehaviour>();
		tileChangeManager = GetComponentInParent<TileChangeManager>();

		// Set grenade to locked state by default
		UpdateSprite(LOCKED_SPRITE);
	}

	public void OnSpawnClient(ClientSpawnInfo info)
	{
		// Set grenade to locked state by default
		UpdateSprite(LOCKED_SPRITE);
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		StartCoroutine(TimeExplode(interaction.Performer));
	}

	private IEnumerator TimeExplode(GameObject originator)
	{
		if (!timerRunning)
		{
			timerRunning = true;
			PlayPinSFX(originator.transform.position);
			if (unstableFuse)
			{
				float fuseVariation = fuseLength / 4;
				fuseLength = Random.Range(fuseLength - fuseVariation, fuseLength + fuseVariation);
			}
			if (unstableRadius)
			{
				float radiusVariation = radius / 4;
				radius = Random.Range(radius - radiusVariation, radius + radiusVariation);
			}
			yield return WaitFor.Seconds(fuseLength);
			Explode("explosion");
		}
	}

	private void UpdateSprite(int sprite)
	{
		// Update sprite in game
		spriteHandler?.ChangeSprite(sprite);
	}

	/// <summary>
	/// This coroutines make sure that sprite in hands is animated
	/// TODO: replace this with more general aproach for animated icons
	/// </summary>
	/// <returns></returns>
	private IEnumerator AnimateSpriteInHands()
	{
		while (timerRunning && !hasExploded)
		{
			if (UIManager.Hands.CurrentSlot != null)
			{
				// UIManager doesn't update held item sprites automatically
				if (UIManager.Hands.CurrentSlot.Item == gameObject)
				{
					UIManager.Hands.CurrentSlot.UpdateImage(gameObject);
				}
			}

			yield return null;
		}

	}

	public void Explode(string damagedBy)
	{
		if (hasExploded)
		{
			return;
		}
		hasExploded = true;
		if (isServer)
		{
			PlaySoundAndShake();
			CreateShape();
			CalcAndApplyExplosionDamage(damagedBy);
			Despawn.ServerSingle(gameObject);
		}
	}

	/// <summary>
	/// Calculate and apply the damage that should be caused by the explosion, updating the server's state for the damaged
	/// objects. Currently always uses a circle
	/// </summary>
	/// <param name="thanksTo">string of the entity that caused the explosion</param>
	[Server]
	public void CalcAndApplyExplosionDamage(string thanksTo)
	{
		Vector2 explosionPos = objectBehaviour.AssumedWorldPositionServer().To2Int();
		//trigger a hotspot caused by grenade explosion
		registerItem.Matrix.ReactionManager.ExposeHotspotWorldPosition(explosionPos.To2Int(), 3200, 0.005f);

		//apply damage to each damaged thing
		foreach (var damagedObject in damagedObjects)
		{
			int calculatedDamage = CalculateDamage(damagedObject.gameObject.TileWorldPosition(), explosionPos.To2Int());
			if (calculatedDamage <= 0) continue;
			damagedObject.ApplyDamage(calculatedDamage, AttackType.Bomb, DamageType.Burn);
		}
		foreach (var damagedLiving in damagedLivingThings)
		{
			int calculatedDamage = CalculateDamage(damagedLiving.gameObject.TileWorldPosition(), explosionPos.To2Int());
			if (calculatedDamage <= 0) continue;
			damagedLiving.ApplyDamage(gameObject, calculatedDamage, AttackType.Bomb, DamageType.Burn);
		}
	}

	private int CalculateDamage(Vector2Int damagePos, Vector2Int explosionPos)
	{
		float distance = Vector2.Distance(explosionPos, damagePos);
		float effect = 1 - ((distance + distance) / ((radius + radius) + minDamage));
		return (int)(damage * effect);
	}

	private bool IsPastWall(Vector2 pos, Vector2 damageablePos, float distance)
	{
		return Physics2D.Raycast(pos, damageablePos - pos, distance, OBSTACLE_MASK).collider == null;
	}

	/// <summary>
	/// Plays explosion sound and shakes ground
	/// </summary>
	private void PlaySoundAndShake()
	{
		byte shakeIntensity = (byte)Mathf.Clamp( damage/5, byte.MinValue, byte.MaxValue);
		ExplosionUtils.PlaySoundAndShake(objectBehaviour.AssumedWorldPositionServer().RoundToInt(), shakeIntensity, (int) shakeDistance);
	}



	/// <summary>
	/// Set the tiles to show fire effect in the pattern that was chosen
	/// This could be used in the future to set it as chemical reactions in a location instead.
	/// </summary>
	private void CreateShape()
	{
		int radiusInteger = (int)radius;
		Vector3Int pos = Vector3Int.RoundToInt(objectBehaviour.AssumedWorldPositionServer());
		if (explosionType == ExplosionType.Square)
		{
			for (int i = -radiusInteger; i <= radiusInteger; i++)
			{
				for (int j = -radiusInteger; j <= radiusInteger; j++)
				{
					Vector3Int checkPos = new Vector3Int(pos.x + i, pos.y + j, 0);
					if (IsPastWall(pos.To2Int(), checkPos.To2Int(), Mathf.Abs(i) + Mathf.Abs(j)))
					{
						CheckDamagedThings(checkPos.To2Int());
						checkPos.x -= 1;
						checkPos.y -= 1;
						StartCoroutine(TimedEffect(checkPos, TileType.Effects, "Fire", DistanceFromCenter(i,j, minEffectDuration, maxEffectDuration)));
					}
				}
			}
		}
		if (explosionType == ExplosionType.Diamond)
		{
			// F is distance from zero, calculated by radius - x
			// if pos.x/pos.y is within that range it will apply affect that position
			int f;
			for (int i = -radiusInteger; i <= radiusInteger; i++)
			{
				f = radiusInteger - Mathf.Abs(i);
				for (int j = -radiusInteger; j <= radiusInteger; j++)
				{
					if (j <= 0 && j >= (-f) || j >= 0 && j <= (0 + f))
					{
						Vector3Int diamondPos = new Vector3Int(pos.x + i, pos.y + j, 0);
						if (IsPastWall(pos.To2Int(), diamondPos.To2Int(), Mathf.Abs(i) + Mathf.Abs(j)))
						{
							CheckDamagedThings(diamondPos.To2Int());
							diamondPos.x -= 1;
							diamondPos.y -= 1;
							StartCoroutine(TimedEffect(diamondPos, TileType.Effects, "Fire", DistanceFromCenter(i,j, minEffectDuration, maxEffectDuration)));
						}
					}
				}
			}
		}
		if (explosionType == ExplosionType.Bomberman)
		{
			for (int i = -radiusInteger; i <= radiusInteger; i++)
			{
				Vector3Int xPos = new Vector3Int(pos.x + i, pos.y, 0);
				if (IsPastWall(pos.To2Int(), xPos.To2Int(), Mathf.Abs(i)))
				{
					CheckDamagedThings(xPos.To2Int());
					xPos.x -= 1;
					xPos.y -= 1;
					StartCoroutine(TimedEffect(xPos, TileType.Effects, "Fire", DistanceFromCenter(i,0, minEffectDuration, maxEffectDuration)));
				}
			}
			for (int j = -radiusInteger; j <= radiusInteger; j++)
			{
				Vector3Int yPos = new Vector3Int(pos.x, pos.y + j, 0);
				if (IsPastWall(pos.To2Int(), yPos.To2Int(), Mathf.Abs(j)))
				{
					CheckDamagedThings(yPos.To2Int());
					yPos.x -= 1;
					yPos.y -= 1;
					StartCoroutine(TimedEffect(yPos, TileType.Effects, "Fire", DistanceFromCenter(0,j, minEffectDuration, maxEffectDuration)));
				}
			}
		}
		if (explosionType == ExplosionType.Circle)
		{
			// F is distance from zero, calculated by radius - x
			// if pos.x/pos.y is within that range it will apply affect that position
			int f;
			for (int i = -radiusInteger; i <= radiusInteger; i++)
			{
				f = radiusInteger - Mathf.Abs(i) + 1;
				for (int j = -radiusInteger; j <= radiusInteger; j++)
				{
					if (j <= 0 && j >= (-f) || j >= 0 && j <= (0 + f))
					{
						Vector3Int circlePos = new Vector3Int(pos.x + i, pos.y + j, 0);
						if (IsPastWall(pos.To2Int(), circlePos.To2Int(), Mathf.Abs(i) + Mathf.Abs(j)))
						{
							CheckDamagedThings(circlePos.To2Int());
							circlePos.x -= 1;
							circlePos.y -= 1;
							StartCoroutine(TimedEffect(circlePos, TileType.Effects, "Fire", DistanceFromCenter(i,j, minEffectDuration, maxEffectDuration)));
						}
					}
				}
			}
		}
	}

	private void CheckDamagedThings(Vector2 worldPosition)
	{
		//TODO: Does this damage things in lockers?
		damagedLivingThings.AddRange(MatrixManager.GetAt<LivingHealthBehaviour>(worldPosition.RoundToInt(), true)
			//only damage each thing once
			.Distinct());
		damagedObjects.AddRange(MatrixManager.GetAt<Integrity>(worldPosition.RoundToInt(), true)
			//dont damage this grenade
			.Where(i => i.gameObject != gameObject)
			//only damage each thing once
			.Distinct());
	}

	public IEnumerator TimedEffect(Vector3Int position, TileType tileType, string tileName, float time)
	{
		tileChangeManager.UpdateTile(position, TileType.Effects, "Fire");
		yield return WaitFor.Seconds(time);
		tileChangeManager.RemoveTile(position, LayerType.Effects);
	}

	/// <summary>
	/// calculates the distance from the the center using the looping x and y vars
	/// returns a float between the limits
	/// </summary>
	private float DistanceFromCenter(int x, int y, float lowLimit = 0.05f, float highLimit = 0.25f)
	{
		float percentage = (Mathf.Abs(x) + Mathf.Abs(y)) / (radius + radius);
		float reversedPercentage = (1 - percentage) * 100;
		float distance = ((reversedPercentage * (highLimit - lowLimit) / 100) + lowLimit);
		return distance;
	}

	private void PlayPinSFX(Vector3 position)
	{
		SoundManager.PlayNetworkedAtPos("armbomb", position);
	}

	private void UpdateTimer(bool timerRunning)
	{
		this.timerRunning = timerRunning;

		if (timerRunning)
		{
			// Start playing arm animation
			UpdateSprite(ARMED_SPRITE);
			// Update grenade icon in hands
			StartCoroutine(AnimateSpriteInHands());
		}
		else
		{
			// We somehow deactivated bomb
			UpdateSprite(LOCKED_SPRITE);
		}

	}

#if UNITY_EDITOR
	/// <summary>
	/// Used only for debug in editor
	/// </summary>
	[ContextMenu("Pull a pin")]
	private void PullPin()
	{
		StartCoroutine(TimeExplode(gameObject));
	}
#endif
}