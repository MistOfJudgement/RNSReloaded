using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RNSReloaded.Randomizer {
    public class GMLDSMap {
        private RValue id {  get; init; }

        public GMLDSMap(RValue id) {
            this.id = id;
        }
        public GMLDSMap(string name) {
            var id = Utils.NullCheck(SafeRNS.Instance.GetGlobalValue(name));
            //var id = SafeRNS.Instance.FindValue(SafeRNS.Instance.GetGlobalInstance(), name);
            this.id = id;
        }

        public bool Exists() {
            var val = SafeRNS.Instance.ExecuteGlobalFunction("ds_map_exists", this.id);
            if (val.HasValue) {
                return val.Value.Bool;
            }
            throw new Exception("Unexpected memory access for ds_map_exists on map " + this.id);
        }

        public RValue FindFirst() {
            var val =  SafeRNS.Instance.ExecuteGlobalFunction("ds_map_find_first", this.id);
            return Utils.NullCheck(val);
        }

        public RValue FindNext(RValue fromKey) {
            var val = SafeRNS.Instance.ExecuteGlobalFunction("ds_map_find_next", this.id, fromKey);
            return Utils.NullCheck(val);
        }

        public RValue FindValue(RValue key) {
            var val = SafeRNS.Instance.ExecuteGlobalFunction("ds_map_find_value", this.id, key);
            return Utils.NullCheck(val);
        }

        //Doesnt work
        public RValue[] KeysToArray() {
            var val = Utils.NullCheck(SafeRNS.Instance.ExecuteGlobalFunction("ds_map_keys_to_array", this.id));
            Utils.Print(val.ToString());
            var lengthVal = SafeRNS.Instance.ArrayGetLength(val);
            Utils.Print(lengthVal.ToString());
            var length = (int) Utils.NullCheck(SafeRNS.Instance.ArrayGetLength(val)).Real;
            Utils.Print(length.ToString());
            var output = new RValue[length];
            for (int i = 0; i < length; i++) {
                output[i] = Utils.NullCheck(SafeRNS.Instance.ArrayGetEntry(val, i));
            }
            return output;
        }

        //Doesn't work
        public RValue[] ValuesToArray() {
            var val = Utils.NullCheck(SafeRNS.Instance.ExecuteGlobalFunction("ds_map_values_to_array", this.id));
            var length = (int) Utils.NullCheck(SafeRNS.Instance.ArrayGetLength(val)).Real;
            var output = new RValue[length];
            for (int i = 0; i < length; i++) {
                output[i] = Utils.NullCheck(SafeRNS.Instance.ArrayGetEntry(val, i));
            }
            return output;
        }

        public Dictionary<RValue, RValue> Collect() {
            var output = new Dictionary<RValue, RValue>();

            var current = this.FindFirst();
            while (current.Type != RValueType.Undefined) {
                var val = this.FindValue(current);
                output[current] = val;
                current = this.FindNext(current);
            }

            return output;
        }
    }
}
