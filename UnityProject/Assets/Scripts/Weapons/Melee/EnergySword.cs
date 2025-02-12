using System.Collections.Generic;
using Light2D;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Pickupable))]
public class EnergySword: NetworkBehaviour, ICheckedInteractable<HandActivate>,
	ICheckedInteractable<InventoryApply>
{
	public ItemAttributes itemAttributes;
	public SpriteHandler spriteHandler;
	public PlayerLightControl playerLightControl;
	public LightSprite worldLight;
	public GameObject worldRenderer;

	[SyncVar(hook = nameof(SyncColor))]
	public int color;

	[Range(0, 100)]
	public float activatedHitDamage = 30;
	private float originalHitDamage;

	[Range(0, 100)]
	public float activatedThrowDamage = 20;
	private float originalThrowDamage;

	public List<string> activatedVerbs = new List<string>();
	private List<string> originalVerbs = new List<string>();

	public ItemSize activatedSize = ItemSize.Huge;
	private ItemSize originalSize;

	public string activatedHitSound = "blade1";
	private string originalHitSound;

	public float activatedLightIntensity = 1;

	[SyncVar(hook = nameof(UpdateState))]
	public bool activated;

	private Pickupable pickupable;

	public void Awake()
	{
		pickupable = GetComponent<Pickupable>();
		if (color == (int)SwordColor.Random)
		{
			color = Random.Range(1, 5);
		}

		worldRenderer.SetActive(false);
	}

	public override void OnStartClient()
	{
		SyncColor(color);
		base.OnStartClient();
	}

	public override void OnStartServer()
	{
		SyncColor(color);
		base.OnStartServer();
	}

	private void SyncColor(int c)
	{
		color = c;

		var lightColor = Color.white;

		switch ((SwordColor)color)
		{
			case SwordColor.Red:
				lightColor = new Color32(250, 130, 130, 255); // LIGHT_COLOR_RED
				break;
			case SwordColor.Blue:
				lightColor = new Color32(64, 206, 255, 255); // LIGHT_COLOR_LIGHT_CYAN
				break;
			case SwordColor.Green:
				lightColor = new Color32(100, 200, 100, 255); // LIGHT_COLOR_GREEN
				break;
			case SwordColor.Purple:
				lightColor = new Color32(155, 81, 255, 255); // LIGHT_COLOR_LAVENDER
				break;
		}

		playerLightControl.Colour = lightColor;
		playerLightControl.PlayerLightData.Colour = lightColor;
		worldLight.Color = lightColor;
		pickupable.RefreshUISlotImage();
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		return true;
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}


		if (interaction.TargetObject != gameObject
		    || !Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver))
		{
			return false;
		}

		//only works if screwdriver is in hand
		if (!interaction.IsFromHandSlot) return false;

		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		ToggleState(interaction.Performer.WorldPosServer());
		PlayerAppearanceMessage.SendToAll(interaction.Performer, (int)interaction.HandSlot.NamedSlot.GetValueOrDefault(NamedSlot.none), gameObject);
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		if (activated)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "You can't adjust the sword while it's on!");
			return;
		}

		if (color == (int)SwordColor.Rainbow)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "It's already fabulous!");
			return;
		}

		if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver))
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "You adjust the crystalline beam emitter...");
			var c = color + 1;
			if (c >= (int)SwordColor.Rainbow)
			{
				c = (int)SwordColor.Red;
			}

			SyncColor(c);
		}
		else if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Multitool))
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "RNBW_ENGAGE");
			SyncColor((int)SwordColor.Rainbow);
		}
	}

	public void ToggleState(Vector3 position)
	{
		UpdateState(!activated);

		SoundManager.PlayNetworkedAtPos(activated ? "saberon" : "saberoff", position, 1f);
	}

	private void UpdateState(bool newState)
	{
		activated = newState;

		UpdateSprite();
		UpdateValues();
		UpdateLight();
		pickupable.RefreshUISlotImage();
	}

	private void UpdateSprite()
	{
		if (activated)
		{
			spriteHandler.ChangeSprite(12 + color);
			spriteHandler.Infos.SetVariant(color);
		}
		else
		{
			spriteHandler.ChangeSprite(12);
			spriteHandler.Infos.SetVariant(0);
		}
		pickupable.RefreshUISlotImage();
	}

	private void UpdateValues()
	{
		if (originalVerbs.Count == 0)
		{ // Get the initial values before we replace
			originalHitDamage = itemAttributes.hitDamage;
			originalThrowDamage = itemAttributes.throwDamage;
			originalVerbs = itemAttributes.attackVerb;
			originalSize = itemAttributes.size;
			originalHitSound = itemAttributes.hitSound;
		}

		if (activated)
		{
			itemAttributes.hitDamage = activatedHitDamage;
			itemAttributes.throwDamage = activatedThrowDamage;
			itemAttributes.attackVerb = activatedVerbs;
			itemAttributes.size = activatedSize;
			itemAttributes.hitSound = activatedHitSound;
		}
		else
		{
			itemAttributes.hitDamage = originalHitDamage;
			itemAttributes.throwDamage = originalThrowDamage;
			itemAttributes.attackVerb = originalVerbs;
			itemAttributes.size = originalSize;
			itemAttributes.hitSound = originalHitSound;
		}
	}

	private void UpdateLight()
	{
		playerLightControl.Toggle(activated, activated ? activatedLightIntensity : 0);
		worldRenderer.SetActive(activated);
	}

	public enum SwordColor
	{
		Random = 0,
		Red = 1,
		Blue = 2,
		Green = 3,
		Purple = 4,
		Rainbow = 5
	}
}