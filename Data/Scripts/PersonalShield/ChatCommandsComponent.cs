using Sandbox.ModAPI;
using System;
using System.Globalization;
using System.Text;

namespace CSEPersonalShield
{
    public sealed class ChatCommandsComponent
    {
        private const string FileName = "hud_offsets.txt";

        private Action<float, float> _onOffsetsChanged;
        private bool _registered;

        public float OffsetX { get; private set; }
        public float OffsetY { get; private set; }

        public void Init(Action<float, float> onOffsetsChanged)
        {
            _onOffsetsChanged = onOffsetsChanged;

            Load();

            if (MyAPIGateway.Utilities != null && !MyAPIGateway.Utilities.IsDedicated)
            {
                if (!_registered)
                {
                    MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
                    _registered = true;
                }
            }

            Apply();
        }

        public void Dispose()
        {
            if (_registered && MyAPIGateway.Utilities != null)
            {
                MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
            }

            _registered = false;
            _onOffsetsChanged = null;
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                return;
            }

            var text = messageText.Trim();
            if (!IsOurCommand(text))
            {
                return;
            }

            sendToOthers = false;

            var args = SplitArgs(text);

            // args[0] is command itself
            if (args.Length == 1 || IsArg(args[1], "help") || IsArg(args[1], "?"))
            {
                PrintHelp();
                return;
            }

            if (IsArg(args[1], "get") || IsArg(args[1], "show"))
            {
                PrintCurrent();
                return;
            }

            if (IsArg(args[1], "reset"))
            {
                Set(0f, 0f);
                PrintCurrent();
                return;
            }
            
            if (IsArg(args[1], "set"))
            {
                if (args.Length < 4)
                {
                    PrintHelp();
                    return;
                }

                float x;
                float y;
                if (!TryParseFloat(args[2], out x) || !TryParseFloat(args[3], out y))
                {
                    PrintHelp();
                    return;
                }

                Set(x, y);
                PrintCurrent();
                return;
            }
            
            if (IsArg(args[1], "x"))
            {
                if (args.Length < 3)
                {
                    PrintHelp();
                    return;
                }

                float x;
                if (!TryParseFloat(args[2], out x))
                {
                    PrintHelp();
                    return;
                }

                Set(x, OffsetY);
                PrintCurrent();
                return;
            }
            
            if (IsArg(args[1], "y"))
            {
                if (args.Length < 3)
                {
                    PrintHelp();
                    return;
                }

                float y;
                if (!TryParseFloat(args[2], out y))
                {
                    PrintHelp();
                    return;
                }

                Set(OffsetX, y);
                PrintCurrent();
                return;
            }

            // /pshud <x> <y>
            if (args.Length >= 3)
            {
                float x;
                float y;
                if (TryParseFloat(args[1], out x) && TryParseFloat(args[2], out y))
                {
                    Set(x, y);
                    PrintCurrent();
                    return;
                }
            }

            PrintHelp();
        }

        private void Set(float x, float y)
        {
            OffsetX = x;
            OffsetY = y;

            Save();
            Apply();
        }

        private void Apply()
        {
            var cb = _onOffsetsChanged;
            if (cb != null)
            {
                cb(OffsetX, OffsetY);
            }
        }

        private static void PrintHelp()
        {
            Show(
                "HUD offset commands:\n" +
                "/pshud get\n" +
                "/pshud set <x> <y>\n" +
                "/pshud x <value>\n" +
                "/pshud y <value>\n" +
                "/pshud reset"
            );
        }

        private void PrintCurrent()
        {
            Show("HUD offset: X=" + OffsetX.ToString("0.##", CultureInfo.InvariantCulture) +
                 " Y=" + OffsetY.ToString("0.##", CultureInfo.InvariantCulture));
        }

        private static void Show(string text)
        {
            if (MyAPIGateway.Utilities != null)
            {
                MyAPIGateway.Utilities.ShowMessage("PersonalShield", text);
            }
        }

        private void Load()
        {
            OffsetX = 0f;
            OffsetY = 0f;

            if (MyAPIGateway.Utilities == null)
            {
                return;
            }

            try
            {
                using (var reader =
                       MyAPIGateway.Utilities.ReadFileInLocalStorage(FileName, typeof(ChatCommandsComponent)))
                {
                    var raw = reader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        return;
                    }

                    // format: "x;y"
                    var parts = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                    {
                        return;
                    }

                    float x;
                    float y;
                    if (!TryParseFloat(parts[0], out x) || !TryParseFloat(parts[1], out y))
                    {
                        return;
                    }

                    OffsetX = x;
                    OffsetY = y;
                }
            }
            catch
            {
                // ignored
            }
        }

        private void Save()
        {
            if (MyAPIGateway.Utilities == null)
            {
                return;
            }

            try
            {
                using (var writer =
                       MyAPIGateway.Utilities.WriteFileInLocalStorage(FileName, typeof(ChatCommandsComponent)))
                {
                    writer.Write(
                        OffsetX.ToString("0.###", CultureInfo.InvariantCulture) + ";" +
                        OffsetY.ToString("0.###", CultureInfo.InvariantCulture)
                    );
                }
            }
            catch
            {
                // ignored
            }
        }

        private static bool IsOurCommand(string text)
        {
            return StartsWithCmd(text, "/pshud");
        }

        private static bool StartsWithCmd(string text, string cmd)
        {
            return text.StartsWith(cmd, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsArg(string a, string expected)
        {
            return string.Equals(a, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string[] SplitArgs(string text)
        {
            return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool TryParseFloat(string s, out float value)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}