using MelonLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Astrum.AstralCore.UI.Attributes;

[assembly: MelonInfo(typeof(Astrum.AstralCore.UI.CoreUI), "AstralCore.UI", "1.1.1", downloadLink: "github.com/Astrum-Project/AstralCore.UI")]
[assembly: MelonColor(ConsoleColor.DarkMagenta)]

namespace Astrum.AstralCore.UI
{
    public class CoreUI : MelonMod
    {
        public static readonly Dictionary<string, Module> Modules = new(StringComparer.OrdinalIgnoreCase);
        public static event Action OnPreScan = new(() => { });
        public static event Action OnRescan = new(() => { });

        public override void OnApplicationLateStart() => Rescan();

        [UIButton("Core.UI", "Rescan")]
        public static void Rescan()
        {
            Stopwatch sw = new();
            sw.Start();

            Modules.Clear();

            OnPreScan();

            foreach ((MemberInfo info, UIBase attr) in MelonHandler.Mods
                .Select(x => x.Assembly)
                .SelectMany(x => x.GetExportedTypes())
                .SelectMany(x => x.GetMembers())
                .SelectMany(x => x.GetCustomAttributes(typeof(UIBase), false).Select(y => (x, y as UIBase))))
            {
                if (!Modules.TryGetValue(attr.Module, out var module))
                    module = Modules[attr.Module] = new Module() { Commands = new Dictionary<string, UIBase>(StringComparer.OrdinalIgnoreCase) };

                try 
                {
                    attr.GetType().GetMethod("Setup", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(attr, new object[1] { info });
                } 
                catch
                {
                    Logger.Error($"An exception has occurred whilst setting up {attr.Module}:{attr.Name}");
                }

                module.Commands[attr.Name] = attr;
            }

            OnRescan();

            sw.Stop();
            Logger.Debug("[Core.UI] Scanned in " + sw.ElapsedMilliseconds + " ms");
        }
    }
}
