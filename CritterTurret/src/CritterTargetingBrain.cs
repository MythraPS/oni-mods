using System.Collections.Generic;
using UnityEngine;

namespace CritterTurret
{
    // Shared behaviour for the auto-targeting critter turrets. Each Sim200ms tick
    // it counts the
    // critters in its room per species; for every checked species whose own
    // population is over (or under) the threshold, it locks onto a critter of that
    // species inside its firing area -- a rotated rectangle matching the on-screen
    // RangeVisualizer, including the turret's own level, like the Robo-Miner -- with
    // line of sight, winds up on it for ChargeTime, then discharges.
    //
    // What the discharge DOES is the only real difference between the two buildings
    // (kill it outright, or catch it alive), so that is the part subclasses supply. Losing the target mid-charge discards the charge either way.
    //
    // Serialized settings deliberately stay on the concrete subclasses rather than
    // moving here: the lethal turret's brain is already live in players' saves, and
    // keeping its [Serialize] field set byte-for-byte identical is what makes this
    // refactor invisible to an existing colony.
    public abstract class CritterTargetingBrain : KMonoBehaviour, ISim200ms, IThresholdSwitch
    {
        // Firing rectangle in local building space (rotates with the building).
        // y starts at 0 so it covers the turret's OWN level, like the Auto-Miner.
        public const int RANGE_MIN_X = -6, RANGE_MAX_X = 6;
        public const int RANGE_MIN_Y = 0, RANGE_MAX_Y = 8;

        // Logic output: 1 once every checked species sits at or under the threshold
        // (nothing left to do), 0 while any species is still over it. Same port id on
        // both buildings -- ports are per-prefab, so they don't collide.
        public static readonly HashedString POPULATION_REACHED_PORT_ID = new HashedString("CritterPopulationReached");

        // How long the discharge flash (particles + sound) stays on after the shot.
        // Sim runs at 200ms, so 0.3f shows the fx for two sim ticks (~0.4s).
        private const float BURST_DURATION = 0.3f;
        // Beam length while charging, as a fraction of the distance to the target:
        // starts as a stub at the muzzle and grows to nearly touching, then snaps
        // out the full distance on discharge.
        private const float CHARGE_FX_MIN = 0.15f;
        private const float CHARGE_FX_MAX = 0.8f;
        // The "gun" art's rest direction. If the head points the wrong way, change this.
        private const float ARM_REST_DEG = -90f;
        // The beam art's rest direction. If the effect points the wrong way,
        // change this (try 90/-90/180).
        private const float FX_REST_DEG = 0f;

        // ---- what subclasses supply ----

        // Wind-up before the discharge lands.
        protected abstract float ChargeTime { get; }
        // Does the actual work to the locked target: kill it, or wrangle it.
        protected abstract void Discharge(GameObject target);
        // Looping FMOD event played for the whole wind-up (the dupes' own laser loops).
        protected abstract string LoopSoundName { get; }
        // Registered string keys for the threshold side screen.
        protected abstract string TitleKey { get; }
        protected abstract string ValueNameKey { get; }
        // The beam kanim. These are the dupes' own multitool effects, taken from the
        // LaserEffect table in BaseMinionConfig.SetupLaserEffects -- the turret uses
        // the "attack" one, the wrangler the "capture" net gun.
        protected virtual string BeamFxAnimName { get { return "attack_beam_fx_kanim"; } }
        // How many tiles the beam art spans unstretched; it is stretched along its
        // length (animWidth) by dist/this so the effect reaches the target.
        protected virtual float FxNaturalLength { get { return 4f; } }
        // Extra per-mode test on a candidate beyond "checked species, in range, in
        // sight" -- e.g. the wrangler skips critters it cannot capture.
        protected virtual bool CanEngage(GameObject go) { return true; }
        // Whether a critter still counts as surplus. The wrangler excludes ones it has
        // already bagged, so a room is worked through once rather than repeatedly
        // while the catches wait for a Duplicant to haul them.
        protected virtual bool CountsTowardPopulation(KPrefabID kpid) { return true; }
        // Hook for registering any invented checklist category with the per-game
        // DiscoveredResources. Runs on every spawn, including after a load.
        protected virtual void RegisterChecklistCategories() { }
        // The invented catch-all checklist category, if this building has one, used
        // to match critters that carry neither BagableCreature nor SwimmingCreature.
        protected virtual Tag? ExtraCategory { get { return null; } }

