using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Pooled scrap (currency/XP) pickups with magnet attraction and end-of-wave auto collection.</summary>
    public class ScrapSystem : MonoBehaviour
    {
        const float CollectDistance = 0.45f;
        const float MagnetSpeed = 20f;

        class Piece
        {
            public Transform Transform;
            public Vector2 Position, Velocity;
            public int Value;
            public bool Magnet;
            public float Spin;
        }

        static ScrapSystem instance;

        Pool<Transform> pool;
        readonly List<Piece> active = new();
        readonly Stack<Piece> freePieces = new();
        bool collectAll;

        public bool IsEmpty => active.Count == 0;

        public static ScrapSystem Create(Transform parent)
        {
            var go = new GameObject("Scrap");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<ScrapSystem>();
            instance = s;
            s.pool = new Pool<Transform>(() =>
            {
                var t = new GameObject("ScrapPiece").transform;
                t.SetParent(go.transform, false);
                Visuals.Glow(t, Palette.Scrap, 1.1f, 0.35f, 0);
                Visuals.Sprite("Body", t, ShapeSprites.Diamond, Palette.Scrap, 1, 0.42f);
                return t;
            }, 64);
            return s;
        }

        public static void Drop(Vector2 position, int value)
        {
            if (!instance || value <= 0) return;
            for (int i = 0; i < value; i++)
            {
                var piece = instance.freePieces.Count > 0 ? instance.freePieces.Pop() : new Piece();
                piece.Transform = instance.pool.Get();
                piece.Position = position;
                piece.Velocity = Random.insideUnitCircle * (value > 1 ? 6f : 2.5f);
                piece.Value = 1;
                piece.Magnet = false;
                piece.Spin = Random.Range(0f, 360f);
                piece.Transform.position = position;
                instance.active.Add(piece);
            }
        }

        public void CollectAll() => collectAll = true;

        public void ResetForWave() => collectAll = false;

        void Update()
        {
            var gm = GameManager.Instance;
            if (!gm || !gm.Player) return;
            Vector2 player = gm.Player.Position;
            float range = gm.Player.PickupRange;
            float dt = Time.deltaTime;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                Vector2 to = player - p.Position;
                float dist = to.magnitude;

                if (!p.Magnet && (collectAll || dist < range) && gm.Player.IsAlive) p.Magnet = true;

                if (p.Magnet)
                {
                    float speed = MagnetSpeed * (collectAll ? 1.5f : 1f);
                    p.Velocity = Vector2.MoveTowards(p.Velocity, to.normalized * speed, 80f * dt);
                }
                else
                {
                    p.Velocity = Vector2.MoveTowards(p.Velocity, Vector2.zero, 8f * dt);
                }

                p.Position += p.Velocity * dt;
                p.Position.x = Mathf.Clamp(p.Position.x, -Arena.HalfSize.x + 0.3f, Arena.HalfSize.x - 0.3f);
                p.Position.y = Mathf.Clamp(p.Position.y, -Arena.HalfSize.y + 0.3f, Arena.HalfSize.y - 0.3f);
                p.Spin += 90f * dt;
                p.Transform.SetPositionAndRotation(p.Position, Quaternion.Euler(0f, 0f, p.Spin));

                if (p.Magnet && dist < CollectDistance)
                {
                    gm.CollectScrap(p.Value);
                    AudioManager.PlayPickup();
                    pool.Release(p.Transform);
                    active[i] = active[^1];
                    active.RemoveAt(active.Count - 1);
                    freePieces.Push(p);
                }
            }
        }
    }
}
