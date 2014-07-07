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

namespace PlayerPersistencePlugin
{
    public class Core : PluginBase, IPlayerEventHandler
	{
		#region "Attributes and Constant"

		const string FILE_PREFIX = "player_";
		const string FILE_SUFFIX = ".sbc";

		SandboxGameAssemblyWrapper m_gameAssemblyWrapper;

		#endregion

		#region "Constructors and Initializers"

		public Core()
		{
			m_gameAssemblyWrapper = SandboxGameAssemblyWrapper.Instance;
		}

		public override void Init()
		{
			Console.WriteLine("Player Persistence Plugin '" + Id + "' initialized!");
		}

		#endregion

		#region "Methods"

		#region Player events

		/// <summary>
		/// This method is called each time a player join the server
		/// </summary>
		/// <param name="remoteUserId">Steam ID of the player</param>
		/// <param name="character">Ingame Character of the player</param>
		public void OnPlayerJoined(ulong remoteUserId, CharacterEntity character)
		{
			Console.WriteLine("Player " + character.Name + " joined the server. Loading player data...");
			try
			{
				CharacterEntity playerInfo = GetPlayerInfo(remoteUserId);

				if (playerInfo == null)
				{
					Console.WriteLine("Player " + character.Name + " ("+remoteUserId+") has no saved data.");
				}
				else
				{
					Console.WriteLine("Finished loading " + character.Name + " data.");
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine("Error: Could not load " + character.Name + " (" + remoteUserId + ") data.");
				Console.WriteLine("Read the server log for more details.");
				LogManager.GameLog.WriteLine(ex);
			}
		}

		/// <summary>
		/// This method is called each time a player left the server
		/// </summary>
		/// <param name="remoteUserId">Steam ID of the player</param>
		/// <param name="character">Ingame Character of the player</param>
		public void OnPlayerLeft(ulong remoteUserId, CharacterEntity character)
		{
			Console.WriteLine("Player " + character.Name + " left the server. Saving player data...");
			SavePlayerInfo(remoteUserId, character);
			Console.WriteLine("Player " + character.Name + "'s data saved correctly.");
		}

		#endregion

		/// <summary>
		/// This method is called on a regular base. It is here simply to respect the contract. It is not used in this plugin.
		/// </summary>
		public override void Update()
		{ }

		/// <summary>
		/// Get the path of the loaded save file.
		/// </summary>
		/// <returns>Path to the loaded save file</returns>
		private string GetSavePath()
		{
			string worldPath = m_gameAssemblyWrapper.GetServerConfig().LoadWorld;
			MyFSPath path = new MyFSPath(MyFSLocationEnum.Saves, worldPath);
			return path.Relative + @"\";
		}

		/// <summary>
		/// Get the information of the last session of the player
		/// </summary>
		/// <param name="steamId">Steam Id of the player</param>
		/// <returns>Return the character of the player, or null if no info were found</returns>
		private CharacterEntity GetPlayerInfo(ulong steamId)
		{
			string worldPath = GetSavePath();
			worldPath += FILE_PREFIX + steamId + FILE_SUFFIX;

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
			string worldPath = GetSavePath();
			worldPath += FILE_PREFIX + steamId + FILE_SUFFIX;

			character.Export(new FileInfo(worldPath));
		}

		#endregion
	}
}
