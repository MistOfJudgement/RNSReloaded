using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RNSReloaded.Randomizer.Tests {
    internal unsafe class MockRNSReloaded : IRNSReloaded {


        IUtil IRNSReloaded.utils => throw new NotImplementedException();

        IBattlePatterns IRNSReloaded.battlePatterns => throw new NotImplementedException();

        IBattleScripts IRNSReloaded.battleScripts => throw new NotImplementedException();

        event Action? IRNSReloaded.OnReady {
            add {
                throw new NotImplementedException();
            }

            remove {
                throw new NotImplementedException();
            }
        }

        event Action<ExecuteItArguments>? IRNSReloaded.OnExecuteIt {
            add {
                throw new NotImplementedException();
            }

            remove {
                throw new NotImplementedException();
            }
        }

        unsafe RValue* IRNSReloaded.ArrayGetEntry(RValue* array, int index) {
            throw new NotImplementedException();
        }

        unsafe RValue? IRNSReloaded.ArrayGetLength(RValue* array) {
            throw new NotImplementedException();
        }

        int? IRNSReloaded.CodeFunctionFind(string name) {
            throw new NotImplementedException();
        }

        unsafe void IRNSReloaded.CreateString(RValue* value, string str) {
            throw new NotImplementedException();
        }

        unsafe RValue? IRNSReloaded.ExecuteCodeFunction(string name, CInstance* self, CInstance* other, int argc, RValue** argv) {
            throw new NotImplementedException();
        }

        unsafe RValue? IRNSReloaded.ExecuteCodeFunction(string name, CInstance* self, CInstance* other, RValue[] arguments) {
            throw new NotImplementedException();
        }

        unsafe RValue? IRNSReloaded.ExecuteScript(string name, CInstance* self, CInstance* other, int argc, RValue** argv) {
            throw new NotImplementedException();
        }

        unsafe RValue? IRNSReloaded.ExecuteScript(string name, CInstance* self, CInstance* other, RValue[] arguments) {
            throw new NotImplementedException();
        }

        unsafe RValue* IRNSReloaded.FindValue(CInstance* instance, string name) {
            throw new NotImplementedException();
        }

        unsafe CRoom* IRNSReloaded.GetCurrentRoom() {
            throw new NotImplementedException();
        }

        unsafe CInstance* IRNSReloaded.GetGlobalInstance() {
            throw new NotImplementedException();
        }

        unsafe CScript* IRNSReloaded.GetScriptData(int id) {
            throw new NotImplementedException();
        }

        unsafe string IRNSReloaded.GetString(RValue* value) {
            throw new NotImplementedException();
        }

        unsafe List<string> IRNSReloaded.GetStructKeys(RValue* value) {
            throw new NotImplementedException();
        }

        RFunctionStringRef IRNSReloaded.GetTheFunction(int id) {
            throw new NotImplementedException();
        }

        void IRNSReloaded.LimitOnlinePlay() {
            throw new NotImplementedException();
        }

        int IRNSReloaded.ScriptFindId(string name) {
            throw new NotImplementedException();
        }
    }
}
