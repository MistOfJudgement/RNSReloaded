using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RNSReloaded.Randomizer;
public unsafe class GmlArray {
	private RValue arrayPointer {get; init;}
	
	private RValue*[] arrayValues;
	public GmlArray(RValue val) {
		if (val.Type != RValueType.Array) {
            throw new Exception("Not an array");
		}
		this.arrayPointer = val;
        this.arrayValues = [];
		this.load();
	}

	public RValue this[int index] {
		get {
			return *this.arrayValues[index];
		}
		set {
			*this.arrayValues[index] = value;
		}
	}

	public unsafe int length() {
		var temp = this.arrayPointer;
        var smth = Utils.NullCheck(IRNSReloaded.Instance.ArrayGetLength(&temp));

		if(smth.Type != RValueType.Real) {
			throw new Exception("Unknown length value returned on array");
		}

		return (int)smth.Real;
	}
	
	private unsafe RValue* GetPointer(int index) {
		var temp = this.arrayPointer;
        return IRNSReloaded.Instance.ArrayGetEntry(&temp, index);
	}

	public unsafe void load() {
		var length = this.length();
		this.arrayValues = new RValue*[length];
		for(int i = 0; i < length; i++) {
			this.arrayValues[i] = this.GetPointer(i);
		}
	}
}

