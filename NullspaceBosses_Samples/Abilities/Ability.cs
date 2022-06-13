using System;
using UnityEngine;

namespace RPG.Combat
{
    public enum ActionType
    { Swing, Cast, Instant, Channel };
    public enum Resource
    { None, Flex, Ult, Rage, Mana, Energy, Spirit, Exertion, Stance }

    public enum TargetType
    { Self, NoTarget, SingleTarget, Point, AoECircle };

    [Flags]
    public enum School
    {
        None = 0,  // 0
        Physical = 1 << 0, // 1
        Fire = 1 << 1, // 2
        Cold = 1 << 2, // 4
        Air = 1 << 3, // 8
        Earth = 1 << 4, // 16
        Nature = 1 << 5, // 32
        Arcane = 1 << 6, // 64
        Necrotic = 1 << 7, // 128
        Chaos = 1 << 8, // 256
        True = 1 << 9, // 512
        Void = 1 << 10 // 1024
    };

    public abstract class Ability
    {
        // Core Data
        public string ActionType { get; set; }
        public string Name { get; set; }
        public School School { get; set; }
        public int SlotID { get; set; }

        // Cost & CD
        public int Cost { get; set; }
        public Resource Resource { get; set; }
        public float Cooldown { get; set; }

        // Output
        public float Range { get; set; }
        public int HPValue { get; set; }
        public int PoiseValue { get; set; }
        public Effect AbilityEffect { get; set; }

        // Sound & Animation
        public string InitSound { get; set; }
        public string HitSound { get; set; }
        public string ExecSound { get; set; }
        public string AnimationName { get; set; }
        public float AnimationDuration { get; set; }
        // Tooltip
        public string Tooltip { get; set; }
        // REin da!

        private string[] TooltipElements = new string[5];
        public string IconID { get; set; }
        // Buffs & Debuffs

        // Source
        public CharacterStatusAPI Source { get; set; }
        public CharacterStatusAPI Target { get; set; }

        public Vector3 SourcePoint { get; set; }

        public Vector3 TargetPoint { get; set; }

        public event Action OnFinish;
        public bool IsProc { get; set; }
        public bool IsOnHit { get; set; }

        // Methods
        public virtual void Init()
        {
        }
        public virtual void Execute()
        {
            // On spell execution
        }
        public virtual void Execute(CharacterStatusAPI target)
        {
            // On Hit with target
        }

        public virtual string[] GetTooltip()
        {
            TooltipElements[0] = Name;
            TooltipElements[1] = Cooldown.ToString();
            TooltipElements[2] = Cost.ToString();
            TooltipElements[3] = HPValue.ToString();
            TooltipElements[4] = PoiseValue.ToString();

            return TooltipElements;
        }
        public void Finish() => OnFinish?.Invoke();
    }
}