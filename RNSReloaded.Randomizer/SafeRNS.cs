

using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RNSReloaded.Randomizer;


public class SafeRNS {


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static SafeRNS Instance { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    private readonly IRNSReloaded rns;
    

    public SafeRNS(IRNSReloaded rns) {
        this.rns = rns;

        Instance = this;
    }

    public unsafe RValue ArrayGetEntry(RValue array, int index) {
        return *this.rns.ArrayGetEntry(&array, index);
    }

    public unsafe RValue? ArrayGetLength(RValue array) {
        return this.rns.ArrayGetLength(&array);
    }

    public int? CodeFunctionFind(string name) {
        return this.rns.CodeFunctionFind(name);
    }

    public unsafe void CreateString(RValue value, string str) {
        this.rns.CreateString(&value, str);
    }

    public unsafe RValue CreateString(string str) {
        RValue rValue = new();
        this.CreateString(rValue, str);
        return rValue;
    }


    public unsafe RValue? ExecuteCodeFunction(string name, CInstance self, CInstance other, RValue[] arguments) {
        return this.rns.ExecuteCodeFunction(name, &self, &other, arguments);
    }

    public unsafe RValue? ExecuteGlobalFunction(string name, params RValue[] arguments) {
        return this.rns.ExecuteCodeFunction(name, null, null, arguments);
    }

    public unsafe RValue? ExecuteScript(string name, CInstance self, CInstance other, RValue[] arguments) {
        return this.rns.ExecuteScript(name, &self, &other, arguments);
    }

    public unsafe RValue? ExecuteGlobalScript(string name, CInstance self, CInstance other, RValue[] arguments) {
        return this.rns.ExecuteScript(name, &self, &other, arguments);
    }

    // I think this also breaks
    //public unsafe RValue FindValue(CInstance instance, string name) {
    //    return *this.rns.FindValue(&instance, name);
    //}

    public unsafe CRoom GetCurrentRoom() {
        return *this.rns.GetCurrentRoom();
    }

    //This isn't useful
    //public unsafe CInstance GetGlobalInstance() {
    //    return *this.rns.GetGlobalInstance();
    //}

    public unsafe RValue? GetGlobalValue(string name) {
        var global = this.rns.GetGlobalInstance();
        var val = this.rns.FindValue(global, name);
        return *val;
    }

    public unsafe CScript GetScriptData(int id) {
        return *this.rns.GetScriptData(id);
    }

    public unsafe string GetString(RValue value) {
        return this.rns.GetString(&value);
    }

    public unsafe List<string> GetStructKeys(RValue value) {
        return this.rns.GetStructKeys(&value);
    }

    public RFunctionStringRef GetTheFunction(int id) {
        return this.rns.GetTheFunction(id);
    }

    public void LimitOnlinePlay() {
        this.rns.LimitOnlinePlay();
    }

    public int ScriptFindId(string name) {
        return this.rns.ScriptFindId(name);
    }
}
