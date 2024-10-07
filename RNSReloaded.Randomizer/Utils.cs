using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces.Structs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RNSReloaded.Randomizer;
public static class Utils {

    public static RValue Undefined { get {
            RValue val = new();
            val.Type = RValueType.Undefined;
            return val;
        }
    }

    public static RValue NullCheck(RValue? value) {
        return value ?? throw new NullReferenceException("RValue was null when probably shouldn't");
    }

    public static ILoggerV1 logger = null!;
    public static void Print(string message) {
        logger.PrintMessage(message, Color.Wheat);
    }
    
}