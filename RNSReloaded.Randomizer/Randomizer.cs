using RNSReloaded.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RNSReloaded.Randomizer;

//At some point I'm going to create a wrapper class over IRNSReloaded to have safe accessor
public unsafe class Randomizer {
    private Dictionary<string, string> completeMap = [];
    private Dictionary<string, string> languageMap = [];

    public Randomizer() {

    }

    public void Randomize(
        IRNSReloaded rns
        ) {
        //Get all the strings that need to be randomized
        //put them in a map ( I guess it could just be an array )
        //  optional: based on some filter
        this.LoadLanguageMap(rns);
        this.LoadAllyData(rns);
        this.LoadItemData(rns);
        //shuffle said map
        //  optional: maybe this step is based on a filter
        //for each entry
        //reverse the map key to the data source
    }
    private void LoadLanguageMap(IRNSReloaded rns) {
        //o h my god do it manually, then tidy it later
        var global = rns.GetGlobalInstance();
        var langMap = rns.FindValue(global, "languageMap");
    }

    private void LoadAllyData(IRNSReloaded rns) {

    }

    private void LoadItemData(IRNSReloaded rns) {

    }
}

