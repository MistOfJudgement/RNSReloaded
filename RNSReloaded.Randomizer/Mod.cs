using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System.Collections;
using System.Drawing;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Diagnostics;
using RNSReloaded;
using System.Runtime.ExceptionServices;
using RNSReloaded.Randomizer.Config;
namespace RNSReloaded.Randomizer;

public class Mod : IMod {
    private Configurator? configurator;
    private Config.Config? config;

    public void StartEx(IModLoaderV1 loader, IModConfigV1 modConfig) {
        
        this.configurator = new Configurator(((IModLoader) loader).GetModConfigDirectory(modConfig.ModId));
        this.config = this.configurator.GetConfiguration<Config.Config>(0);
        this.config.ConfigurationUpdated += this.ConfigurationUpdated;
    }

    private void ConfigurationUpdated(IUpdatableConfigurable newConfig) {
        this.config = (Config.Config) newConfig;
    }

    
    public void Ready() {
        
    }
    
    public void Suspend() {
    }

    public void Resume() {
    }

    public bool CanSuspend() => true;

    public void Unload() { }
    public bool CanUnload() => false;

    public Action Disposing => () => { };
}
