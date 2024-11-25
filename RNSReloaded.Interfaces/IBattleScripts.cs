using RNSReloaded.Interfaces.Structs;

namespace RNSReloaded.Interfaces;

public unsafe interface IBattleScripts {
    public const int FLAG_HOLMGANG      = 0b0001;
    public const int FLAG_NO_POSITIONAL = 0b0010;
    public const int FLAG_NO_TARGET     = 0b0100;

    public void end(CInstance* self, CInstance* other);

    public void heal(CInstance* self, CInstance* other, double amount);
    public bool health_threshold(CInstance* self, CInstance* other, double threshold);
    public void make_number_warning(CInstance* self, CInstance* other, int x, int y, int displayNumber, int duration);
    public void make_warning_colormatch(CInstance* self, CInstance* other, int x, int y, int radius, int element, int duration);
    public void make_warning_colormatch_burst(CInstance* self, CInstance* other, int x, int y, int radius, int element);
    public void make_warning_colormatch_targ(CInstance* self, CInstance* other, int playerId, int radius, int element, int duration);
    public void move_character(CInstance* self, CInstance* other, double x, double y, int moveTime);
    public void move_character_absolute(CInstance* self, CInstance* other, double x, double y, int moveTime);
    public void set_special_flags(CInstance* self, CInstance* other, int flags);

    public void pattern_set_color_colormatch(CInstance* self, CInstance* other, int color);

    public void pattern_set_drawlayer(CInstance* self, CInstance* other, int drawlayerId);

    public void pattern_set_projectile_key(CInstance* self, CInstance* other, string key, string sfxset);

    public void phase_pattern_remove(CInstance* self, CInstance* other);

    public void sound(CInstance* self, CInstance* other, double arg1, double arg2);

    public void sound_x(CInstance* self, CInstance* other, int x);

    public bool time(CInstance* self, CInstance* other, int time);

    public bool time_repeating(CInstance* self, CInstance* other, int loopOffset, int loopLength);

    public bool time_repeat_times(CInstance* self, CInstance* other, int startTime, int timeBetween, int times);

    public int[] order_random(CInstance* self, CInstance* other, bool excludeKO, params int[] groupings);

    public void pattern_deal_damage_enemy_subtract(CInstance* self, CInstance* other, int teamId, int playerId, int damageAmount);
    public void warning_msg_pos(CInstance* self, CInstance* other, int x, int y, string msg, int warnMsg, int duration);
    public void warning_msg_t(CInstance* self, CInstance* other, int playerId, string msg, int warnMsg, int duration);


    public RValue sbgv(CInstance* self, CInstance* other, string name, RValue defaultVal);
    public RValue sbsv(CInstance* self, CInstance* other, string name, RValue toSave);
}
