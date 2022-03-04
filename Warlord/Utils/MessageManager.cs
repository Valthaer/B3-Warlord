using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Warlord
{
    public class MessageManager
    {
        internal static void DisplayMessage(string msg)
        {            
            InformationManager.DisplayMessage(new InformationMessage(msg, ModuleColors.uiStandardTextColor));
        }

        internal static void DisplayMessage(string msg, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage(msg, color));
        }

        internal static void DisplayMessage(TextObject textObj)
        {
            InformationManager.DisplayMessage(new InformationMessage(textObj.ToString()));
        }

        internal static void DisplayMessage(TextObject textObj, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage(textObj.ToString(), color));
        }

        internal static void DisplayNotification(string msg, BasicCharacterObject character)
        {
            InformationManager.AddQuickInformation(new TextObject(msg), announcerCharacter: character);
        }

        internal static void DisplayNotification(TextObject textObj, BasicCharacterObject character)
        {
            InformationManager.AddQuickInformation(textObj, announcerCharacter: character);
        }
        internal static void DisplayDebugMessage(string msg)
        {
            if (WarlordModuleSettings.Instance.DebugMessagesEnabled)
            {
                InformationManager.DisplayMessage(new InformationMessage("[Warlord DEBUG] " + msg, Colors.Red));
            }                
        }
    }
}
