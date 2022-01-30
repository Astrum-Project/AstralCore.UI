using MelonLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Astrum.AstralCore.UI.Attributes;

[assembly: MelonInfo(typeof(Astrum.AstralCore.UI.CoreUI), "AstralCore.UI", "1.5.0", downloadLink: "github.com/Astrum-Project/AstralCore.UI")]
[assembly: MelonColor(ConsoleColor.DarkMagenta)]

namespace Astrum.AstralCore.UI
{
    public class CoreUI : MelonMod
    {
        public static readonly Dictionary<string, Module> Modules = new(StringComparer.OrdinalIgnoreCase);

        public static event Action<Module> ModuleCreated = new(_ => { });
        public static event Action<Module> ModuleRemoved = new(_ => { });
        public static event Action<Module, UIBase> CommandRegistered = new((_, __) => { });
        public static event Action<Module, UIBase> CommandUnregistered = new((_, __) => { });

        public override void OnApplicationLateStart()
        {
            Stopwatch sw = new();
            sw.Start();

            MelonHandler.Mods.ForEach(x => ScanAssembly(x.Assembly));

            sw.Stop();
            Logger.Debug("[Core.UI] Scanned in " + sw.ElapsedMilliseconds + " ms"); 
        }

        public static void ScanAssembly(Assembly assembly)
        {
            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types; }

            foreach ((MemberInfo info, UIBase attr) in types
                .SelectMany(x => x.GetMembers())
                .SelectMany(x => x.GetCustomAttributes(typeof(UIBase), false).Select(y => (x, y as UIBase))))
            {
                if (!Modules.TryGetValue(attr.Module, out var module))
                    ModuleCreated(module = CreateModule(attr.Module));

                try { attr.GetType().GetMethod("Setup").Invoke(attr, new object[1] { info }); }
                catch { Logger.Error($"An exception has occurred whilst setting up {attr.Module}:{attr.Name}"); }

                CommandRegistered(module, module.Commands[attr.Name] = attr);
            }
        }

        public static void Register(UIBase elem)
        {
            if (elem.Module == null || elem.Name == null)
                throw new Exception("Element was not setup prior to registration");

            if (!Modules.TryGetValue(elem.Module, out Module module))
                ModuleCreated(module = CreateModule(elem.Module));

            CommandRegistered(module, module.Commands[elem.Name] = elem);
        }

        public static bool Unregister(UIBase elem)
        {
            if (elem.Module == null || elem.Name == null)
                return false;

            return Unregister(elem.Module, elem.Name);
        }

        public static bool Unregister(string moduleName, string elemName)
        {
            if (!Modules.TryGetValue(moduleName, out Module module)) return false;

            if (module.Commands.TryGetValue(elemName, out var command))
            {
                module.Commands.Remove(elemName);
                CommandUnregistered(module, command);
            }
            else return false;

            if (module.Commands.Count <= 0 && Modules.Remove(moduleName))
                ModuleRemoved(module);

            return true;
        }

        private static Module CreateModule(string name) => Modules[name] = new Module() { Commands = new Dictionary<string, UIBase>(StringComparer.OrdinalIgnoreCase) };
    }
}