        private float charge;
        private float burstRemaining;
        // Where the last shot was aimed, so the discharge flash can finish playing
        // on the tick after the target dies and stops being acquirable.
        private Vector3 lastAimPos;
        private float currentCount;
        // Room population per species, and the species whose own population is
        // over (or under) the threshold. Each checked species is judged on its own
        // count, so a herd of Hatches getting culled doesn't drag the two Pufts
        // sharing their room into it. Keyed by prefab tag, which is the same
        // granularity the checklist uses -- babies and morphs are their own rows,
        // so they are their own populations too.
        private readonly Dictionary<Tag, int> speciesCounts = new Dictionary<Tag, int>();
        private readonly HashSet<Tag> engagedSpecies = new HashSet<Tag>();
        // The critter currently being worked on. Held until it dies or leaves the
        // firing area / line of sight, instead of re-picking nearest every tick.
        private GameObject lockedTarget;
        private HashSet<int> arcCells;
        private string loopSound;
        private bool loopPlaying;
        // Only pushed when it changes; LogicPorts.SendSignal walks the network.
        private int lastSignal = -1;

        private KBatchedAnimController body;
        private GameObject armGo;
        private KBatchedAnimController armCtrl;
        private GameObject fxGo;
        private KBatchedAnimController fxCtrl;
        private bool fxOn;

        [MyCmpGet] private Operational operational;
        [MyCmpGet] private LoopingSounds loopingSounds;
        [MyCmpGet] private LogicPorts logicPorts;
        [MyCmpGet] protected TreeFilterable speciesFilter;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            body = GetComponent<KBatchedAnimController>();
            try { SetupArm(); }
            catch (System.Exception e) { Debug.LogWarning("[" + GetType().Name + "] arm rig setup failed (building still works): " + e); }

            // The dupes' own laser sounds are LOOPING FMOD events (a dupe starts one
            // when its anim begins and stops it when it ends), so the turret runs one
            // for the length of the wind-up rather than one-shotting it per discharge.
            loopSound = GlobalAssets.GetSound(LoopSoundName, false);

            // Any checklist categories the subclass invents have to exist before
            // the side screen can target this building. A new building's checklist
            // starts with every box unchecked -- it holds fire until told what to
            // shoot.
            RegisterChecklistCategories();
        }

        protected override void OnCleanUp()
        {
            SetLoopSound(false);
            HideBeamFx();
            base.OnCleanUp();
        }

        private void SetLoopSound(bool on)
        {
            if (loopingSounds == null || string.IsNullOrEmpty(loopSound)) return;
            if (on == loopPlaying) return;
            if (on) loopingSounds.StartSound(loopSound);
            else loopingSounds.StopSound(loopSound);
            loopPlaying = on;
        }

        private void SetupArm()
        {
            if (body == null) return;
            body.SetSymbolVisiblity("gun_target", false);
            armGo = new GameObject(body.name + ".gun");
            armGo.SetActive(false);
            armGo.transform.SetParent(transform);
            var kpid = armGo.AddComponent<KPrefabID>();
            kpid.PrefabTag = new Tag(armGo.name);
            armCtrl = armGo.AddComponent<KBatchedAnimController>();
            armCtrl.AnimFiles = body.AnimFiles;
            armCtrl.initialAnim = "gun";
            // TransferArm renders in front of Building/BuildingFront -- it's the
            // layer the Auto-Sweeper's rotating arm uses. On the body's own layer
            // the draw order ties and the turret's neck can draw over the gun.
            armCtrl.sceneLayer = Grid.SceneLayer.TransferArm;
            armCtrl.isMovable = true;
            armGo.SetActive(true);
            armCtrl.Play("gun", KAnim.PlayMode.Loop);
            PositionArm();
        }

        private void PositionArm()
        {
            if (body == null || armGo == null) return;
            Matrix4x4 m = body.GetSymbolTransform("gun_target", out bool _);
            Vector3 p = m.GetColumn(3);
            // Must match the controller's scene layer, like the beam fx does with
            // FXFront; keeping the body's z would put the gun back behind the neck.
            p.z = Grid.GetLayerZ(Grid.SceneLayer.TransferArm);
            armGo.transform.position = p;
        }

