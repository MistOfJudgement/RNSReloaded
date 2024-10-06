using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using static System.Diagnostics.Debug;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Frozen;
namespace RNSReloaded.Randomizer;

//At some point I'm going to create a wrapper class over IRNSReloaded to have safe accessor
public unsafe class Randomizer {
    public struct HBSInfoData {
        public string id;
        public string strId;
        public string name;
        public string description;
        // 15 more RValues
    }
    
    public struct AllyData {
        public string id;
        public string name;
        public string description;
        //3 ints
        // a bunch of id strings for moves and upgrades
        // 4 moves * (1 normal + 5 upgrades)
    }

    // the items data array is of the format [ [ItemData: unlocked, ItemData: locked] ]
    public struct ItemData {
        public string id;
        int smth; //probably to do with the set but I need to actually check
        public string name;
        public string description;
        //14 ints
        // string ? is blank
        //2 ints
    }

    public struct EnemyData {
        string id;
        string name;
        string description;
        string anim;
        //2 large nums, 2 ints, 1 float, 3 ints
    }

     struct TrinketData {
        string id;
        string name;
        string description;
        string unlockCondition;
        // a bunch of stuff
    }

    struct NPCInfo {
        string id;
        string fullname;
        string name;
        string animData;
        //ints
    }

    private Dictionary<string, string> completeMap = [];
    public FrozenDictionary<string, string> CompleteMap { get => this.completeMap.ToFrozenDictionary(); }
    private Dictionary<string, string> languageMap = [];
    public FrozenDictionary<string, string> LanguageMap { get => this.languageMap.ToFrozenDictionary(); }
    private Dictionary<string, AllyData> allyData = [];
    private Dictionary<string, ItemData> itemData = [];
    public Randomizer() {

    }

    public void Randomize(
        IRNSReloaded rns,
        ILoggerV1 logger
        ) {
        //Get all the strings that need to be randomized
        //put them in a map ( I guess it could just be an array )
        //  optional: based on some filter
        this.LoadLanguageMap(rns);
        this.LoadAllData(rns);
        //shuffle said map
        //  optional: maybe this step is based on a filter
        //for each entry
        //reverse the map key to the data source
        logger.PrintMessage($"Loaded {this.languageMap.Count} values into the lang map", Color.Wheat);
        logger.PrintMessage($"Loaded {this.allyData.Count} value into allydata", Color.Wheat);
    }
    public void LoadLanguageMap(IRNSReloaded rns) {
        //o h my god do it manually, then tidy it later
        var global = rns.GetGlobalInstance();
        var langMap = rns.FindValue(global, "languageMap");
        //something is broken
        var currentKey = rns.ExecuteCodeFunction("ds_map_find_first", null, null, [*langMap]).GetValueOrDefault();

        while (currentKey.Type != RValueType.Undefined) {
            var value = rns.ExecuteCodeFunction("ds_map_find_first", null, null, [*langMap, currentKey]).GetValueOrDefault();
            if (value.Type == RValueType.String) {
                this.languageMap[rns.GetString(&currentKey)] = rns.GetString(&value);
            }
            currentKey = rns.ExecuteCodeFunction("ds_map_find_next", null, null, [*langMap, currentKey]).GetValueOrDefault();
        }
    }

    private void LoadAllyData(IRNSReloaded rns) {
        var global = rns.GetGlobalInstance();
        var allyMap = rns.FindValue(global, "allyData");
        var lengthRValue = rns.ArrayGetLength(allyMap).GetValueOrDefault();
        Assert(lengthRValue.Type == RValueType.Real, "Expected a Real type for the length of allyData array.");
        int length = (int) lengthRValue.Real;
        for (int i = 0; i < length; i++) {
            var entry = rns.ArrayGetEntry(allyMap, i);
            var id = rns.GetString(rns.ArrayGetEntry(entry, 0));
            var name = rns.GetString(rns.ArrayGetEntry(entry, 1));
            var desc = rns.GetString(rns.ArrayGetEntry(entry, 2));
            var allyData = new AllyData() {
                id = id,
                name = name,
                description = desc
            };
            this.allyData[id] = allyData;
        }
    }

    private void LoadItemData(IRNSReloaded rns) {
        var global = rns.GetGlobalInstance();
        var itemMap = rns.FindValue(global, "itemData");
        var lengthRValue = rns.ArrayGetLength(itemMap).GetValueOrDefault();
        Assert(lengthRValue.Type == RValueType.Real, "Expected a Real type for the length of itemData array.");
        int length = (int) lengthRValue.Real;
        for (int i = 0; i < length; i++) {
            var itemData = rns.ArrayGetEntry(itemMap, i);
            var unlockedItem = rns.ArrayGetEntry(itemData, 0);
            var id = rns.GetString(rns.ArrayGetEntry(unlockedItem, 0));
            var name = rns.GetString(rns.ArrayGetEntry(unlockedItem, 2));
            var desc = rns.GetString(rns.ArrayGetEntry(unlockedItem, 3));
            var item = new ItemData() {
                id = id,
                name = name,
                description = desc
            };
            this.itemData[id] = item;

        }

    }

    private void LoadData<T>(IRNSReloaded rns, string dataKey, Action<T, RValue, IRNSReloaded> populateData, Dictionary<string, T> dataDictionary) where T : new() {
        var global = rns.GetGlobalInstance();
        var dataMap = rns.FindValue(global, dataKey);
        var lengthRValue = rns.ArrayGetLength(dataMap).GetValueOrDefault();
        Assert(lengthRValue.Type == RValueType.Real, $"Expected a Real type for the length of {dataKey} array.");
        int length = (int)lengthRValue.Real;
        for (int i = 0; i < length; i++) {
            var entry = rns.ArrayGetEntry(dataMap, i);
            var id = rns.GetString(rns.ArrayGetEntry(entry, 0));
            var data = new T();
            populateData(data, *entry, rns);
            dataDictionary[id] = data;
        }
    }

    private void PopulateAllyData(AllyData allyData, RValue entry, IRNSReloaded rns) {
        allyData.id = rns.GetString(rns.ArrayGetEntry(&entry, 0));
        allyData.name = rns.GetString(rns.ArrayGetEntry(&entry, 1));
        allyData.description = rns.GetString(rns.ArrayGetEntry(&entry, 2));
    }

    private void PopulateItemData(ItemData itemData, RValue entry, IRNSReloaded rns) {
        var unlockedItem = rns.ArrayGetEntry(&entry, 0);
        itemData.id = rns.GetString(rns.ArrayGetEntry(unlockedItem, 0));
        itemData.name = rns.GetString(rns.ArrayGetEntry(unlockedItem, 2));
        itemData.description = rns.GetString(rns.ArrayGetEntry(unlockedItem, 3));
    }

    public void LoadAllData(IRNSReloaded rns) {
        this.LoadData(rns, "allyData", this.PopulateAllyData, this.allyData);
        this.LoadData(rns, "itemData", this.PopulateItemData, this.itemData);
    }
    
}

