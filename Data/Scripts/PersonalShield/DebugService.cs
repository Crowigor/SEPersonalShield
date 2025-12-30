using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Utils;

namespace CSEPersonalShield
{
    public static class DebugService
    {
        public static void Message(string message)
        {
            General(message);
        }

        public static void General(string message)
        {
            if (!Config.Debug)
            {
                return;
            }

            Send("[GEN] " + message);
        }

        public static void Damage(string message)
        {
            if (!Config.Debug || !Config.DebugDamage)
            {
                return;
            }

            Send("[DMG] " + message);
        }

        public static void Recharge(string message)
        {
            if (!Config.Debug || !Config.DebugRecharge)
            {
                return;
            }

            Send("[RCH] " + message);
        }

        private static void Send(string message)
        {
            var sender = Config.DebugLogSender;

            try
            {
                MyLog.Default.WriteLineAndConsole("[" + sender + "] " + message);
            }
            catch
            {
                // ignored
            }

            try
            {
                if (MyAPIGateway.Session == null)
                {
                    return;
                }

                if (MyAPIGateway.Session.IsServer)
                {
                    MyVisualScriptLogicProvider.SendChatMessage(message, sender);
                    return;
                }

                if (MyAPIGateway.Utilities != null && !MyAPIGateway.Utilities.IsDedicated)
                {
                    MyAPIGateway.Utilities.ShowMessage(sender, message);
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}