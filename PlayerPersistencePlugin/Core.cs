using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Common.ObjectBuilders;

using SEModAPIExtensions.API.Plugin;
using SEModAPIExtensions.API.Plugin.Events;

using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.Support;

using VRageMath;
using VRage.Common.Utils;
using System.IO;
using System.Threading;

namespace PlayerPersistencePlugin
{
    public class Core : PluginBase, IPlayerEventHandler, ICharacterEventHandler
	{
		#region "Attributes and Constant"

		const string FILE_PREFIX = "player_";
		const string FILE_SUFFIX = ".sbc";
		const string CONSOLE_PREFIX = "[PPP]: ";

		private SandboxGameAssemblyWrapper m_gameAssemblyWrapper;
		private Dictionary<ulong, CharacterEntity> m_deletedCharacterEntity;
		private Mutex m_mutex;

		#endregion

		#region "Constructors and Initializers"

		public Core()
		{
			m_gameAssemblyWrapper = SandboxGameAssemblyWrapper.Instance;

			m_deletedCharacterEntity = new Dictionary<ulong, CharacterEntity>();
			m_mutex = new Mutex();
		}

		public override void Init()
		{
			Console.WriteLine(CONSOLE_PREFIX + "Player Persistence Plugin '" + Id + "' initialized!");
		}

		#endregion

		#region "Methods"

		#region Player events

		/// <summary>
		/// This method is called each time a player join the server
		/// </summary>
		/// <param name="remoteUserId">Steam ID of the player</param>
		/// <param name="character">Ingame Character of the player</param>
		public void OnPlayerJoined(ulong remoteUserId)
		{
		}

