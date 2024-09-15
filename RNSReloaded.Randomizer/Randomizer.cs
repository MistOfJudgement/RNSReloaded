using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RNSReloaded.Randomizer;
public class Randomizer {
    private Dictionary<string, string> completeMap = [];

    public Randomizer() {

    }

    public void Randomize() {
        //Get all the strings that need to be randomized
        //put them in a map ( I guess it could just be an array )
        //  optional: based on some filter
        this.LoadLanguageMap();
        this.LoadAllyData();
        this.LoadItemData();
        //shuffle said map
        //  optional: maybe this step is based on a filter
        //for each entry
        //reverse the map key to the data source
    }
    public void LoadLanguageMap() {
        //o h my god do it manually, then tidy it later
    }

    public void LoadAllyData() {

    }

    public void LoadItemData() {

    }
}

