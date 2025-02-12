﻿using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Magical dog AI brain for corgis!
/// Used for all corgis, remember to set the name of the
/// dog in inspector.
/// *All logic should be server side.
/// </summary>
public class CorgiAI : MobAI
{
	private string dogName;

	//Set this inspector. The corgi will only respond to
	//voice commands from these job types:
	public List<JobType> allowedToGiveCommands = new List<JobType>();

	//TODO: later we can make it so capt or hop can tell the dog to
	//respond to commands from others based on their names

	private float timeForNextRandomAction;
	private float timeWaiting;

	protected override void Awake()
	{
		base.Awake();
		dogName = mobName.ToLower();
	}

	protected override void AIStartServer()
	{
		followingStopped.AddListener(OnFollowingStopped);
		exploringStopped.AddListener(OnExploreStopped);
		fleeingStopped.AddListener(OnFleeStopped);
	}

	public override void LocalChatReceived(ChatEvent chatEvent)
	{
		ProcessLocalChat(chatEvent);
		base.LocalChatReceived(chatEvent);
	}

	void ProcessLocalChat(ChatEvent chatEvent)
	{
		var speaker = PlayerList.Instance.Get(chatEvent.speaker, false);

		if (speaker.Script == null) return;
		if (speaker.Script.playerNetworkActions == null) return;

		// Check for an ID card (could not find a better solution)
		IDCard card = null;
		var playerStorage = speaker.Script.ItemStorage;
		var idId = playerStorage.GetNamedItemSlot(NamedSlot.id).ItemObject;
		var handId = playerStorage.GetActiveHandSlot().ItemObject;
		if (idId != null && idId.GetComponent<IDCard>() != null)
		{
			card = idId.GetComponent<IDCard>();
		}
		else if (handId != null &&
		         handId.GetComponent<IDCard>() != null)
		{
			card = handId.GetComponent<IDCard>();
		}

		if (card == null) return;

		bool allowCommands = false;
		foreach (JobType t in allowedToGiveCommands)
		{
			if (t == card.GetJobType) allowCommands = true;
		}

		if (!allowCommands) return;

		StartCoroutine(PerformVoiceCommand(chatEvent.message.ToLower(), speaker));
	}

	IEnumerator PerformVoiceCommand(string msg, ConnectedPlayer speaker)
	{
		//We want these ones to happen right away:
		if (msg.Contains($"{dogName} run") || msg.Contains($"{dogName} get out of here"))
		{
			StartFleeing(speaker.GameObject.transform, 10f);
			yield break;
		}

		if (msg.Contains($"{dogName} stay") || msg.Contains($"{dogName} sit")
		                                    || msg.Contains($"{dogName} stop"))
		{
			ResetBehaviours();
			yield break;
		}

		//Slight delay for the others:
		yield return WaitFor.Seconds(0.5f);

		if (msg.Contains($"{dogName} come") || msg.Contains($"{dogName} follow")
		                                    || msg.Contains($"come {dogName}"))
		{
			if (Random.value > 0.8f)
			{
				yield return StartCoroutine(ChaseTail(1));
			}

			FollowTarget(speaker.GameObject.transform);
			yield break;
		}

		if (msg.Contains($"{dogName} find food") || msg.Contains($"{dogName} explore"))
		{
			if (Random.value > 0.8f)
			{
				yield return StartCoroutine(ChaseTail(2));
			}

			BeginExploring();
			yield break;
		}
	}

	IEnumerator ChaseTail(int times)
	{
		var timesSpun = 0;

		while (timesSpun <= times)
		{
			for (int spriteDir = 1; spriteDir < 5; spriteDir++)
			{
				dirSprites.DoManualChange(spriteDir);
				yield return WaitFor.Seconds(0.3f);
			}

			timesSpun++;
		}

		yield return WaitFor.EndOfFrame;
	}

	//TODO: Do extra stuff on these events, like barking when being told to sit:
	void OnFleeStopped()
	{
	}

	void OnExploreStopped()
	{
	}

	void OnFollowingStopped()
	{
	}

	//Updates only on the server
	protected override void UpdateMe()
	{
		if (health.IsDead || health.IsCrit || health.IsCardiacArrest) return;

		base.UpdateMe();
		MonitorExtras();
	}

	void MonitorExtras()
	{
		//TODO monitor hunger when it is added

		if (IsPerformingTask) return;

		timeWaiting += Time.deltaTime;
		if (timeWaiting > timeForNextRandomAction)
		{
			timeWaiting = 0f;
			timeForNextRandomAction = Random.Range(8f, 30f);

			DoRandomAction(Random.Range(1, 3));
		}
	}

	void DoRandomAction(int randAction)
	{
		switch (randAction)
		{
			case 1:
				StartCoroutine(ChaseTail(Random.Range(1, 5)));
				break;
			case 2:
				NudgeInDir(Random.Range(1, 9));
				break;
			//case 3 is nothing
		}
	}
}