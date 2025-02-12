﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PathFinding;
using UnityEngine;
using Mirror;
using UnityEditor;
using Random = UnityEngine.Random;

namespace IngameDebugConsole
{
	/// <summary>
	/// Contains all the custom defined commands for the IngameDebugLogger
	/// </summary>
	public class DebugLogUnitystationCommands : MonoBehaviour
	{
		[ConsoleMethod("suicide", "kill yo' self")]
		public static void RunSuicide()
		{
			bool playerSpawned = (PlayerManager.LocalPlayer != null);
			if (!playerSpawned)
			{
				Logger.LogError("Cannot commit suicide. Player has not spawned.", Category.DebugConsole);

			}
			else
			{
				SuicideMessage.Send(null);
			}
		}

		[ConsoleMethod("damage-self", "Server only cmd.\nUsage:\ndamage-self <bodyPart> <brute amount> <burn amount>\nExample: damage-self LeftArm 40 20.Insert")]
		public static void RunDamageSelf(string bodyPartString, int burnDamage, int bruteDamage)
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.LogError("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			bool success = BodyPartType.TryParse(bodyPartString, true, out BodyPartType bodyPart);
			if (success == false)
			{
				Logger.LogError("Invalid body part '" + bodyPartString + "'", Category.DebugConsole);
				return;
			}

			bool playerSpawned = (PlayerManager.LocalPlayer != null);
			if (playerSpawned == false)
			{
				Logger.LogError("Cannot damage player. Player has not spawned.", Category.DebugConsole);
				return;
			}

			Logger.Log("Debugger inflicting " + burnDamage + " burn damage and " + bruteDamage + " brute damage on " + bodyPart + " of " + PlayerManager.LocalPlayer.name, Category.DebugConsole);
			HealthBodyPartMessage.Send(PlayerManager.LocalPlayer, PlayerManager.LocalPlayer, bodyPart, burnDamage, bruteDamage);
		}

#if UNITY_EDITOR
		[MenuItem("Networking/Restart round")]
#endif
		[ConsoleMethod("restart-round", "restarts the round. Server only cmd.")]
		public static void RunRestartRound()
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.LogError("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			Logger.Log("Triggered round restart from DebugConsole.", Category.DebugConsole);
			GameManager.Instance.RestartRound();
		}

		[ConsoleMethod("call-shuttle", "Calls the escape shuttle. Server only command")]
		public static void CallEscapeShuttle()
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.LogError("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			if (GameManager.Instance.PrimaryEscapeShuttle.Status == ShuttleStatus.DockedCentcom)
			{
				GameManager.Instance.PrimaryEscapeShuttle.CallShuttle(out var result, 40);
				Logger.Log("Called Escape shuttle from DebugConsole: "+result, Category.DebugConsole);
			}
			else
			{
				Logger.Log("Escape shuttle isn't docked at centcom to be called.", Category.DebugConsole);
			}
		}

