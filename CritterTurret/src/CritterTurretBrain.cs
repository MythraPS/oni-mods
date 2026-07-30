using System.Collections.Generic;
using KSerialization;
using UnityEngine;

namespace CritterTurret
{
    // The turret's behaviour. Each Sim200ms tick it counts the critters in its room
    // per species; for every checked species whose own population is over the
    // threshold (and if the building is powered/operational), it locks onto a
    // critter of that species inside its firing
    // area (a rotated rectangle that matches the on-screen RangeVisualizer,
    // including the turret's own level -- like the Robo-Miner) with line of sight,
    // charges for two seconds and kills it with a single shot. Losing the target
    // mid-charge discards the charge. Kills drop meat normally.
    [SerializationConfig(MemberSerialization.OptIn)]
    public class CritterTurretBrain : KMonoBehaviour, ISim200ms, IThresholdSwitch
    {
        // Firing rectangle in local building space (rotates with the building).
        // y starts at 0 so it covers the turret's OWN level, like the Auto-Miner.
        public const int RANGE_MIN_X = -6, RANGE_MAX_X = 6;
        public const int RANGE_MIN_Y = 0, RANGE_MAX_Y = 8;

        // The turret winds up on a locked target for CHARGE_TIME, then discharges
        // once and kills it outright, whether it's a 5 HP Shine Bug or a 400 HP
        // Smooth Hatch. 2s matches the old three-shot burst (shots landed at t=0,
        // 1, 2 for 35-45% of max HP each), so time-to-kill is unchanged.
        private const float CHARGE_TIME = 2f;
        // How long the discharge flash (particles + sound) stays on after the kill
        // shot. Sim runs at 200ms, so 0.3f shows the fx for two sim ticks (~0.4s).
        private const float BURST_DURATION = 0.3f;
        // Beam length while charging, as a fraction of the distance to the target:
        // starts as a stub at the muzzle and grows to nearly touching, then snaps
        // out the full distance on discharge.
        private const float CHARGE_FX_MIN = 0.15f;
        private const float CHARGE_FX_MAX = 0.8f;
        // The "gun" art's rest direction. If the head points the wrong way, change this.
        private const float ARM_REST_DEG = -90f;
        // attack_beam_fx_kanim's rest direction. If the particles spray the wrong way,
        // change this (try 90/-90/180).
        private const float FX_REST_DEG = 0f;
        // How many tiles the spray art spans unstretched; the fx is stretched along its
        // length (animWidth) by dist/FX_NATURAL_LEN so particles reach the target.
        private const float FX_NATURAL_LEN = 4f;

        // ---- persisted settings ----
        [Serialize] public float populationThreshold = 4f;
        [Serialize] public bool activateAboveThreshold = true;

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
        // The critter currently being fired at. Held until it dies or leaves the
        // firing area / line of sight, instead of re-picking nearest every tick.
        private GameObject lockedTarget;
        private HashSet<int> arcCells;
        private string laserLoopSound;
        private bool laserLoopPlaying;

        private KBatchedAnimController body;
        private GameObject armGo;
        private KBatchedAnimController armCtrl;
        private GameObject fxGo;
        private KBatchedAnimController fxCtrl;
        private bool fxOn;

        [MyCmpGet] private Operational operational;
        [MyCmpGet] private LoopingSounds loopingSounds;
        [MyCmpGet] private TreeFilterable speciesFilter;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            body = GetComponent<KBatchedAnimController>();
            try { SetupArm(); }
            catch (System.Exception e) { Debug.LogWarning("[CritterTurret] arm rig setup failed (turret still fires): " + e); }

            // The dupes' attack sound. It's a LOOPING FMOD event (dupes start it when
            // their attack anim begins and stop it when it ends), so the turret runs it
            // while engaging a target rather than one-shotting it per shot.
            laserLoopSound = GlobalAssets.GetSound("AttackLaser_gun", false);

            // Make sure the Other Critters checklist category exists before the
            // side screen can target this turret. A new turret's checklist starts
            // with every box unchecked -- it holds fire until told what to shoot.
            CritterTurretTags.EnsureOtherCrittersRegistered();
        }

        protected override void OnCleanUp()
        {
            SetLaserLoop(false);
            HideBeamFx();
            base.OnCleanUp();
        }

        private void SetLaserLoop(bool on)
        {
            if (loopingSounds == null || string.IsNullOrEmpty(laserLoopSound)) return;
            if (on == laserLoopPlaying) return;
            if (on) loopingSounds.StartSound(laserLoopSound);
            else loopingSounds.StopSound(laserLoopSound);
            laserLoopPlaying = on;
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

        private Vector3 Muzzle() { return armGo != null ? armGo.transform.position : transform.position; }

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
            // Active (= 120 W draw + self-heat) only while actually engaging a
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
            if (charge >= CHARGE_TIME)
            {
                FireAt(target);
                charge = 0f;
                burstRemaining = BURST_DURATION;
            }

            // The laser loop runs for the whole wind-up rather than blipping once
            // per hit, and the beam grows out of the muzzle as the charge builds,
            // so the shot both looks and sounds like it is charging.
            SetLaserLoop(true);
            UpdateBeamFx(lastAimPos, burstRemaining > 0f
                ? 1f
                : Mathf.Lerp(CHARGE_FX_MIN, CHARGE_FX_MAX, charge / CHARGE_TIME));
        }

