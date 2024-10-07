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

public unsafe class Mod : IMod {
    private Configurator? configurator;
    private Config.Config? config;
    private WeakReference<IRNSReloaded>? rnsReloadedRef;
    private WeakReference<IReloadedHooks>? hooksRef;
    private ILoggerV1 logger;

    public void StartEx(IModLoaderV1 loader, IModConfigV1 modConfig) {
        
        this.configurator = new Configurator(((IModLoader) loader).GetModConfigDirectory(modConfig.ModId));
        this.config = this.configurator.GetConfiguration<Config.Config>(0);
        this.config.ConfigurationUpdated += this.ConfigurationUpdated;

        this.hooksRef = loader.GetController<IReloadedHooks>();
        this.rnsReloadedRef = loader.GetController<IRNSReloaded>();
        this.logger = loader.GetLogger();
        Utils.logger = this.logger;
        if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)) {
            rnsReloaded.OnReady += this.Ready;
        }
    }

    private void ConfigurationUpdated(IUpdatableConfigurable newConfig) {
        this.config = (Config.Config) newConfig;
    }

    
    public void Ready() {
        if ((this.hooksRef != null && this.hooksRef.TryGetTarget(out var hooks)) &&
                (this.rnsReloadedRef != null && this.rnsReloadedRef.TryGetTarget(out var rns))) {
            var id = rns.ScriptFindId("scr_lang_strings_init");
            var data = rns.GetScriptData(id - 100_000);
            var hook = hooks.CreateHook(this.LangStringDetour, data->Functions->Function);
            hook.Activate();
            hook.Enable();
            this.logger.PrintMessage("Enabled Hook", Color.Red);
            _ = new SafeRNS(rns);
        }
        this.logger.PrintMessage("Done Ready", Color.Red);


    }
    private RValue* LangStringDetour(CInstance* self,
                                     CInstance* other,
                                     RValue* ret,
                                     int argc,
                                     RValue** argv) {
        if (this.rnsReloadedRef != null && this.rnsReloadedRef.TryGetTarget(out var rns)) {
            var randomizer = new Randomizer();
            randomizer.Randomize(rns, this.logger);
        }
        return ret;
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