        protected Vector3 Muzzle() { return armGo != null ? armGo.transform.position : transform.position; }

        private void AimArmAt(Vector3 targetPos)
        {
            if (armGo == null) return;
            PositionArm();
            Vector3 from = armGo.transform.position;
            Vector2 d = new Vector2(targetPos.x - from.x, targetPos.y - from.y);
            if (d.sqrMagnitude < 0.0001f) return;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            armGo.transform.rotation = Quaternion.Euler(0f, 0f, ang + ARM_REST_DEG);
        }

        public void Sim200ms(float dt)
        {
            int turretCell = OriginCell();
            if (!Grid.IsValidCell(turretCell)) return;

            TallyRoomBySpecies(turretCell);
            // Reported whether or not the building is powered: it is a reading about
            // the room, not about the turret, and staying live while switched off is
            // what lets it drive the thing that switches the turret off.
            SendPopulationSignal();

            bool armed = operational != null && operational.IsOperational && engagedSpecies.Count > 0;

            burstRemaining -= dt;
            if (!armed)
            {
                if (operational != null) operational.SetActive(false);
                charge = 0f;
                CoastDischargeFx();
                return;
            }

            GameObject target = AcquireTarget(turretCell);
            // Active (= full power draw + self-heat) only while actually engaging a
            // target, like the Robo-Miner while digging; armed-but-idle is free.
            if (operational != null) operational.SetActive(target != null);

            if (target == null)
            {
                // A half-charged shot doesn't carry over to the next critter.
                charge = 0f;
                CoastDischargeFx();
                return;
            }

            lastAimPos = TargetCenter(target);
            AimArmAt(lastAimPos);

            charge += dt;
            if (charge >= ChargeTime)
            {
                Discharge(target);
                charge = 0f;
                burstRemaining = BURST_DURATION;
            }

            // The laser loop runs for the whole wind-up rather than blipping once
            // per hit, and the beam grows out of the muzzle as the charge builds,
            // so the shot both looks and sounds like it is charging.
            SetLoopSound(true);
            UpdateBeamFx(lastAimPos, burstRemaining > 0f
                ? 1f
                : Mathf.Lerp(CHARGE_FX_MIN, CHARGE_FX_MAX, charge / ChargeTime));
        }

        private void SendPopulationSignal()
        {
            if (logicPorts == null) return;
            int signal = engagedSpecies.Count == 0 ? 1 : 0;
            if (signal == lastSignal) return;
            lastSignal = signal;
            logicPorts.SendSignal(POPULATION_REACHED_PORT_ID, signal);
        }

        // The discharge destroys or removes its own target, and the pass that
        // finishes off a species disarms the turret outright, so the flash has to be
        // allowed to play out after there is nothing left to aim at.
        private void CoastDischargeFx()
        {
            if (burstRemaining > 0f) { SetLoopSound(true); UpdateBeamFx(lastAimPos, 1f); }
            else { SetLoopSound(false); HideBeamFx(); }
        }

        private int OriginCell()
        {
            Building b = GetComponent<Building>();
            return b != null ? b.GetCell() : Grid.PosToCell(gameObject);
        }

        // The firing area: the local rectangle rotated by the building's orientation.
        // Built once (geometry is static); line of sight is still checked live.
        private void BuildArcCells()
        {
            arcCells = new HashSet<int>();
            int origin = OriginCell();
            Rotatable rot = GetComponent<Rotatable>();
            for (int y = RANGE_MIN_Y; y <= RANGE_MAX_Y; y++)
                for (int x = RANGE_MIN_X; x <= RANGE_MAX_X; x++)
                {
                    CellOffset off = new CellOffset(x, y);
                    if (rot != null) off = rot.GetRotatedCellOffset(off);
                    int c = Grid.OffsetCell(origin, off);
                    if (Grid.IsValidCell(c)) arcCells.Add(c);
                }
        }

        private List<KPrefabID> RoomCreatures(int cell)
        {
            if (Game.Instance == null || Game.Instance.roomProber == null) return null;
            CavityInfo cavity = Game.Instance.roomProber.GetCavityForCell(cell);
            return cavity?.creatures;
        }