        // The kill shot destroys its own target, and the cull that finishes off a
        // species disarms the turret outright, so the discharge flash has to be
        // allowed to play out after there is nothing left to aim at.
        private void CoastDischargeFx()
        {
            if (burstRemaining > 0f) { SetLaserLoop(true); UpdateBeamFx(lastAimPos, 1f); }
            else { SetLaserLoop(false); HideBeamFx(); }
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

        // Checked in the species checklist = shot. The side screen stores
        // individual species prefab tags (babies are their own rows, e.g.
        // "Hatchling"); the category-tag fallbacks cover the whole-category
        // checkbox state some codepaths write instead of the leaves.
        private bool Matches(KPrefabID kpid)
        {
            // Robots aren't critters: never shot, never counted, no checkbox.
            if (kpid.HasTag(GameTags.Robot)) return false;
            if (speciesFilter == null) return true;
            if (speciesFilter.ContainsTag(kpid.PrefabTag)) return true;
            bool bagable = kpid.HasTag(GameTags.BagableCreature);
            bool swimming = kpid.HasTag(GameTags.SwimmingCreature);
            if (bagable && speciesFilter.ContainsTag(GameTags.BagableCreature)) return true;
            if (swimming && speciesFilter.ContainsTag(GameTags.SwimmingCreature)) return true;
            return !bagable && !swimming && speciesFilter.ContainsTag(CritterTurretTags.OtherCritters);
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
                if (kpid == null || !Matches(kpid)) continue;
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
                if (activateAboveThreshold ? kv.Value > populationThreshold
                                           : kv.Value < populationThreshold)
                    engagedSpecies.Add(kv.Key);

                if (first || (activateAboveThreshold ? kv.Value > currentCount : kv.Value < currentCount))
                    currentCount = kv.Value;
                first = false;
            }
        }

        // Lock-on: keep hammering the same critter until it dies or escapes the
        // firing area / line of sight; only then pick the nearest valid target.
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
                if (go.GetComponent<Health>() == null) continue;

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
            var health = go.GetComponent<Health>();
            if (health == null || health.hitPoints <= 0f) return false;
            int cell = Grid.PosToCell(go);
            return arcCells.Contains(cell) && HasLineOfSight(turretCell, cell);
        }

        private void FireAt(GameObject target)
        {
            Health health = target.GetComponent<Health>();
            if (health == null) return;
            // One discharge, one kill. Overkill by design: hitPoints covers an
            // already-wounded critter, maxHitPoints covers anything that heals
            // between the charge starting and the shot landing.
            health.Damage(Mathf.Max(health.hitPoints, health.maxHitPoints) + 1f);
            // The AttackLaser_gun loop (SetLaserLoop) is the wind-up; this is the
            // crack of the discharge on top of it.
            string s = GlobalAssets.GetSound("Building_Dmg_Metal", false);
            if (!string.IsNullOrEmpty(s)) KFMOD.PlayOneShot(s, Muzzle());
        }

        // The shot graphic: the dupes' own attack-beam particle spray
        // (attack_beam_fx_kanim / "loop" -- the white particles the multitool shoots
        // in attack mode). Runs from the muzzle, aimed at the target, while engaging.
        private void EnsureBeamFx()
        {
            if (fxGo != null) return;
            KAnimFile anim = Assets.GetAnim("attack_beam_fx_kanim");
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
                fxCtrl.animWidth = Mathf.Max(0.1f, d.magnitude * Mathf.Clamp01(reach) / FX_NATURAL_LEN);
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
        public float Threshold { get { return populationThreshold; } set { populationThreshold = value; } }
        public bool ActivateAboveThreshold { get { return activateAboveThreshold; } set { activateAboveThreshold = value; } }
        public float CurrentValue { get { return currentCount; } }
        public float RangeMin { get { return 0f; } }
        public float RangeMax { get { return 200f; } }
        public LocString Title { get { return (LocString)"STRINGS.UI.UISIDESCREENS.CRITTERTURRET.OPEN_FIRE"; } }
        public LocString ThresholdValueName { get { return (LocString)"STRINGS.UI.UISIDESCREENS.CRITTERTURRET.VALUE_NAME"; } }
        public string AboveToolTip { get { return "Fire at each checked species whose own population in the room is above {0}"; } }
        public string BelowToolTip { get { return "Fire at each checked species whose own population in the room is below {0}"; } }
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
