using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System.Drawing;

namespace RNSReloaded.FullmoonArsenal {
    
    internal unsafe class TasshaFight : CustomFight {

        private IHook<ScriptDelegate> starburstHook;
        private IHook<ScriptDelegate>? jumpCleaveHook;
        private IHook<ScriptDelegate>? bubbleLineHook;

        public TasshaFight(IRNSReloaded rnsReloaded, ILoggerV1 logger, IReloadedHooks hooks) :
            base(rnsReloaded, logger, hooks, "bp_wolf_snowfur0_s", "bp_wolf_snowfur0_pt2") {
            this.playerRng = new Random();
            // Regualar fight = setup
            // pt2 = final phase

            var script = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("bp_wolf_snowfur0_pt3") - 100000);
            this.starburstHook =
                hooks.CreateHook<ScriptDelegate>(this.StarburstPhase, script->Functions->Function);
            this.starburstHook.Activate();
            this.starburstHook.Enable();

            script = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("bp_wolf_snowfur0_pt4") - 100000);
            this.jumpCleaveHook =
                hooks.CreateHook<ScriptDelegate>(this.JumpCleavePhase, script->Functions->Function);
            this.jumpCleaveHook.Activate();
            this.jumpCleaveHook.Enable();

            script = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("bp_wolf_snowfur0_pt5") - 100000);
            this.bubbleLineHook =
                hooks.CreateHook<ScriptDelegate>(this.BubbleLinePhase, script->Functions->Function);
            this.bubbleLineHook.Activate();
            this.bubbleLineHook.Enable();
        }


        private double myX = 0, myY = 0;
        private int seed;
        private Random playerRng;
        private (double x, double y) posSnapshot = (0, 0);

        private int DashToPlayer(CInstance* self, CInstance* other, int startTime, int target) {
            if (this.scrbp.time(self, other, startTime)) {
                double playerX = this.utils.GetPlayerVar(target, "distMovePrevX")->Real;
                double playerY = this.utils.GetPlayerVar(target, "distMovePrevY")->Real;

                // If more than 150 pixels away, then move
                (double x, double y) vec = (this.myX - playerX, this.myY - playerY);
                double vecMag = Math.Sqrt(vec.x * vec.x + vec.y * vec.y);

                if (vecMag > 100) {
                    if (playerX == this.myX) {
                        this.myX++;
                    }
                    (double x, double y) vec_u = (vec.x / vecMag, vec.y / vecMag);

                    this.bp.move_position_synced(self, other, duration: 500, position: (playerX + vec_u.x * 100, playerY + vec_u.y * 100));
                    this.myX = playerX + vec_u.x * 100;
                    this.myY = playerY + vec_u.y * 100;
                }

            }
            return 500;
        }
        private int DashCleave(CInstance* self, CInstance* other, int startTime, int target) {
            int time = startTime;
            time += this.DashToPlayer(self, other, time, target);
            if (this.scrbp.time(self, other, time)) {
                double playerX = this.utils.GetPlayerVar(target, "distMovePrevX")->Real;
                double playerY = this.utils.GetPlayerVar(target, "distMovePrevY")->Real;
                this.posSnapshot = (playerX, playerY);
            }
            time += 100;
            if (this.scrbp.time(self, other, time)) {
                (double x, double y) vec = (this.myX - this.posSnapshot.x, this.myY - this.posSnapshot.y);
                int cleaveAngle = (int) (Math.Atan2(vec.y, vec.x) * 180 / Math.PI) + 180;

                this.bp.cleave_fixed(self, other, spawnDelay: 600, positions: [((this.myX, this.myY), cleaveAngle)]);
            }
            time += 600;
            return time - startTime;
        }

        private int DashCleaveWarn(CInstance* self, CInstance* other, int startTime, int target) {
            if (this.scrbp.time(self, other, startTime)) {
                this.bp.thorns_fixed(self, other, warningDelay: 0, warnMsg: 0, spawnDelay: 1500, radius: 150, targetMask: 1 << target, position: (this.myX, this.myY));
            }
            return 1500 + this.DashCleave(self, other, startTime + 1500, target);
        }

        private Dictionary<int, ((double x, double y) pos, int rot)[]> starburstCached = new Dictionary<int, ((double x, double y) pos, int rot)[]>();
        private int StarburstLaser(CInstance* self, CInstance* other, int startTime, int target, int numLasers = 5, int spawnDelay = 3000, int eraseDelay = 5000, (double x, double y)? posOverride = null) {
            if (this.scrbp.time(self, other, startTime)) {
                double playerX = this.utils.GetPlayerVar(target, "distMovePrevX")->Real;
                double playerY = this.utils.GetPlayerVar(target, "distMovePrevY")->Real;
                if (posOverride.HasValue) {
                    playerX = posOverride.Value.x;
                    playerY = posOverride.Value.y;
                }
                this.starburstCached[target] = [];
                for (int i = 0; i < numLasers; i++) {
                    int rot = i * 180 / numLasers;
                    var warnDelay = i * 100;

                    double slope = Math.Tan(((double) rot) / 180 * Math.PI);
                    (double x, double y) coords;
                    if (Math.Abs(slope) <= 0.01) {
                        coords = (-50, playerY);
                    } else if (Math.Abs(slope) > 1e5) {
                        coords = (playerX, -50);
                    } else {
                        coords = (playerX - (playerY + 50) / slope, -50);
                    }

                    this.bp.ray_single(self, other,
                        warningDelay: warnDelay,
                        spawnDelay: spawnDelay,
                        eraseDelay: spawnDelay,
                        width: 5,
                        position: coords,
                        angle: rot
                    );
                    this.starburstCached[target] = this.starburstCached[target].Concat([(coords, rot)]).ToArray();
                }
            }
            // We split these lasers up and do this caching because otherwise we send too many patterns
            // in a single frame which tends to break peoples' games
            if (this.scrbp.time(self, other, startTime + spawnDelay)) {
                foreach (var laser in this.starburstCached[target]) {
                    this.bp.ray_single(self, other,
                        warningDelay: 0,
                        spawnDelay: 0,
                        eraseDelay: eraseDelay - spawnDelay,
                        width: 100,
                        position: laser.pos,
                        angle: laser.rot
                    );
                }
            }
            return eraseDelay;
        }

        private int StarburstRotate(CInstance* self, CInstance* other, int startTime, int target, int numLasers = 5, int spawnDelay = 4000, int eraseDelay = 8000, int rot = 90, (double x, double y)? posOverride = null) {
            int time = startTime;
            if (this.scrbp.time(self, other, time)) {
                double playerX = this.utils.GetPlayerVar(target, "distMovePrevX")->Real;
                double playerY = this.utils.GetPlayerVar(target, "distMovePrevY")->Real;
                this.posSnapshot = (playerX, playerY);
                if (posOverride.HasValue) {
                    this.posSnapshot = posOverride.Value;
                }
            }
            this.StarburstLaser(self, other, time, target, numLasers: numLasers, spawnDelay: spawnDelay, eraseDelay: spawnDelay, posOverride: posOverride);
            if (this.scrbp.time(self, other, time + numLasers * 100)) {
                this.bp.ray_spinfast(self, other,
                    warningDelay: 0,
                    spawnDelay: spawnDelay,
                    eraseDelay: spawnDelay,
                    width: 10,
                    angle: rot > 0 ? 1 : -1,
                    position: this.posSnapshot,
                    rot: 0,
                    numLasers: 0,
                    warningRadius: 100
                );
            }
            time += spawnDelay;
            if (this.scrbp.time(self, other, time)) {
                this.bp.ray_spinfast(self, other,
                    warningDelay: 0,
                    spawnDelay: 0,
                    eraseDelay: eraseDelay - spawnDelay,
                    width: 100,
                    angle: rot,
                    position: this.posSnapshot,
                    rot: 0,
                    numLasers: numLasers * 2,
                    warningRadius: 100
                );
            }
            time += eraseDelay - spawnDelay;

            return time - startTime;
        }
        private int BubbleLine(CInstance* self, CInstance* other, int startTime, int bubbleDuration, int skipPercent = 0) {
            int time = startTime;
            bool startLeft = this.rng.Next(0, 2) == 1;
            if (this.scrbp.time(self, other, time)) {
                var x0 = startLeft ? 50 : 1920 - 50;
                var x1 = startLeft ? 1920 - 50 : 50;
                this.bp.move_position_synced(self, other, duration: 1000, position: (x0, 1080/2));
                this.bp.move_position_synced(self, other, spawnDelay: 2000, duration: 667, position: (x1, 1080/2));
                this.bp.move_position_synced(self, other, spawnDelay: 3000, duration: 333, position: (1920 / 2, 1080 / 2));
                this.myX = 1920 / 2;
                this.myY = 1080 / 2;
                this.bp.ray_single(self, other, warningDelay: 500, spawnDelay: 3000, eraseDelay: 3000, width: 5, position: (-50, 1080/2));
            }
            time += 2500;
            if (this.scrbp.time(self, other, time)) {
                this.bp.gravity_pull_temporary(self, other, eraseDelay: bubbleDuration);
            }
            time += 500;
            if (this.scrbp.time(self, other, time)) {
                this.bp.ray_single(self, other, spawnDelay: 0, eraseDelay: bubbleDuration, width: 150, position: (-50, 1080 / 2));
            }
            if (this.scrbp.time_repeat_times(self, other, time, 500, bubbleDuration / 500)) {
                if (this.rng.Next(0, 100) >= skipPercent) {
                    switch (this.rng.Next(0, 3)) {
                        case 0:
                            int x = this.rng.Next(-150, -10);
                            this.bp.light_line(self, other, spawnDelay: 0, position: (x, 1080 / 2), angle: 90, spd: 1, lineLength: 2100, numBullets: this.rng.Next(5, 8), type: 0);
                            this.bp.light_line(self, other, spawnDelay: 0, position: (x, 1080 / 2), angle: -90, spd: 1, lineLength: 2100, numBullets: this.rng.Next(5, 8), type: 0);
                            break;
                        case 1:
                            x = this.rng.Next(-150, -10);
                            this.bp.light_line(self, other, spawnDelay: 0, position: (x, 1080 / 2), angle: 90, spd: 4, lineLength: 2100, numBullets: this.rng.Next(6, 11), type: 1);
                            this.bp.light_line(self, other, spawnDelay: 0, position: (x, 1080 / 2), angle: -90, spd: 4, lineLength: 2100, numBullets: this.rng.Next(6, 11), type: 1);
                            break;
                        case 2:
                            x = this.rng.Next(-150, -10);
                            this.bp.fire2_line(self, other, spawnDelay: 0, position: (x, 1080 / 2), angle: 90, spd: 2, lineLength: 2100, numBullets: this.rng.Next(4, 12));
                            this.bp.fire2_line(self, other, spawnDelay: 0, position: (x, 1080 / 2), angle: -90, spd: 2, lineLength: 2100, numBullets: this.rng.Next(4, 12));
                            break;
                    }
                }
            }
            return time + bubbleDuration - startTime;
        }

        private int BubbleLineRotating(CInstance* self, CInstance* other, int startTime, int bubbleDuration, int rot, int skipPercent = 0) {
            int time = startTime;

            bool startLeft = this.rng.Next(0, 2) == 1;
            if (this.scrbp.time(self, other, startTime)) {
                var x0 = startLeft ? 50 : 1920 - 50;
                var x1 = startLeft ? 1920 - 50 : 50;
                this.bp.move_position_synced(self, other, duration: 1000, position: (x0, 1080 / 2));
                this.bp.move_position_synced(self, other, spawnDelay: 2000, duration: 1000, position: (x1, 1080 / 2));
                this.bp.move_position_synced(self, other, spawnDelay: 3000, duration: 333, position: (1920 / 2, 1080 / 2));
                this.myX = 1920 / 2;
                this.myY = 1080 / 2;
                this.bp.ray_single(self, other, warningDelay: 500, spawnDelay: 3000, eraseDelay: 3000, width: 5, position: (-50, 1080 / 2));
                this.bp.ray_spinfast(self, other,
                    warningDelay: 0,
                    spawnDelay: 3000,
                    numLasers: 0,
                    angle: rot > 0 ? 1 : -1,
                    rot: 0,
                    width: 100,
                    eraseDelay: 3000,
                    warningRadius: 100,
                    position: (1920 / 2, 1080 / 2)
                );
            }
            time += 2500;
            if (this.scrbp.time(self, other, time)) {
                this.bp.gravity_pull_temporary(self, other, eraseDelay: bubbleDuration);
            }
            time += 500;
            if (this.scrbp.time(self, other, time)) {
                this.bp.ray_spinfast(self, other,
                    warningDelay: 0,
                    spawnDelay: 0,
                    numLasers: 2,
                    angle: rot,
                    rot: 0,
                    width: 100,
                    eraseDelay: bubbleDuration,
                    warningRadius: 100,
                    position: (1920 / 2, 1080 / 2)
                );
            }
            if (this.scrbp.time_repeat_times(self, other, time, 500, bubbleDuration / 500)) {
                int thisBattleTime = (int) this.rnsReloaded.FindValue(self, "patternExTime")->Real;
                int thisMechTime = thisBattleTime - time;
                double percentThroughMech = ((float) thisMechTime) / bubbleDuration;
                int rotEdit = (int) (percentThroughMech * rot);

                int lengthAdd = this.rng.Next(0, 200);
                var pos = (1920 / 2 + this.rng.Next(0, 20), 1080 / 2 + this.rng.Next(0, 20));
                if (this.rng.Next(0, 100) >= skipPercent) {
                    switch (this.rng.Next(0, 3)) {
                        case 0:
                            this.bp.light_line(self, other, spawnDelay: 0, position: pos, angle: 90 + rotEdit, lineAngle: rotEdit, spd: 1, lineLength: 1100 + lengthAdd, numBullets: this.rng.Next(3, 8), type: 0);
                            this.bp.light_line(self, other, spawnDelay: 0, position: pos, angle: -90 + rotEdit, lineAngle: rotEdit, spd: 1, lineLength: 1100 + lengthAdd, numBullets: this.rng.Next(3, 8), type: 0);

                            this.bp.light_line(self, other, spawnDelay: 0, position: pos, angle: 90 + rotEdit, lineAngle: rotEdit + 180, spd: 1, lineLength: 1100 + lengthAdd, numBullets: this.rng.Next(3, 8), type: 0);
                            this.bp.light_line(self, other, spawnDelay: 0, position: pos, angle: -90 + rotEdit, lineAngle: rotEdit + 180, spd: 1, lineLength: 1100 + lengthAdd, numBullets: this.rng.Next(3, 8), type: 0);
                            break;
                        case 1:
                            this.bp.light_line(self, other, spawnDelay: 0, position: pos, angle: 90 + rotEdit, lineAngle: rotEdit, spd: 4, lineLength: 1100 + lengthAdd, numBullets: this.rng.Next(5, 9), type: 1);
                            this.bp.light_line(self, other, spawnDelay: 0, position: pos, angle: -90 + rotEdit, lineAngle: rotEdit, spd: 4, lineLength: 1100 + lengthAdd, numBullets: this.rng.Next(5, 9), type: 1);

                            this.bp.light_line(self, other, spawnDelay: 0, position: pos, angle: 90 + rotEdit, lineAngle: rotEdit + 180, spd: 4, lineLength: 1100 + lengthAdd, numBullets: this.rng.Next(5, 9), type: 1);
                            this.bp.light_line(self, other, spawnDelay: 0, position: pos, angle: -90 + rotEdit, lineAngle: rotEdit + 180, spd: 4, lineLength: 1100 + lengthAdd, numBullets: this.rng.Next(5, 9), type: 1);
                            break;
                        case 2:
                            this.bp.fire2_line(self, other, spawnDelay: 0, position: pos, angle: 90 + rotEdit, lineAngle: rotEdit, spd: 2, lineLength: 1100 + lengthAdd, numBullets: this.rng.Next(4, 11));
                            this.bp.fire2_line(self, other, spawnDelay: 0, position: pos, angle: -90 + rotEdit, lineAngle: rotEdit, spd: 2, lineLength: 1100 + lengthAdd, numBullets: this.rng.Next(4, 11));

                            this.bp.fire2_line(self, other, spawnDelay: 0, position: pos, angle: 90 + rotEdit, lineAngle: rotEdit + 180, spd: 2, lineLength: 1100 + lengthAdd, numBullets: this.rng.Next(4, 11));
                            this.bp.fire2_line(self, other, spawnDelay: 0, position: pos, angle: -90 + rotEdit, lineAngle: rotEdit + 180, spd: 2, lineLength: 1100 + lengthAdd, numBullets: this.rng.Next(4, 11));
                            break;
                    }
                }
            }

            return time + bubbleDuration - startTime;
        }

        private int VerticalLasers(CInstance* self, CInstance* other, int startTime, int warnDelay = 4000, int eraseDelay = 3000) {
            if (this.scrbp.time(self, other, startTime)) {
                this.bp.ray_multi_v(self, other, spawnDelay: warnDelay, eraseDelay: warnDelay, width: 5, positions: [
                    (1920/6 * -3, -20),
                    (1920/6 * -2, -20),
                    (1920/6 * -1, -20),
                    (1920/6 * 0, -20),
                    (1920/6 * 1, -20),
                    (1920/6 * 2, -20),
                    (1920/6 * 3, -20),
                ]);
            }
            if (this.scrbp.time(self, other, startTime + warnDelay)) {
                this.bp.ray_multi_v(self, other, spawnDelay: 0, eraseDelay: eraseDelay, width: 40, positions: [
                    (1920/6 * -3, -20),
                    (1920/6 * -2, -20),
                    (1920/6 * -1, -20),
                    (1920/6 * 0, -20),
                    (1920/6 * 1, -20),
                    (1920/6 * 2, -20),
                    (1920/6 * 3, -20),
                ]);
            }
            return warnDelay + eraseDelay;
        }

        private int LimitCut(CInstance* self, CInstance* other, int startTime) {
            int time = startTime;
            if (this.scrbp.time(self, other, time)) {
                this.playerTargets = [0, 1, 2, 3];
                // if <4p make sure no crash by just targeting player 0
                this.playerTargets = this.playerTargets.Select(x => x >= this.utils.GetNumPlayers() ? 0 : x).ToArray();
                this.rng.Shuffle(this.playerTargets);
                // Give the classic limit cut markers
                this.bp.apply_hbs_synced(self, other, hbs: "hbs_group_0", hbsDuration: 6500, targetMask: 1 << this.playerTargets[0]);
                this.bp.apply_hbs_synced(self, other, hbs: "hbs_group_1", hbsDuration: 6500, targetMask: 1 << this.playerTargets[1]);
                this.bp.apply_hbs_synced(self, other, hbs: "hbs_group_2", hbsDuration: 6500, targetMask: 1 << this.playerTargets[2]);
                this.bp.apply_hbs_synced(self, other, hbs: "hbs_group_3", hbsDuration: 6500, targetMask: 1 << this.playerTargets[3]);

                this.bp.thorns_fixed(self, other, warningDelay: 0, warnMsg: 0, spawnDelay: 1500, radius: 150, targetMask: 1 << this.playerTargets[0], position: (this.myX, this.myY));
                this.bp.thorns(self, other, warningDelay: 0, warnMsg: 0, spawnDelay: 2700, radius: 150, targetMask: (1 << this.playerTargets[0]) | (1 << this.playerTargets[1]));
                this.bp.thorns(self, other, warningDelay: 0, warnMsg: 0, spawnDelay: 4300, radius: 150, targetMask: (1 << this.playerTargets[1]) | (1 << this.playerTargets[2]));
                this.bp.thorns(self, other, warningDelay: 0, warnMsg: 0, spawnDelay: 5500, radius: 150, targetMask: (1 << this.playerTargets[2]) | (1 << this.playerTargets[3]));
            }
            time += 2000;
            time += this.DashCleave(self, other, time, this.playerTargets[0]); // 1200 ms
            time += this.DashToPlayer(self, other, time, this.playerTargets[1]); // 500 ms
            this.StarburstLaser(self, other, time, this.playerTargets[1], spawnDelay: 1100, eraseDelay: 4700);
            time += 1100;
            time += this.DashCleave(self, other, time, this.playerTargets[2]); // 1200 ms
            time += this.DashToPlayer(self, other, time, this.playerTargets[1]); // 500 ms
            time += this.StarburstLaser(self, other, time, this.playerTargets[3], spawnDelay: 1000, eraseDelay: 2000);
            return time - startTime;
        }

        private int StartRegularPhase(CInstance* self, CInstance* other, int startTime) {
            if (this.scrbp.time(self, other, startTime)) {
                this.scrbp.phase_pattern_remove(self, other);
                this.scrbp.heal(self, other, 1);

                this.bp.move_position_synced(self, other, duration: 1000, position: (1920 / 2, 1080 / 2));
                this.myX = 1920 / 2;
                this.myY = 1080 / 2;
            }
            return 2000;
        }
        private bool PhaseChange(CInstance* self, CInstance* other, double hpThreshold) {
            this.logger.PrintMessage("Checking for phase", this.logger.ColorRed);
            if (this.scrbp.health_threshold(self, other, hpThreshold)) {
                this.logger.PrintMessage("Changing phase", this.logger.ColorRed);

                string phase = "bp_wolf_snowfur0_pt2";
                if (this.phasesRemaining.Count > 0) {
                    phase = this.phasesRemaining.Pop();
                }
                RValue[] args = [new RValue(this.rnsReloaded.ScriptFindId(phase))];
                this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);
                this.scrbp.end(self, other);
                return true;
            }
            return false;
        }

        int[] playerTargets = [0, 0, 0, 0];
        Stack<string> phasesRemaining = [];
        public override RValue* FightDetour(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        ) {
            this.logger.PrintMessage("Intro Phase", this.logger.ColorRed);

            int time = 0;
            if (this.scrbp.time(self, other, time)) {
                this.bp.move_position_synced(self, other, duration: 1000, position: (1920 / 2, 1080 / 2));
                this.myX = 1920 / 2;
                this.myY = 1080 / 2;
                this.seed = new Random().Next();
                this.playerTargets = [0, 1, 2, 3];
                // if <4p make sure no crash by just targeting player 0
                this.playerTargets = this.playerTargets.Select(x => x >= this.utils.GetNumPlayers() ? 0 : x).ToArray();
                this.rng.Shuffle(this.playerTargets);
                string[] phases = ["bp_wolf_snowfur0_pt3", "bp_wolf_snowfur0_pt4", "bp_wolf_snowfur0_pt5"];
                this.rng.Shuffle(phases);
                this.phasesRemaining = new Stack<string>(phases);

                // Testing
                this.phasesRemaining.Push("bp_wolf_snowfur0_pt5");
                this.scrbp.set_special_flags(self, other, IBattleScripts.FLAG_HOLMGANG);
            }
            if (this.scrbp.time_repeating(self, other, 0, 500)) {
                this.PhaseChange(self, other, 1);
            }

            return returnValue;

            //time += this.DashCleaveWarn(self, other, time, this.playerTargets[0]);
            //time += 500;
            //time += this.StarburstLaser(self, other, time, this.playerTargets[1], spawnDelay: 1000, eraseDelay: 2000);
            //time += this.DashCleaveWarn(self, other, time, this.playerTargets[2]);
            //time += 500;
            //time += this.StarburstLaser(self, other, time, this.playerTargets[3], spawnDelay: 1000, eraseDelay: 2000);
            //time += 1000;
            //this.StarburstLaser(self, other, time, this.playerTargets[0]);
            //this.StarburstLaser(self, other, time, this.playerTargets[1]);
            //this.StarburstLaser(self, other, time, this.playerTargets[2]);
            //time += this.StarburstLaser(self, other, time, this.playerTargets[3]);
            //time += this.LimitCut(self, other, time);

            time += this.VerticalLasers(self, other, time, 3000, 7000);
            this.BubbleLine(self, other, time - 10000, 10000);
            this.DashCleaveWarn(self, other, time - 5000, this.playerRng.Next(0, this.utils.GetNumPlayers()));
            time += this.VerticalLasers(self, other, time, 3000, 7000);
            this.DashCleaveWarn(self, other, time - 5000, this.playerRng.Next(0, this.utils.GetNumPlayers()));
            this.BubbleLine(self, other, time - 10000, 10000);
            time += this.VerticalLasers(self, other, time, 3000, 7000);
            this.BubbleLine(self, other, time - 10000, 10000);
            this.DashCleaveWarn(self, other, time - 5000, this.playerRng.Next(0, this.utils.GetNumPlayers()));

            // Rotating laser and then occasional FAST starburst lasers to dodge during it
            //   with alternating rotation
            // fieldlimit 1 player in place, force other 3 to not cleave/spread them

            this.logger.PrintMessage("Time: " + time, this.logger.ColorRed);
            if (this.scrbp.time(self, other, time)) {
                this.bp.enrage(self, other);
            }
            return returnValue;
        }

        private int Rem0Callback(CInstance* self, CInstance* other, int startTime) {
            int time = startTime;
            // Yeet into place
            if (this.scrbp.time(self, other, time)) {
                this.rng.Shuffle(this.playerTargets);
                // Top half
                this.bp.fieldlimit_rectangle_temporary(self, other, position: (960, 270), width: 5, height: 5, color: IBattlePatterns.FIELDLIMIT_RED, targetMask: 1 << this.playerTargets[0], eraseDelay: 2000);
                this.bp.apply_hbs_synced(self, other, delay: 0, hbs: "hbs_group_0", hbsDuration: 66000, targetMask: 1 << this.playerTargets[0]);
                // Bottom half
                this.bp.fieldlimit_rectangle_temporary(self, other, position: (960, 780), width: 5, height: 5, color: IBattlePatterns.FIELDLIMIT_BLUE, targetMask: 1 << this.playerTargets[1], eraseDelay: 2000);
                this.bp.apply_hbs_synced(self, other, delay: 0, hbs: "hbs_group_2", hbsDuration: 66000, targetMask: 1 << this.playerTargets[1]);

                // Left half
                this.bp.fieldlimit_rectangle_temporary(self, other, position: (480, 540), width: 5, height: 5, color: IBattlePatterns.FIELDLIMIT_YELLOW, targetMask: 1 << this.playerTargets[2], eraseDelay: 2000);
                this.bp.apply_hbs_synced(self, other, delay: 0, hbs: "hbs_group_3", hbsDuration: 66000, targetMask: 1 << this.playerTargets[2]);

                // Right half
                this.bp.fieldlimit_rectangle_temporary(self, other, position: (1410, 540), width: 5, height: 5, color: IBattlePatterns.FIELDLIMIT_PURPLE, targetMask: 1 << this.playerTargets[3], eraseDelay: 2000);
                this.bp.apply_hbs_synced(self, other, delay: 0, hbs: "hbs_group_1", hbsDuration: 66000, targetMask: 1 << this.playerTargets[3]);
            }
            time += 3000;

            // Setup the long fieldlimits
            if (this.scrbp.time(self, other, time)) {
                // Top half
                this.bp.fieldlimit_rectangle_temporary(self, other,
                    position: (960, 270),
                    width: 1840,
                    height: 470,
                    color: IBattlePatterns.FIELDLIMIT_RED,
                    targetMask: 1 << this.playerTargets[0],
                    eraseDelay: 66000
                );
                // Bottom half
                this.bp.fieldlimit_rectangle_temporary(self, other,
                    position: (960, 780),
                    width: 1840,
                    height: 470,
                    color: IBattlePatterns.FIELDLIMIT_BLUE,
                    targetMask: 1 << this.playerTargets[1],
                    eraseDelay: 66000
                );
                // Left half
                this.bp.fieldlimit_rectangle_temporary(self, other,
                    position: (480, 540),
                    width: 900,
                    height: 1020,
                    color: IBattlePatterns.FIELDLIMIT_YELLOW,
                    targetMask: 1 << this.playerTargets[2],
                    eraseDelay: 66000
                );
                // Right half
                this.bp.fieldlimit_rectangle_temporary(self, other,
                    position: (1410, 540),
                    width: 900,
                    height: 1020,
                    color: IBattlePatterns.FIELDLIMIT_PURPLE,
                    targetMask: 1 << this.playerTargets[3],
                    eraseDelay: 66000
                );
            }
            time += 6000;

            // Color match
            if (this.scrbp.time_repeat_times(self, other, time, 20000, 3)) {
                this.scrbp.order_random(self, other, false, 2, 2);
                var orderBin = this.rnsReloaded.FindValue(self, "orderBin");
                var group0 = this.rnsReloaded.ArrayGetEntry(orderBin, 0);
                var group1 = this.rnsReloaded.ArrayGetEntry(orderBin, 1);

                this.bp.colormatch(self, other,
                    warningDelay: 3000,
                    warnMsg: 2,
                    spawnDelay: 19000,
                    radius: 200,
                    targetMask: (int) group0->Real,
                    color: IBattlePatterns.COLORMATCH_BLUE
                );
                this.bp.colormatch(self, other,
                    warningDelay: 3000,
                    warnMsg: 2,
                    spawnDelay: 19000,
                    radius: 200,
                    targetMask: (int) group1->Real,
                    color: IBattlePatterns.COLORMATCH_RED
                );
            }

            for (int i = 0; i < 12; i++) {
                time += this.StarburstLaser(self, other, time, this.playerTargets[i % 4], spawnDelay: 3000, eraseDelay: 5000);
            }
            return time - startTime;
        }
        // "pt3", actual time is ~90-100s
        public RValue* StarburstPhase(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        ) {
            this.logger.PrintMessage("Starburst phase", this.logger.ColorRed);

            int time = 0;

            this.playerRng = new Random(this.seed);
            time += this.StartRegularPhase(self, other, time);

            time += this.StarburstLaser(self, other, time, target: this.playerTargets[0], numLasers: 3, spawnDelay: 2000, eraseDelay: 3000);
            time += this.StarburstLaser(self, other, time, target: this.playerTargets[1], numLasers: 4, spawnDelay: 2000, eraseDelay: 3000);
            time += this.StarburstLaser(self, other, time, target: this.playerTargets[2], numLasers: 5, spawnDelay: 2000, eraseDelay: 3000);
            time += this.StarburstLaser(self, other, time, target: this.playerTargets[3], numLasers: 6, spawnDelay: 2000, eraseDelay: 3000);
            
            this.StarburstLaser(self, other, time, target: this.playerTargets[0], numLasers: 3, spawnDelay: 2000, eraseDelay: 3000);
            this.StarburstLaser(self, other, time, target: this.playerTargets[1], numLasers: 3, spawnDelay: 2000, eraseDelay: 3000);
            this.StarburstLaser(self, other, time, target: this.playerTargets[2], numLasers: 3, spawnDelay: 2000, eraseDelay: 3000);
            time += this.StarburstLaser(self, other, time, target: this.playerTargets[3], numLasers: 3, spawnDelay: 2000, eraseDelay: 3000);

            // Rotation stuff
            this.StarburstRotate(self, other, time, 0, rot: 100, posOverride: (1920 / 2, 1080 / 2), numLasers: 3);
            if (this.scrbp.time(self, other, time)) {
                this.rng.Shuffle(this.playerTargets);
                this.bp.prscircle(self, other, spawnDelay: 5000, radius: 550, position: this.posSnapshot);
                this.bp.prscircle(self, other, spawnDelay: 6666, radius: 550, position: this.posSnapshot);
                this.bp.prscircle(self, other, spawnDelay: 8333, radius: 550, position: this.posSnapshot);
                this.bp.prscircle(self, other, spawnDelay: 10000, radius: 550, position: this.posSnapshot);
            }
            time += 2000;

            this.StarburstLaser(self, other, time, this.playerTargets[0], numLasers: 3, spawnDelay: 3500, eraseDelay: 4000);
            time += this.StarburstLaser(self, other, time, this.playerTargets[1], numLasers: 3, spawnDelay: 3500, eraseDelay: 4000);

            this.StarburstRotate(self, other, time, 0, spawnDelay: 2000, eraseDelay: 6000, rot: -100, posOverride: (1920 / 2, 1080 / 2), numLasers: 3);
            if (this.scrbp.time(self, other, time)) {
                this.bp.prscircle(self, other, spawnDelay: 1250, radius: 550, position: this.posSnapshot);
                this.bp.prscircle(self, other, spawnDelay: 2500, radius: 550, position: this.posSnapshot);
                this.bp.prscircle(self, other, spawnDelay: 3750, radius: 550, position: this.posSnapshot);
                this.bp.prscircle(self, other, spawnDelay: 5000, radius: 550, position: this.posSnapshot);
            }
            time += 2000;
            this.StarburstLaser(self, other, time, this.playerTargets[2], numLasers: 3, spawnDelay: 3500, eraseDelay: 4000);
            time += this.StarburstLaser(self, other, time, this.playerTargets[3], numLasers: 3, spawnDelay: 3500, eraseDelay: 4000);

            // Mink windmill callback
            double gameSpeed = 1;
            for (int i = 0; i < 4; i++) {
                gameSpeed += 0.4;
                if (this.scrbp.time(self, other, time)) {
                    this.rng.Shuffle(this.playerTargets);
                    this.bp.circle_spreads(self, other, radius: 200, spawnDelay: 2000, targetMask: 1 << this.playerTargets[0]);
                    this.utils.GetGlobalVar("gameTimeSpeed")->Real = gameSpeed;
                    this.bp.clockspot(self, other, warningDelay: 1000, warningDelay2: 6000, spawnDelay: 12000, fanAngle: 20, position: (this.myX, this.myY));
                }
                time += 2000;
                time += this.StarburstRotate(self, other, time, this.playerTargets[0], numLasers: 2, spawnDelay: 6000, eraseDelay: 12000, rot: this.playerRng.Next(0, 2) == 1 ? 180 : -180);
            }

            time += this.Rem0Callback(self, other, time);

            // Speed cooldown
            if (this.scrbp.time(self, other, time)) {
                this.logger.PrintMessage("Finished with rem0 callback", this.logger.ColorRedLight);
                this.rng.Shuffle(this.playerTargets);
            }
            while (gameSpeed > 1) {
                int spawnDelay = (int) (1000 * gameSpeed);
                int eraseDelay = (int) (1666 * gameSpeed);

                this.StarburstLaser(self, other, time, target: this.playerTargets[0], numLasers: 3, spawnDelay: spawnDelay, eraseDelay: eraseDelay);
                this.StarburstLaser(self, other, time, target: this.playerTargets[1], numLasers: 3, spawnDelay: spawnDelay, eraseDelay: eraseDelay);
                this.StarburstLaser(self, other, time, target: this.playerTargets[2], numLasers: 3, spawnDelay: spawnDelay, eraseDelay: eraseDelay);
                time += this.StarburstLaser(self, other, time, target: this.playerTargets[3], numLasers: 3, spawnDelay: spawnDelay, eraseDelay: eraseDelay);
                gameSpeed -= 0.4;
                if (this.scrbp.time(self, other, time)) {
                    this.logger.PrintMessage("Slowing game speed, now at " + gameSpeed, this.logger.ColorRedLight);
                    this.utils.GetGlobalVar("gameTimeSpeed")->Real = gameSpeed;
                }
            }

            if (this.scrbp.time(self, other, time)) {
                this.utils.GetGlobalVar("gameTimeSpeed")->Real = 1; // Just to absolutely make sure we're back to normal
                if (!this.PhaseChange(self, other, 0.3)) {
                    this.bp.enrage_deco(self, other);
                }
            }
            time += 6000;
            if (this.scrbp.time_repeating(self, other, time, 2000)) {
                this.PhaseChange(self, other, 0.3);
            }
            this.StarburstLaser(self, other, time, this.playerTargets[0], 4, spawnDelay: 2000, eraseDelay: 20000);
            time += 2000;
            this.StarburstLaser(self, other, time, this.playerTargets[1], 5, spawnDelay: 2000, eraseDelay: 18000);
            time += 2000;
            this.StarburstLaser(self, other, time, this.playerTargets[2], 6, spawnDelay: 2000, eraseDelay: 16000);
            time += 2000;
            this.StarburstLaser(self, other, time, this.playerTargets[3], 7, spawnDelay: 2000, eraseDelay: 14000);
            time += 2000;
            this.StarburstLaser(self, other, time, 0, 8, spawnDelay: 2000, eraseDelay: 12000, posOverride: (1920/2, 1080/2));
            time += 2000;
            if (this.scrbp.time(self, other, time)) {
                this.bp.enrage(self, other, spawnDelay: 0, timeBetween: 667);
            }
            this.logger.PrintMessage("Time " + time, this.logger.ColorRedLight);
            return returnValue;
        }

        // "pt4"
        public RValue* JumpCleavePhase(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        ) {
            this.logger.PrintMessage("Jump Cleave phase", this.logger.ColorRed);

            int time = 0;
            if (this.scrbp.time(self, other, time)) {
                this.scrbp.phase_pattern_remove(self, other);
                this.scrbp.heal(self, other, 1);
            }
            // Add field limit yeet + jump cleave thing?

            if (this.scrbp.time_repeating(self, other, 0, 10000)) {
                this.bp.fieldlimit_rectangle_temporary(self, other, position: (500, 1000), width: 100, height: 100, targetMask: 0, eraseDelay: 1000);
                this.PhaseChange(self, other, 0.9);
            }
            return returnValue;
        }

        private int MinkRainstormCallback(CInstance* self, CInstance* other, int startTime) {
            int time = startTime;
            if (this.scrbp.time(self, other, time)) {
                this.bp.fieldlimit_rectangle_temporary(self, other,
                    position: (1920 / 2, 1080 / 2),
                    width: 1740,
                    height: 900,
                    color: IBattlePatterns.FIELDLIMIT_WHITE,
                    eraseDelay: 6000
                );
            }
            if (this.scrbp.time_repeat_times(self, other, time, 5000, 4)) {
                // Nonfunctional spread for just the warning message, so players know when fieldlimit decrease
                // snapshot happens
                this.bp.circle_spreads(self, other, warnMsg: 2, spawnDelay: 5000, radius: 0);
            }
            time += 2000;
            this.BubbleLine(self, other, time, 5000 * 3);
            time += 3000;
            if (this.scrbp.time_repeat_times(self, other, time, 5000, 4)) {
                int minX = int.MaxValue;
                int maxX = int.MinValue;

                int minY = int.MaxValue;
                int maxY = int.MinValue;

                for (int i = 0; i < this.utils.GetNumPlayers(); i++) {
                    var playerX = this.utils.GetPlayerVar(i, "distMovePrevX");
                    var playerY = this.utils.GetPlayerVar(i, "distMovePrevY");
                    minX = Math.Min(minX, (int) playerX->Real);
                    maxX = Math.Max(maxX, (int) playerX->Real);
                    minY = Math.Min(minY, (int) playerY->Real);
                    maxY = Math.Max(maxY, (int) playerY->Real);
                }
                minX = 500;
                minY = 500;

                int width = maxX - minX;
                int height = maxY - minY;
                this.bp.fieldlimit_rectangle_temporary(self, other,
                    position: (minX + width / 2, minY + height / 2),
                    width: width,
                    height: height,
                    color: IBattlePatterns.FIELDLIMIT_WHITE,
                    eraseDelay: 6000
                );
            }
            time += 5000 * 2;

            time += this.BubbleLineRotating(self, other, time, 5000 * 2, 180);

            return time - startTime;
        }

        // "pt5"
        public RValue* BubbleLinePhase(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        ) {
            this.logger.PrintMessage("Bubble Line phase", this.logger.ColorRed);
            this.playerRng = new Random(this.seed);

            int time = 0;
            time += this.StartRegularPhase(self, other, time);

            time += this.BubbleLine(self, other, time, 8000);
            time += this.BubbleLineRotating(self, other, time, 8000, this.playerRng.Next(0, 2) == 1 ? 180 : -180);

            if (this.scrbp.time(self, other, time)) {
                this.bp.clockspot(self, other, warningDelay2: 1000, fanAngle: 30, spawnDelay: 4000, warnMsg: 2);
            }
            time += 1000;
            this.BubbleLine(self, other, time, 8000, skipPercent: 60);
            time += this.BubbleLineRotating(self, other, time, 8000, this.playerRng.Next(0, 2) == 1 ? 90 : -90, skipPercent: 40);

            time += this.MinkRainstormCallback(self, other, time);

            // Stacking them gives double the projectile spam. Is it possible? Maybe not
            this.BubbleLine(self, other, time, 8000);
            time += this.BubbleLine(self, other, time, 8000, skipPercent: 50);

            // Something with cleaves?


            // Soft enrage
            /*
            if (this.scrbp.time(self, other, time)) {
                if (!this.PhaseChange(self, other, 0.3)) {
                    this.bp.enrage_deco(self, other);
                }
            }
            time += 2000;

            if (this.scrbp.time_repeating(self, other, time, 5000)) {
                this.PhaseChange(self, other, 0.3);
            }
            this.BubbleLine(self, other, time, 20000);
            time += 5000;
            this.BubbleLine(self, other, time, 20000);
            time += 5000;
            this.BubbleLine(self, other, time, 20000);
            time += 5000;
            this.BubbleLine(self, other, time, 20000);
            time += 5000;
            // Hard enrage
            if (this.scrbp.time(self, other, time)) {
                if (!this.PhaseChange(self, other, 0.3)) {
                    this.bp.enrage(self, other, spawnDelay: 0, timeBetween: 667);
                }
            }
            */
            this.logger.PrintMessage("Time " + time, this.logger.ColorRedLight);
            return returnValue;
        }

        // "pt2"
        public override RValue* FightAltDetour(
            CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
        ) {
            this.logger.PrintMessage("Final phase", this.logger.ColorRed);

            int time = 0;
            if (this.scrbp.time(self, other, time)) {
                this.scrbp.phase_pattern_remove(self, other);
                this.scrbp.heal(self, other, 1);
                this.scrbp.set_special_flags(self, other, IBattleScripts.FLAG_NO_POSITIONAL);
            }


            if (this.scrbp.time_repeating(self, other, 0, 10000)) {
                this.bp.colormatch(self, other);
            }
            return returnValue;
        }
    }
}