        // Checked in the species checklist = targeted. The side screen stores
        // individual species prefab tags (babies are their own rows, e.g.
        // "Hatchling"); the category-tag fallbacks cover the whole-category
        // checkbox state some codepaths write instead of the leaves.
        protected bool Matches(KPrefabID kpid)
        {
            // Robots aren't critters: never targeted, never counted, no checkbox.
            if (kpid.HasTag(GameTags.Robot)) return false;
            if (speciesFilter == null) return true;
            if (speciesFilter.ContainsTag(kpid.PrefabTag)) return true;
            bool bagable = kpid.HasTag(GameTags.BagableCreature);
            bool swimming = kpid.HasTag(GameTags.SwimmingCreature);
            if (bagable && speciesFilter.ContainsTag(GameTags.BagableCreature)) return true;
            if (swimming && speciesFilter.ContainsTag(GameTags.SwimmingCreature)) return true;
            if (bagable || swimming) return false;
            Tag? extra = ExtraCategory;
            return extra.HasValue && speciesFilter.ContainsTag(extra.Value);
        }

        // Counts each checked species in the room separately and works out which of
        // them are over (or under) the threshold on their own numbers.
        private void TallyRoomBySpecies(int cell)
        {
            speciesCounts.Clear();
            engagedSpecies.Clear();
            currentCount = 0f;

            var list = RoomCreatures(cell);
            if (list == null) return;
            foreach (var kpid in list)
            {
                if (kpid == null || !Matches(kpid) || !CountsTowardPopulation(kpid)) continue;
                Tag key = kpid.PrefabTag;
                int n;
                speciesCounts.TryGetValue(key, out n);
                speciesCounts[key] = n + 1;
            }

            // The side screen shows one number, so show the population that is
            // driving the turret: the biggest herd when firing above a threshold,
            // the smallest when firing below one.
            bool first = true;
            foreach (var kv in speciesCounts)
            {
                if (ActivateAboveThreshold ? kv.Value > Threshold
                                           : kv.Value < Threshold)
                    engagedSpecies.Add(kv.Key);

                if (first || (ActivateAboveThreshold ? kv.Value > currentCount : kv.Value < currentCount))
                    currentCount = kv.Value;
                first = false;
            }
        }

        // Lock-on: keep working the same critter until it is dealt with or escapes
        // the firing area / line of sight; only then pick the nearest valid target.
        private GameObject AcquireTarget(int turretCell)
        {
            if (arcCells == null) BuildArcCells();
            if (StillValidTarget(lockedTarget, turretCell)) return lockedTarget;
            lockedTarget = null;

            var list = RoomCreatures(turretCell);
            if (list == null) return null;

            Vector3 from = Muzzle();
            GameObject best = null;
            float bestDist = float.MaxValue;

            foreach (var kpid in list)
            {
                // Checked in the checklist AND over its own population threshold.
                if (kpid == null || !Matches(kpid) || !engagedSpecies.Contains(kpid.PrefabTag)) continue;
                GameObject go = kpid.gameObject;
                if (go.GetComponent<CreatureBrain>() == null) continue; // critters only, never dupes
                if (!CanEngage(go)) continue;

                int targetCell = Grid.PosToCell(go);
                if (!arcCells.Contains(targetCell)) continue;
                if (!HasLineOfSight(turretCell, targetCell)) continue;

                float dist = Vector3.Distance(from, go.transform.position);
                if (dist < bestDist) { bestDist = dist; best = go; }
            }
            lockedTarget = best;
            return best;
        }

        private bool StillValidTarget(GameObject go, int turretCell)
        {
            if (go == null) return false; // includes Unity-destroyed objects
            var kpid = go.GetComponent<KPrefabID>();
            if (kpid == null || kpid.HasTag(GameTags.Dead) || !Matches(kpid)) return false;
            // Drop the lock as soon as this species falls back to the threshold,
            // so the last shot of a cull isn't already in flight when it does.
            if (!engagedSpecies.Contains(kpid.PrefabTag)) return false;
            if (!CanEngage(go)) return false;
            int cell = Grid.PosToCell(go);
            return arcCells.Contains(cell) && HasLineOfSight(turretCell, cell);
        }

