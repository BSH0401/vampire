using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Auto-aiming weapon mounted around the player (기획서 4.1: 100% 자동 조작).</summary>
    public class Weapon : MonoBehaviour
    {
        const float MuzzleOffset = 0.35f;

        Transform pivot;
        Transform barrel;
        float cooldown;
        float recoil;

        public WeaponData Data { get; private set; }

        public static Weapon Create(WeaponData data, Transform parent)
        {
            var go = new GameObject($"Weapon_{data.id}");
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<Weapon>();
            w.Data = data;

            var color = WeaponTags.ColorOf(data.tag);
            w.pivot = new GameObject("Pivot").transform;
            w.pivot.SetParent(go.transform, false);
            var barrel = Visuals.Sprite("Barrel", w.pivot, ShapeSprites.White, color, 22);
            barrel.transform.localScale = new Vector3(0.42f, 0.14f, 1f);
            barrel.transform.localPosition = new Vector3(0.14f, 0f, 0f);
            w.barrel = barrel.transform;
            Visuals.Sprite("Mount", go.transform, ShapeSprites.Circle, color, 23, 0.22f);
            w.cooldown = Random.Range(0f, data.fireInterval);
            return w;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (!gm || gm.State != GameState.Combat) return;

            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 2f);
            barrel.localPosition = new Vector3(0.14f - recoil, 0f, 0f);
            cooldown -= Time.deltaTime;

            Vector2 origin = transform.position;
            var target = Targeting.FindNearest(origin, Data.range);
            if (target == null) return;

            Vector2 dir = (target.Position - origin).normalized;
            pivot.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            if (cooldown > 0f) return;
            cooldown = Data.fireInterval;
            Fire(origin + dir * MuzzleOffset, dir);
        }

        void Fire(Vector2 muzzle, Vector2 dir)
        {
            int count = Data.projectileCount;
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            for (int i = 0; i < count; i++)
            {
                float offset = count == 1 ? 0f : Mathf.Lerp(-Data.spreadAngle, Data.spreadAngle, i / (count - 1f));
                offset += Random.Range(-2f, 2f);
                float a = (baseAngle + offset) * Mathf.Deg2Rad;
                bool crit = Random.value < Data.critChance;
                ProjectileSystem.Spawn(Data, muzzle, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), crit);
            }
            recoil = 0.08f;
        }
    }

    public static class Targeting
    {
        /// <summary>Nearest enemy in range; data towers are only targeted when no enemy is in range.</summary>
        public static IDamageable FindNearest(Vector2 from, float range)
        {
            IDamageable best = null;
            float bestSqr = range * range;
            foreach (var e in Enemy.Active)
            {
                if (!e.IsAlive) continue;
                float d = (e.Position - from).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = e; }
            }
            if (best != null) return best;

            foreach (var t in DataTower.Active)
            {
                if (!t.IsAlive) continue;
                float d = (t.Position - from).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = t; }
            }
            return best;
        }
    }
}
