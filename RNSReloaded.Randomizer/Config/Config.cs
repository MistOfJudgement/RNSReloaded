using System.ComponentModel;

namespace RNSReloaded.Randomizer.Config;

public class Config : Configurable<Config> {

    [Category("TextRandomizer"), DefaultValue(false)]
    [Description("Checked if strings should only be swapped with strings of the same category")]
    public bool RandomizeWithinCategory { get; set; } = true;

    [Category("TextRandomizer"), DefaultValue(true)]
    public bool RandTextEffects { get; set; } = true;

    [Category("TextRandomizer"), DefaultValue(true)]
    public bool RandTextHBS { get; set; } = false;

    [Category("TextRandomizer"), DefaultValue(true)]
    public bool RandTextNames { get; set; } = false;

    [Category("TextRandomizer"), DefaultValue(true)]
    public bool RandTextDesc { get; set; } = false;

    [Category("TextRandomizer"), DefaultValue(true)]
    public bool RandTextTitles { get; set; } = false;

    [Category("TextRandomizer"), DefaultValue(true)]
    public bool RandTextMenus {  get; set; } = false;

}