        // The shot graphic: the dupes' own multitool effect for this action, run
        // from the muzzle and aimed at the target while engaging.
        private void EnsureBeamFx()
        {
            if (fxGo != null) return;
            KAnimFile anim = Assets.GetAnim(BeamFxAnimName);
            if (anim == null) return;
            fxGo = new GameObject(name + ".beam_fx");
            fxGo.SetActive(false);
            fxGo.transform.SetParent(transform);
            var kpid = fxGo.AddComponent<KPrefabID>();
            kpid.PrefabTag = new Tag(fxGo.name);
            fxCtrl = fxGo.AddComponent<KBatchedAnimController>();
            fxCtrl.AnimFiles = new KAnimFile[] { anim };
            fxCtrl.initialAnim = "loop";
            fxCtrl.sceneLayer = Grid.SceneLayer.FXFront;
            fxCtrl.isMovable = true;
        }

        // Aim at the critter's visual center (its collider), not its transform origin
        // (which sits at its feet and makes the spray point low).
        private static Vector3 TargetCenter(GameObject go)
        {
            var col = go.GetComponent<KBoxCollider2D>();
            if (col != null) return go.transform.position + (Vector3)col.offset;
            return go.transform.position + new Vector3(0f, 0.3f, 0f);
        }

        // reach = how far along the line to the target the spray extends: below 1
        // it is a charging stub sitting at the muzzle, at 1 it is the full shot.
        private void UpdateBeamFx(Vector3 targetPos, float reach)
        {
            EnsureBeamFx();
            if (fxGo == null) return;

            Vector3 from = Muzzle();
            from.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);
            fxGo.transform.position = from;
            Vector2 d = new Vector2(targetPos.x - from.x, targetPos.y - from.y);
            if (d.sqrMagnitude > 0.0001f)
            {
                float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                fxGo.transform.rotation = Quaternion.Euler(0f, 0f, ang + FX_REST_DEG);
                // Stretch the spray along its length so it reaches the target
                // (animWidth scales the anim's local X before rotation).
                fxCtrl.animWidth = Mathf.Max(0.1f, d.magnitude * Mathf.Clamp01(reach) / FxNaturalLength);
                fxCtrl.SetDirty();
            }

            if (!fxOn)
            {
                fxGo.SetActive(true);
                fxCtrl.Play("loop", KAnim.PlayMode.Loop);
                fxOn = true;
            }
        }

        private void HideBeamFx()
        {
            if (!fxOn) return;
            if (fxGo != null) fxGo.SetActive(false);
            fxOn = false;
        }

        private bool HasLineOfSight(int a, int b)
        {
            Grid.CellToXY(a, out int x0, out int y0);
            Grid.CellToXY(b, out int x1, out int y1);
            int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy, cx = x0, cy = y0;
            while (true)
            {
                bool isEndpoint = (cx == x0 && cy == y0) || (cx == x1 && cy == y1);
                if (!isEndpoint)
                {
                    int c = Grid.XYToCell(cx, cy);
                    if (Grid.IsValidCell(c) && Grid.Solid[c]) return false;
                }
                if (cx == x1 && cy == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; cx += sx; }
                if (e2 < dx) { err += dx; cy += sy; }
            }
            return true;
        }

        // ---- IThresholdSwitch (population-threshold side screen) ----
        // Threshold/ActivateAboveThreshold are left to the subclasses so their
        // [Serialize] fields keep their existing names in saved games.
        public abstract float Threshold { get; set; }
        public abstract bool ActivateAboveThreshold { get; set; }
        public abstract string AboveToolTip { get; }
        public abstract string BelowToolTip { get; }
        public float CurrentValue { get { return currentCount; } }
        public float RangeMin { get { return 0f; } }
        public float RangeMax { get { return 200f; } }
        public LocString Title { get { return (LocString)TitleKey; } }
        public LocString ThresholdValueName { get { return (LocString)ValueNameKey; } }
        public ThresholdScreenLayoutType LayoutType { get { return (ThresholdScreenLayoutType)0; } }
        public int IncrementScale { get { return 1; } }
        public NonLinearSlider.Range[] GetRanges { get { return NonLinearSlider.GetDefaultRange(RangeMax); } }
        public float GetRangeMinInputField() { return RangeMin; }
        public float GetRangeMaxInputField() { return RangeMax; }
        public LocString ThresholdValueUnits() { return ""; }
        public string Format(float value, bool units) { return value.ToString(); }
        public float ProcessedSliderValue(float input) { return Mathf.Round(input); }
        public float ProcessedInputValue(float input) { return Mathf.Round(input); }
    }
}
