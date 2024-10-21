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
        public unsafe GMLDSMap(string name) {
            var id = *Utils.GetGlobalValue(name);
            //var id = SafeRNS.Instance.FindValue(SafeRNS.Instance.GetGlobalInstance(), name);
            this.id = id;
        }

        public bool Exists() {
            var val = Utils.ExecuteGlobalFunction("ds_map_exists", this.id);
            if (val.HasValue) {
                return val.Value.Bool;
            }
            throw new Exception("Unexpected memory access for ds_map_exists on map " + this.id);
        }

        public RValue FindFirst() {
            var val = Utils.ExecuteGlobalFunction("ds_map_find_first", this.id);
            return Utils.NullCheck(val);
        }

        public RValue FindNext(RValue fromKey) {
            var val = Utils.ExecuteGlobalFunction("ds_map_find_next", this.id, fromKey);
            return Utils.NullCheck(val);
        }

        public RValue FindValue(RValue key) {
            var val = Utils.ExecuteGlobalFunction("ds_map_find_value", this.id, key);
            return Utils.NullCheck(val);
        }

        public void Set(RValue key, RValue value) {
            Utils.ExecuteGlobalFunction("ds_map_set", this.id, key, value);
        }

        //Doesnt work
        public GmlArray KeysToArray() {
            var val = Utils.ExecuteGlobalFunction("ds_map_keys_to_array", this.id);

            return new GmlArray(Utils.NullCheck(val));
            


        }

        //Doesn't work
        public GmlArray ValuesToArray() {
            var val = Utils.NullCheck(Utils.ExecuteGlobalFunction("ds_map_values_to_array", this.id));
            return new GmlArray (Utils.NullCheck(val));
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
