using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Destructible obstacle that drops a pile of scrap (기획서 4.2).</summary>
    public class DataTower : MonoBehaviour, IDamageable
    {
        public static readonly List<DataTower> Active = new();

        const float BaseHP = 60f;
        const int ScrapReward = 10;

        SpriteRenderer body;
        SpriteRenderer[] dataBits;
        float hp, flashTimer;

        public bool IsAlive => hp > 0f;
        public Vector2 Position => transform.position;

        public static DataTower Create(Transform parent)
        {
            var go = new GameObject("DataTower") { layer = GameLayers.Obstacle };
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<DataTower>();
            go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 1.4f);
            Visuals.Glow(go.transform, Palette.Tower, 3f, 0.18f, 4);
            t.body = Visuals.Sprite("Body", go.transform, ShapeSprites.NeonSquare, Palette.Tower, 5);
            t.body.transform.localScale = new Vector3(1.15f, 1.6f, 1f);
            t.dataBits = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                var bit = Visuals.Sprite($"Bit_{i}", go.transform, ShapeSprites.White, Palette.Tower, 6);
                bit.transform.localPosition = new Vector3(0f, -0.4f + i * 0.4f, 0f);
                bit.transform.localScale = new Vector3(0.55f, 0.1f, 1f);
                t.dataBits[i] = bit;
            }
            go.SetActive(false);
            return t;
        }

        public void Activate(Vector2 position, int wave)
        {
            transform.position = position;
            hp = BaseHP * (1f + 0.35f * (wave - 1));
            gameObject.SetActive(true);
            if (!Active.Contains(this)) Active.Add(this);
        }

        public void Deactivate()
        {
            hp = 0f;
            Active.Remove(this);
            gameObject.SetActive(false);
        }

        void Update()
        {
            flashTimer -= Time.deltaTime;
            body.color = flashTimer > 0f ? Color.white : Palette.Tower;
            for (int i = 0; i < dataBits.Length; i++)
            {
                var c = Palette.Tower;
                c.a = Mathf.Repeat(Time.time * 1.5f + i * 0.33f, 1f) > 0.5f ? 0.9f : 0.2f;
                dataBits[i].color = c;
            }
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive) return;
            hp -= info.Amount;
            flashTimer = 0.05f;
            DamagePopups.Show(Position, info.Amount, new Color(0.7f, 0.8f, 1f), info.Crit);
            if (hp > 0f) return;

            FxSystem.Pulse(Position, Palette.Tower, 0.3f, 3.5f, 0.35f);
            CameraShake.Add(0.3f);
            ScrapSystem.Drop(Position, ScrapReward);
            Deactivate();
        }
    }
}
