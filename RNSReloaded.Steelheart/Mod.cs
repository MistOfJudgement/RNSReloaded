using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RNSReloaded.PermanentWinds;

public unsafe class Mod : IMod {
    private WeakReference<IRNSReloaded>? rnsReloadedRef;
    private WeakReference<IReloadedHooks>? hooksRef;
    private ILoggerV1 logger = null!;

    private IHook<ScriptDelegate>? damageHook;
    private IHook<ScriptDelegate>? enrageHook;
    private IHook<ScriptDelegate>? newFightHook;
    // Need to disable enrage after phase change
    private IHook<ScriptDelegate>? phaseChangeHook;
    // For Shira unavoidable steel yourself attacks
    private IHook<ScriptDelegate>? steelHook;
    private IHook<ScriptDelegate>? steelActivateHook;
    // To disable general invulnerability
    private IHook<ScriptDelegate>? invulnHook;
    private IHook<ScriptDelegate>? playerDmgHook;
    // To disable invuln from certain buffs
    private IHook<ScriptDelegate>? buffFlagCheckHook;

    // Flag to track if the enemy has started enrage
    // Set to false on combat start and phase change (scrdt_encounter, scrbp_boss_heal)
    // Set to true on enrage (scrbp_make_warning_enrage) (the actual enrage pattern is not used by bosses)
    private bool enraged = false;
    // Name of the current encounter, so we can check vs. boss encounters to see where to pause HP at.
    private string currentEncounter = "";
    private static readonly Dictionary<string, double> BossPhaseChanges = new Dictionary<string, double>() {
        { "enc_rabbit_queen0", 0.3 },
        { "enc_dragon_mythril0", 0.4 },
        { "enc_bird_valedictorian0", 0.4 },
        { "enc_frog_idol0", 0.4 },
        { "enc_mouse_paladin0", 0.4 },
        { "enc_wolf_steeltooth0", 0.4 },
    };

    // Counts how many times Shira has used her steel yourself attack. Note that the script is 
    // called twice per time she attacks. So 1/2 = first attack, 3/4 = 2nd, 5/6 = 3rd
    private int shiraSteelCount = 0;