		[ConsoleMethod("log", "Adjust individual log levels\nUsage:\nloglevel <category> <level> \nExample: loglevel Health 0\n-1 = Off \n0 = Error \n1 = Warning \n2 = Info \n 3 = Trace")]
		public static void SetLogLevel(string logCategory, int level)
		{
			bool catFound = false;
			Category category = Category.Unknown;
			foreach (Category c in Enum.GetValues(typeof(Category)))
			{
				if (c.ToString().ToLower() == logCategory.ToLower())
				{
					catFound = true;
					category = c;
				}
			}

			if (!catFound)
			{
				Logger.Log("Category not found", Category.DebugConsole);
				return;
			}

			LogLevel logLevel = LogLevel.Info;

			if (level > (int)LogLevel.Trace)
			{
				logLevel = LogLevel.Trace;
			}
			else
			{
				logLevel = (LogLevel)level;
			}

			Logger.SetLogLevel(category, logLevel);
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Push everyone up")]
#endif
		private static void PushEveryoneUp()
		{
			foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers)
			{
				player.GameObject.GetComponent<PlayerScript>().PlayerSync.Push(Vector2Int.up);
			}
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Spawn some meat")]
#endif
		private static void SpawnMeat()
		{
			foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers) {
				Vector3 playerPos = player.Script.WorldPos;
				Vector3 spawnPos = playerPos + new Vector3( 0, 2, 0 );
				GameObject mealPrefab = CraftingManager.Meals.FindOutputMeal("Meat Steak");
				var slabs = new List<CustomNetTransform>();
				for ( int i = 0; i < 5; i++ ) {
					slabs.Add( Spawn.ServerPrefab(mealPrefab, spawnPos).GameObject.GetComponent<CustomNetTransform>() );
				}
				for ( var i = 0; i < slabs.Count; i++ ) {
					Vector3 vector3 = i%2 == 0 ? new Vector3(i,-i,0) : new Vector3(-i,i,0);
					slabs[i].ForceDrop( spawnPos + vector3/10 );
				}
			}
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Print player positions")]
#endif
		private static void PrintPlayerPositions()
		{
			//For every player in the connected player list (this list is serverside-only)
			foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers) {
				//Printing this the pretty way, example:
				//Bob (CAPTAIN) is located at (77,0, 52,0, 0,0)
				Logger.LogFormat( "{0} ({1)} is located at {2}.", Category.Server, player.Name, player.Job, player.Script.WorldPos );
			}

		}
#if UNITY_EDITOR
		[MenuItem("Networking/Spawn dummy player")]
#endif
//TODO: Removing dummy spawning capability for now
//		[ConsoleMethod("spawn-dummy", "Spawn dummy player (Server)")]
//		private static void SpawnDummyPlayer() {
//			SpawnHandler.SpawnDummyPlayer( JobType.ASSISTANT );
//		}

#if UNITY_EDITOR
		[MenuItem("Networking/Transform Waltz (Server)")]
		private static void MoveAll()
		{
			CustomNetworkManager.Instance.MoveAll();
		}
#endif

#if UNITY_EDITOR
		[MenuItem("Networking/Gib All (Server)")]
#endif
		[ConsoleMethod("gib-all", "Gib All (Server)")]
		private static void GibAll()
		{
			GibMessage.Send();
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Reset round time")]
#endif
		[ConsoleMethod("reset-time", "Reset round time")]
		private static void ExtendRoundTime()
		{
			GameManager.Instance.ResetRoundTime();
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Kill local player (Server only)")]
#endif
		[ConsoleMethod("suicide", "Kill local player (Server only)")]
		private static void KillLocalPlayer()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				PlayerManager.LocalPlayerScript.playerHealth.ApplyDamage(null, 99999f, AttackType.Internal, DamageType.Brute);
			}
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Respawn local player (Server only)")]
#endif
		[ConsoleMethod("respawn", "Respawn local player (Server only)")]
		private static void RespawnLocalPlayer()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRespawnPlayer();
			}
		}

		private static HashSet<MatrixInfo> usedMatrices = new HashSet<MatrixInfo>();
		private static Tuple<MatrixInfo, Vector3> lastUsedMatrix;
#if UNITY_EDITOR
		[MenuItem("Networking/Crash random matrix into station")]
#endif
		private static void CrashIntoStation()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				StopLastCrashed();

				Vector2 appearPos = new Vector2Int(-50, 37);
				var usedMatricesCount = usedMatrices.Count;

				var matrices = MatrixManager.Instance.MovableMatrices;
				//limit to shuttles if you wish
//					.Where( matrix => matrix.GameObject.name.ToLower().Contains( "shuttle" )
//								   || matrix.GameObject.name.ToLower().Contains( "pod" ) );

