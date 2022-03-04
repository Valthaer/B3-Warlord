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
		[SettingProperty("Relation by Events Enabled", "If Enabled, will improve relations to nearby characters by succesful events.")]
		public bool RelationByEventsEnabled { get; set; } = true;

#if DEBUG
		[XmlElement]
		[SettingProperty("Debug Messages Enabled", "If Enabled, will show debug messages.")]
		public bool DebugMessagesEnabled { get; set; } = false;
#endif
		//[XmlElement]
		//[SettingProperty(" X Increase", 1, 5, "The base value that your X will increase from Y")]
		//public int XIValueIncrease { get; set; } = 1;

		public const string InstanceID = "WarlordModuleSettings";
	}
}