    public void Start(IModLoaderV1 loader) {
        this.rnsReloadedRef = loader.GetController<IRNSReloaded>();
        this.hooksRef = loader.GetController<IReloadedHooks>()!;
        
        this.logger = loader.GetLogger();

        if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)) {
            rnsReloaded.OnReady += this.Ready;
        }
    }

    public void Ready() {
        if (
            this.rnsReloadedRef != null
            && this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)
            && this.hooksRef != null
            && this.hooksRef.TryGetTarget(out var hooks)
        ) {
            rnsReloaded.LimitOnlinePlay();

            var damageScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_pattern_deal_damage_enemy_subtract") - 100000);
            this.damageHook =
                hooks.CreateHook<ScriptDelegate>(this.EnemyDamageDetour, damageScript->Functions->Function);
            this.damageHook.Activate();
            this.damageHook.Enable();

            var enrageScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scrbp_make_warning_enrage") - 100000);
            this.enrageHook =
                hooks.CreateHook<ScriptDelegate>(this.EnrageDetour, enrageScript->Functions->Function);
            this.enrageHook.Activate();
            this.enrageHook.Enable();

            var newFightScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scrdt_encounter") - 100000);
            this.newFightHook =
                hooks.CreateHook<ScriptDelegate>(this.NewFightDetour, newFightScript->Functions->Function);
            this.newFightHook.Activate();
            this.newFightHook.Enable();

            var phaseChangeScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scrbp_boss_heal") - 100000);
            this.phaseChangeHook =
                hooks.CreateHook<ScriptDelegate>(this.PhaseChangeDetour, phaseChangeScript->Functions->Function);
            this.phaseChangeHook.Activate();
            this.phaseChangeHook.Enable();

            var steelScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("bp_rabbit_queen1_steel") - 100000);
            this.steelHook =
                hooks.CreateHook<ScriptDelegate>(this.SteelDetour, steelScript->Functions->Function);
            this.steelHook.Activate();
            this.steelHook.Enable();

            var steelAScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("bp_rabbit_queen1_steel_activate") - 100000);
            this.steelActivateHook =
                hooks.CreateHook<ScriptDelegate>(this.SteelActivateDetour, steelAScript->Functions->Function);
            this.steelActivateHook.Activate();
            this.steelActivateHook.Enable();

            var invulnScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_player_invuln") - 100000);
            this.invulnHook =
                hooks.CreateHook<ScriptDelegate>(this.InvulnDetour, invulnScript->Functions->Function);
            this.invulnHook.Activate();
            this.invulnHook.Enable();

            var playerDmgScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_pattern_deal_damage_ally") - 100000);
            this.playerDmgHook =
                hooks.CreateHook<ScriptDelegate>(this.PlayerDmgDetour, playerDmgScript->Functions->Function);
            this.playerDmgHook.Activate();
            this.playerDmgHook.Enable();

            var buffFlagScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_hbsflag_check") - 100000);
            this.buffFlagCheckHook =
                hooks.CreateHook<ScriptDelegate>(this.AddHbsFlagCheckDetour, buffFlagScript->Functions->Function);
            this.buffFlagCheckHook.Activate();
            this.buffFlagCheckHook.Enable();
        }
    }

    private RValue* AddHbsFlagCheckDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        returnValue = this.buffFlagCheckHook!.OriginalFunction(self, other, returnValue, argc, argv);
        if (argv[2]->Real == 1 || argv[2]->Real == 2 || argv[2]->Real == 32) { // Vanish/Ghost, Stoneskin, Super
            returnValue->Real = 0;
        }
        return returnValue;
    }

    // Normally I'd just call the real invuln hook here instead of setting a flag as a hidden 
    // parameter to the invuln detour, but for whatever reason when I try to call it, despite
    // having NO string arguments, scr_player_invuln complains about an invalid STRING argument
    private bool isTakingDamage = false;
    private RValue* PlayerDmgDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        this.isTakingDamage = true;
        returnValue = this.playerDmgHook!.OriginalFunction(self, other, returnValue, argc, argv);
        this.isTakingDamage = false;
        return returnValue;
    }

    private RValue* InvulnDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        // Change invuln to a very negative number so extra invuln effects don't work.
        // Note that this still procs on invuln effects
        if (!this.isTakingDamage) {
            argv[0]->Real = -30000;
        }
        returnValue = this.invulnHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }

    private RValue* GetEnemy(double id) {
        if (this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded)) {
            var instance = rnsReloaded.GetGlobalInstance();
            var playerList = rnsReloaded.FindValue(instance, "player");
            var enemyList = rnsReloaded.ArrayGetEntry(playerList, 1);
            return rnsReloaded.ArrayGetEntry(enemyList, (int) id);
        } else {
            throw new NullReferenceException("Hardcore mod not loaded properly - can't get RnsReloaded reference");
        }
    }
    private double GetEnemyHP(double id) {
        if (this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded)) {
            var instance = rnsReloaded.GetGlobalInstance();
            var hpList = rnsReloaded.FindValue(instance, "playerHp");
            var enemyHps = rnsReloaded.ArrayGetEntry(hpList, 1);
            var hpValue = rnsReloaded.ArrayGetEntry(enemyHps, (int) id);
            switch (hpValue->Type) {
                case RValueType.Real:
                    return hpValue->Real;
                case RValueType.Int32:
                    return hpValue->Int32;
                case RValueType.Int64:
                    return hpValue->Int64;
                default:
                    return 0;
            }
        } else {
            throw new NullReferenceException("Hardcore mod not loaded properly - can't get RnsReloaded reference");
        }
    }

    private double GetEnemyMaxHP(double id) {
        if (this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded)) {
            return rnsReloaded.FindValue(this.GetEnemy(id)->Object, "displayMaxHp")->Real;
        } else {
            throw new NullReferenceException("Hardcore mod not loaded properly - can't get RnsReloaded reference");
        }
    }

    private RValue* EnemyDamageDetour(
        CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
    ) {
        // If enraged, pale keep mobs, or matti mice summons, then skip the HP locking
        // (pale keep mobs never enrage, and matti mice need to be killable to get to enrage)
        if (!this.enraged &&
            !(this.currentEncounter == "enc_mouse_paladin1" && argv[1]->Real > 0) &&
            !this.currentEncounter.StartsWith("enc_queens"))
        {
            double enemyHp = this.GetEnemyHP(argv[1]->Real);
            double enemyMaxHp = this.GetEnemyMaxHP(argv[1]->Real);
            if (BossPhaseChanges.TryGetValue(this.currentEncounter, out var hpThreshold)) {
                // Bosses phase change higher HP (depends on the boss)
                // So to force p1 to go to enrage, we put a check there instead of at 1 HP
                if ((enemyHp - argv[2]->Real) < (enemyMaxHp * hpThreshold + 1)) {
                    argv[2]->Real = Math.Max((enemyHp - enemyMaxHp * hpThreshold) - 1, 0);
                }
            } else if (enemyHp - argv[2]->Real < 1) {
                // Non bosses (and boss phase 2s) can go to 1 HP before waiting on enrage to be killable
                argv[2]->Real = Math.Max(enemyHp - 1, 0);
            }
        }

        returnValue = this.damageHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }

    private RValue* EnrageDetour(
        CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
    ) {
        this.enraged = true;
        returnValue = this.enrageHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }

    private RValue* NewFightDetour(
        CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
    ) {
        this.enraged = false;
        returnValue = this.newFightHook!.OriginalFunction(self, other, returnValue, argc, argv);
        this.currentEncounter = argv[0]->ToString();
        return returnValue;
    }

    private RValue* PhaseChangeDetour(
        CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
    ) {
        this.enraged = false;
        // Change the encounter to the phase 2 version, since it doesn't automatically happen
        // This is important for caring about p2 mechanics (aka matti mice)
        this.currentEncounter = this.currentEncounter.Replace("0", "1");
        this.shiraSteelCount = 0;
        returnValue = this.phaseChangeHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }

    private RValue* SteelActivateDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        this.shiraSteelCount++;
        if (this.shiraSteelCount == 5) {
            return returnValue;
        }
        returnValue = this.steelActivateHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }

    private RValue* SteelDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        // If this is after the 3rd time she casts steel yourself, we've hit soft enrage
        if (this.shiraSteelCount >= 5) {
            this.enraged = true;
        }
        returnValue = this.steelHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }

    public void Suspend() {
        this.damageHook?.Disable();
    }

    public void Resume() {
        this.damageHook?.Enable();
    }

    public bool CanSuspend() => false; // Add suspend/resume code and set to true once ready

    public void Unload() { }
    public bool CanUnload() => false;

    public Action Disposing => () => { };
}
