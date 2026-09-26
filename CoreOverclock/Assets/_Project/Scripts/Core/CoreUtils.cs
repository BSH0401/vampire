using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CoreOverclock
{
    public struct DamageInfo
    {
        public float Amount;
        public bool Crit;
        public Vector2 Direction;
        public float Knockback;
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        Vector2 Position { get; }
        void TakeDamage(in DamageInfo info);
    }

    /// <summary>Simple component pool. Objects are deactivated on release, activated on get.</summary>
    public class Pool<T> where T : Component
    {
        readonly Func<T> factory;
        readonly Stack<T> free = new();

        public Pool(Func<T> factory, int prewarm = 0)
        {
            this.factory = factory;
            for (int i = 0; i < prewarm; i++)
            {
                var t = factory();
                t.gameObject.SetActive(false);
                free.Push(t);
            }
        }

        public T Get()
        {
            var t = free.Count > 0 ? free.Pop() : factory();
            t.gameObject.SetActive(true);
            return t;
        }

        public void Release(T t)
        {
            t.gameObject.SetActive(false);
            free.Push(t);
        }
    }

    public static class GameLayers
    {
        public static int Player, Enemy, Wall, Obstacle;
        public static int HittableMask;

        public static void Init()
        {
            Player = Get("Player");
            Enemy = Get("Enemy");
            Wall = Get("Wall");
            Obstacle = Get("Obstacle");
            HittableMask = (1 << Enemy) | (1 << Obstacle);
        }

        static int Get(string name)
        {
            int l = LayerMask.NameToLayer(name);
            if (l < 0)
            {
                Debug.LogError($"Layer '{name}' missing. Run 'Core Overclock/Setup Project'.");
                return 0;
            }
            return l;
        }
    }

    public static class Palette
    {
        public static readonly Color Background = new(0.03f, 0.03f, 0.06f);
        public static readonly Color Grid = new(0.25f, 0.3f, 0.6f, 0.18f);
        public static readonly Color Player = new(0.25f, 1f, 0.95f);
        public static readonly Color Wall = new(0.3f, 0.65f, 1f);
        public static readonly Color Scrap = new(0.45f, 1f, 0.5f);
        public static readonly Color Danger = new(1f, 0.25f, 0.3f);
        public static readonly Color Tower = new(0.35f, 0.55f, 1f);
        public static readonly Color Text = new(0.9f, 0.95f, 1f);
    }

    public static class GameInput
    {
        static InputAction move, pause;

        public static void Init()
        {
            if (move != null) return;
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            move.Enable();

            pause = new InputAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");
            pause.Enable();
        }

        public static Vector2 Move
        {
            get
            {
                if (DevCommandLine.Autopilot) return DevCommandLine.AutopilotMove();
                return Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
            }
        }

        public static bool PausePressed => pause.WasPressedThisFrame();
    }
}
