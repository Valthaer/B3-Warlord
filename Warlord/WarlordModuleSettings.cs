using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ModLib.Definitions;
using ModLib.Definitions.Attributes;

namespace Warlord
{
	public class WarlordModuleSettings : SettingsBase
	{
		public override string ModName
		{
			get
			{
				return "Warlord";
			}
		}

		public override string ModuleFolderName
		{
			get
			{
				return "Warlord";
			}
		}

		[XmlElement]
		public override string ID { get; set; } = "WarlordModuleSettings";
		public static WarlordModuleSettings Instance
		{
			get
			{
				return (WarlordModuleSettings)SettingsDatabase.GetSettings<WarlordModuleSettings>();
			}
		}

		[XmlElement]
		[SettingProperty("Lord Party Spawn Delay Enabled", "If Enabled, Lord Parties time to spawn after being destroyed will be larger.")]
		public bool LordPartySpawnDelayEnabled { get; set; } = true;

		[XmlElement]
		[SettingProperty("Lord Party Spawn Size Reduced Enabled", "If Enabled, Lord Parties will spawn only with the Lords after being destroyed.")]
		public bool LordPartySpawnSizeReducedEnabled { get; set; } = true;

		[XmlElement]
		[SettingProperty("Battle Relationship Increase", 1, 5, "The maximum base value that your relationship will increase from battles")]
		public int BattleRelationshipIncrease { get; set; } = 1;

		[XmlElement]
		[SettingProperty("Tournament Relationship Increase", 1, 5, "The base value that your relationship will increase from tournaments")]
		public int TournamentRelationshipIncrease { get; set; } = 1;

		[XmlElement]
		[SettingProperty("Radius", 10, 500, "This is the size of the radius inside which villages and towns will be affected by the relationship increase.")]
		public int Radius { get; set; } = 10;

		public const string InstanceID = "WarlordModuleSettings";
	}
}