		/// <summary>
		/// This method is called each time a player left the server
		/// </summary>
		/// <param name="remoteUserId">Steam ID of the player</param>
		/// <param name="character">Ingame Character of the player</param>
		public void OnPlayerLeft(ulong remoteUserId)
		{
			try
			{
				if (!m_deletedCharacterEntity.ContainsKey(remoteUserId))
				{
					Console.WriteLine(CONSOLE_PREFIX + " Error! The user " + remoteUserId + " was not found.");
				}
				else
				{
					CharacterEntity player = m_deletedCharacterEntity[remoteUserId];

					Console.WriteLine(CONSOLE_PREFIX + "Player " + remoteUserId + " left the server. Saving player data...");

					//CharacterEntity player = GetPlayerEntity(remoteUserId);			
					SavePlayerInfo(remoteUserId, player);
					m_deletedCharacterEntity.Remove(remoteUserId);
					Console.WriteLine(CONSOLE_PREFIX + "Player " + player.Name + "'s (" + remoteUserId + ") data saved correctly.");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(CONSOLE_PREFIX + "Error: An error occured while trying to save " + remoteUserId + " data.");
				Console.WriteLine(CONSOLE_PREFIX + "Read the server log for more details.");
				LogManager.ErrorLog.WriteLine(CONSOLE_PREFIX + ex);
			}
		}

		#endregion

		/// <summary>
		/// This method is called on a regular base. It is here simply to respect the contract. It is not used in this plugin.
		/// </summary>
		public override void Update()
		{ }

		public override void Shutdown()
		{
		}

		/// <summary>
		/// Get the player entity of the player with the specified steam Id
		/// </summary>
		/// <param name="steamId">Steam Id of the player</param>
		/// <returns></returns>
		private CharacterEntity GetPlayerEntity(ulong steamId)
		{
			foreach (CharacterEntity character in SectorObjectManager.Instance.GetTypedInternalData<CharacterEntity>())
			{
				if (character.SteamId == steamId)
					return character;
			}	

			return null;
		}

		/// <summary>
		/// Get the information of the last session of the player
		/// </summary>
		/// <param name="steamId">Steam Id of the player</param>
		/// <returns>Return the character of the player, or null if no info were found</returns>
		private CharacterEntity LoadPlayerInfo(ulong steamId)
		{
			string worldPath = m_gameAssemblyWrapper.GetServerConfig().LoadWorld;
			worldPath += "\\" + FILE_PREFIX + steamId + FILE_SUFFIX;

			if (!File.Exists(worldPath))
				return null;

			FileInfo playerPath = new FileInfo(worldPath);
			CharacterEntity character = new CharacterEntity(playerPath);

			return character;
		}

		/// <summary>
		/// Save the information of the character of the player
		/// </summary>
		/// <param name="steamId">Steam Id of the player</param>
		/// <param name="character">Character of the player</param>
		private void SavePlayerInfo(ulong steamId, CharacterEntity character)
		{
			string worldPath = m_gameAssemblyWrapper.GetServerConfig().LoadWorld;
			worldPath += "\\" + FILE_PREFIX + steamId + FILE_SUFFIX;

			character.Export(new FileInfo(worldPath));
		}

		#endregion

		/// <summary>
		/// This event handler is not used in this project but is necessary to respect the interface
		/// </summary>
		/// <param name="entity"></param>
		public void OnCharacterCreated(CharacterEntity entity)
		{
			//if the character is contained in the dictionnary, then the player just respawed
			if (m_deletedCharacterEntity.ContainsKey(entity.SteamId))
			{
				m_deletedCharacterEntity[entity.SteamId] = entity;
				Console.WriteLine(CONSOLE_PREFIX + "Player " + entity.Name + " (" + entity.SteamId + ") respawned.");
			}
			//If it is not contained inside the dictionnary, then the player just joined the server
			else
			{
				m_deletedCharacterEntity.Add(entity.SteamId, entity);

				Console.WriteLine(CONSOLE_PREFIX + "Player " + entity.Name + " (" + entity.SteamId + ") joined the server. Loading player data...");
				try
				{
					CharacterEntity playerInfo = LoadPlayerInfo(entity.SteamId);

					if (playerInfo == null)
					{
						Console.WriteLine(CONSOLE_PREFIX + "Player " + entity.SteamId + " (" + entity.SteamId + ") has no saved data.");
					}
					else
					{
						entity.Health = playerInfo.Health;
						entity.BatteryLevel = playerInfo.BatteryLevel;

						entity.Inventory.RefreshInventory();
						Thread.Sleep(1500);

						Console.WriteLine(CONSOLE_PREFIX + "Removing entities: " + entity.Inventory.Items.Count);

						foreach (InventoryItemEntity currentItem in entity.Inventory.Items)
						{
							entity.Inventory.DeleteEntry(currentItem);
							entity.Inventory.NextItemId--;
						}

						//Fill the inventory
						int i = 0;
						foreach (InventoryItemEntity currentItem in playerInfo.Inventory.Items)
						{
							Console.WriteLine(CONSOLE_PREFIX + "Adding entities: " + i++ + " - " + currentItem.Name);
							entity.Inventory.NewEntry(currentItem);
							entity.Inventory.NextItemId++;

							Thread.Sleep(150);
						}
						
						//entity.LightEnabled = playerInfo.LightEnabled;
						//entity.JetpackEnabled = playerInfo.JetpackEnabled;
						//entity.DampenersEnabled = playerInfo.DampenersEnabled;


						Console.WriteLine(CONSOLE_PREFIX + "Finished loading " + entity.SteamId + " (" + entity.SteamId + ") data.");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(CONSOLE_PREFIX + "Error: An error occured while trying to read " + entity.SteamId + " (" + entity.SteamId + ") data.");
					Console.WriteLine(CONSOLE_PREFIX + "Read the server log for more details.");
					LogManager.APILog.WriteLine(CONSOLE_PREFIX + ex);
				}

			}
		}

		/// <summary>
		/// OnCharacterDeleted event handler
		/// </summary>
		/// <param name="entity">Character entity</param>
		public void OnCharacterDeleted(CharacterEntity entity)
		{
		}
	}
}