				foreach ( var movableMatrix in matrices )
				{
					if ( movableMatrix.GameObject.name.ToLower().Contains( "verylarge" ) )
					{
						continue;
					}

					if ( usedMatrices.Contains( movableMatrix ) )
					{
						continue;
					}

					usedMatrices.Add( movableMatrix );
					lastUsedMatrix = new Tuple<MatrixInfo, Vector3>(movableMatrix, movableMatrix.MatrixMove.State.Position);
					var mm = movableMatrix.MatrixMove;
					mm.SetPosition( appearPos );
					mm.RequiresFuel = false;
					mm.SafetyProtocolsOn = false;
					mm.RotateTo( Orientation.Right );
					mm.SetSpeed( 15 );
					mm.StartMovement();

					break;
				}

				if ( usedMatricesCount == usedMatrices.Count && usedMatricesCount > 0 )
				{ //ran out of unused matrices - doing it again
					usedMatrices.Clear();
					CrashIntoStation();
				}
			}
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Stop last crashed matrix")]
#endif
		private static void StopLastCrashed()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				if ( lastUsedMatrix != null )
				{
					lastUsedMatrix.Item1.MatrixMove.StopMovement();
					lastUsedMatrix.Item1.MatrixMove.SetPosition( lastUsedMatrix.Item2 );
					lastUsedMatrix = null;
				}
			}
		}
		private static GameObject maskPrefab = Resources.Load<GameObject>("Prefabs/Prefabs/Items/Clothing/Resources/BreathMask");
		private static GameObject oxyTankPrefab = Resources.Load<GameObject>("Prefabs/Prefabs/Items/Other/Resources/Emergency Oxygen Tank");
#if UNITY_EDITOR
		[MenuItem("Networking/Make players EVA-ready")]
#endif
		private static void MakeEvaReady()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				foreach ( ConnectedPlayer player in PlayerList.Instance.InGamePlayers )
				{

					var helmet = Spawn.ServerCloth(Spawn.ClothingStoredData["mining hard suit helmet"]).GameObject;
					var suit = Spawn.ServerCloth(Spawn.ClothingStoredData["mining hard suit"]).GameObject;
					var mask = Spawn.ServerPrefab(maskPrefab).GameObject;
					var oxyTank = Spawn.ServerPrefab(oxyTankPrefab).GameObject;

					Inventory.ServerAdd(helmet, player.Script.ItemStorage.GetNamedItemSlot(NamedSlot.head), ReplacementStrategy.Drop);
					Inventory.ServerAdd(suit, player.Script.ItemStorage.GetNamedItemSlot(NamedSlot.exosuit), ReplacementStrategy.Drop);
					Inventory.ServerAdd(mask, player.Script.ItemStorage.GetNamedItemSlot(NamedSlot.mask), ReplacementStrategy.Drop);
					Inventory.ServerAdd(oxyTank, player.Script.ItemStorage.GetNamedItemSlot(NamedSlot.storage01), ReplacementStrategy.Drop);
					player.Script.Equipment.IsInternalsEnabled = true;
				}

			}
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Spawn Rods")]
#endif
		private static void SpawnRods()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				Spawn.ServerPrefab("Rods", PlayerManager.LocalPlayerScript.WorldPos + Vector3Int.up, cancelIfImpassable: true);
			}
		}
#if UNITY_EDITOR
		[MenuItem("Networking/Slip Local Player")]
#endif
		private static void SlipPlayer()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				PlayerManager.LocalPlayerScript.registerTile.Slip( true );
			}
		}
		[ConsoleMethod("spawn-antag", "Spawns a random antag. Server only command")]
		public static void SpawnAntag()
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.LogError("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			Antagonists.AntagManager.Instance.CreateAntag();
		}
		[ConsoleMethod("antag-status", "Shows status of all antag objectives. Server only command")]
		public static void ShowAntagObjectives()
		{
			if (CustomNetworkManager.Instance._isServer == false)
			{
				Logger.LogError("Can only execute command from server.", Category.DebugConsole);
				return;
			}

			Antagonists.AntagManager.Instance.ShowAntagStatusReport();
		}

	}
}