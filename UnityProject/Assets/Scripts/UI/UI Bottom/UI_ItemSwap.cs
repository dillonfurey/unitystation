﻿using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_ItemSwap : TooltipMonoBehaviour, IPointerClickHandler, IDropHandler,
	IPointerEnterHandler, IPointerExitHandler, IDragHandler, IEndDragHandler
{
	private UI_ItemSlot itemSlot;
	public override string Tooltip => itemSlot.NamedSlot.ToString();

	private Color32 successOverlayColor = new Color32(0, 255, 0, 92);
	private Color32 failOverlayColor = new Color32(255, 0, 0, 92);

	public void OnPointerClick(BaseEventData eventData)
	{
		OnPointerClick((PointerEventData) eventData);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left && !eventData.dragging)
		{
			OnClick();
		}
	}

	public void OnClick()
	{
		SoundManager.Play("Click01");
		//if there is an item in this slot, try interacting.
		if (itemSlot.Item != null)
		{
			itemSlot.TryItemInteract();
		}
		//otherwise, try switching hands to this hand if this is a hand slot and not already active
		else if (itemSlot.ItemSlot.NamedSlot == NamedSlot.leftHand && UIManager.Hands.CurrentSlot != itemSlot)
		{
			UIManager.Hands.SetHand(false);
		}
		else if (itemSlot.ItemSlot.NamedSlot == NamedSlot.rightHand && UIManager.Hands.CurrentSlot != itemSlot)
		{
			UIManager.Hands.SetHand(true);
		}
		else
		{
			//otherwise, try just interacting with the blank slot (which will transfer the item
			itemSlot.TryItemInteract();
		}
	}


	public void OnDrag(PointerEventData data)
	{
		if (data.button == PointerEventData.InputButton.Left && itemSlot.Item != null)
		{
			UIManager.DragAndDrop.UI_ItemDrag(itemSlot);
		}
	}

	private void Awake()
	{
		itemSlot = GetComponentInChildren<UI_ItemSlot>();
	}

	public new void OnPointerEnter(PointerEventData eventData)
	{
		base.OnPointerEnter(eventData);

		var item = UIManager.Hands.CurrentSlot.Item;
		if (item == null
		    || itemSlot.Item != null
		    || itemSlot.NamedSlot == NamedSlot.leftHand
		    || itemSlot.NamedSlot == NamedSlot.rightHand)
		{
			return;
		}

		itemSlot.UpdateImage(item.gameObject,
			Validations.CanPutItemToSlot(PlayerManager.LocalPlayerScript, itemSlot.ItemSlot, item, NetworkSide.Client)
			? successOverlayColor : failOverlayColor);
	}

	public new void OnPointerExit(PointerEventData eventData)
	{
		base.OnPointerExit(eventData);

		itemSlot.UpdateImage(null);
	}

	public void OnDrop(PointerEventData data)
	{
		//something was dropped onto this slot
		if (UIManager.DragAndDrop.FromSlotCache != null && UIManager.DragAndDrop.DraggedItem != null)
		{
			var fromSlot = UIManager.DragAndDrop.DraggedItem.GetComponent<Pickupable>().ItemSlot;

			//if there's an item in the target slot, try inventory apply interaction
			var targetItem = itemSlot.ItemSlot.ItemObject;
			if (targetItem != null)
			{
				var invApply = InventoryApply.ByLocalPlayer(itemSlot.ItemSlot, fromSlot);
				//check interactables in the fromSlot (if it's occupied)
				if (fromSlot.ItemObject != null)
				{
					var fromInteractables = fromSlot.ItemObject.GetComponents<IBaseInteractable<InventoryApply>>()
						.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
					if (InteractionUtils.ClientCheckAndTrigger(fromInteractables, invApply) != null)
					{
						UIManager.DragAndDrop.DropInteracted = true;
						UIManager.DragAndDrop.StopDrag();
						return;
					}

				}

				//check interactables in the target
				var targetInteractables = targetItem.GetComponents<IBaseInteractable<InventoryApply>>()
					.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
				if (InteractionUtils.ClientCheckAndTrigger(targetInteractables, invApply) != null)
				{
					UIManager.DragAndDrop.DropInteracted = true;
					UIManager.DragAndDrop.StopDrag();
					return;
				}
			}
			else
			{
				UIManager.DragAndDrop.DropInteracted = true;
				UIManager.DragAndDrop.StopDrag();
				Inventory.ClientRequestTransfer(fromSlot, itemSlot.ItemSlot);
			}
		}
		UIManager.DragAndDrop.StopDrag();
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		//dragging this slot ended somewhere
		UIManager.DragAndDrop.StopDrag();
	}
}