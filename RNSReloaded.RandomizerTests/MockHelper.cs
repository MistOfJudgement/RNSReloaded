using Moq;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Interfaces;

namespace RNSReloaded.RandomizerTests;
public static unsafe class MockHelper {
    public static Dictionary<int, string> stringStorage = [];
    public static CInstance globalInstance;
    public static RValue CreateStringRValue(string value) {
        stringStorage[value.GetHashCode()] = value;
        return new RValue {  Int32 = value.GetHashCode(), Type = RValueType.String };
    }

    public unsafe static Mock<IRNSReloaded> SetupMockIRNSReloaded(Dictionary<string, string> mapData) {
        var mockRns = new Mock<IRNSReloaded>();

        // Create a dummy global instance
        mockRns.Setup(r => r.GetGlobalInstance());

        // Create a dummy language map
        var langMap = new RValue();
        mockRns.Setup(r => r.FindValue(&globalInstance, "languageMap")).Returns(&langMap);

        // Setup ds_map_find_first and ds_map_find_next based on mapData
        SetupMapIteration(mockRns, mapData, langMap);

        return mockRns;
    }

    private static void SetupMapIteration(Mock<IRNSReloaded> mockRns, Dictionary<string, string> mapData, RValue langMap) {
        var keys = new List<RValue>();
        foreach (var kvp in mapData) {
            var key = CreateStringRValue(kvp.Key);
            var value = CreateStringRValue(kvp.Value);

            keys.Add(key);

            // Mock ds_map_find_first to return the corresponding value
            mockRns.Setup(r => r.ExecuteCodeFunction("ds_map_find_first", null, null, It.Is<RValue[]>(args => args[1].Equals(key))))
                   .Returns(value);

            // Mock GetString for both key and value
            mockRns.Setup(r => r.GetString(&key)).Returns(kvp.Key);
            mockRns.Setup(r => r.GetString(&value)).Returns(kvp.Value);
        }

        // Setup ds_map_find_first to return the first key
        if (keys.Count > 0) {
            mockRns.Setup(r => r.ExecuteCodeFunction("ds_map_find_first", null, null, It.IsAny<RValue[]>()))
                   .Returns(keys[0]);
        }

        // Setup ds_map_find_next to iterate through keys, then return undefined
        var sequence = mockRns.SetupSequence(r => r.ExecuteCodeFunction("ds_map_find_next", null, null, It.IsAny<RValue[]>()));
        for (var i = 1; i < keys.Count; i++) {
            sequence.Returns(keys[i]);
        }
        sequence.Returns(new RValue { Type = RValueType.Undefined });
    }
}
