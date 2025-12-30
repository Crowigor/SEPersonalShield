using System.Collections.Generic;
using CSE.Data.Scripts.PersonalShield;
using VRage.Utils;

namespace CSEPersonalShield
{
    public static class GroupDefinitions
    {
        public enum Group
        {
            Tools,
            Collision,
            Creature,
            Bullet,
            Rocket,
            Explosion,
            Other // Don't remove
        }

        public static readonly Dictionary<Group, DamageGroup> Groups = new Dictionary<Group, DamageGroup>
        {
            {
                Group.Tools, new DamageGroup
                {
                    DisplayName = "Tools",
                    IconMaterial = "CSEPersonalShield_DamageTools",
                    DamageTypes = new List<MyStringHash>
                    {
                        MyStringHash.GetOrCompute("Drill"),
                        MyStringHash.GetOrCompute("Grind"),
                        MyStringHash.GetOrCompute("Weld"),
                    }
                }
            },
            {
                Group.Collision, new DamageGroup
                {
                    DisplayName = "Collision",
                    IconMaterial = "CSEPersonalShield_DamageCollision",
                    DamageTypes = new List<MyStringHash>
                    {
                        MyStringHash.GetOrCompute("Fall"),
                        MyStringHash.GetOrCompute("Environment")
                    }
                }
            },
            {
                Group.Creature, new DamageGroup() // Ag
                {
                    DisplayName = "Creature",
                    IconMaterial = "CSEPersonalShield_DamageCreature",
                    DamageTypes = new List<MyStringHash>
                    {
                        MyStringHash.GetOrCompute("Wolf"),
                        MyStringHash.GetOrCompute("Spider"),
                    }
                }
            },

            {
                Group.Bullet, new DamageGroup // Au
                {
                    DisplayName = "Bullet",
                    IconMaterial = "CSEPersonalShield_DamageBullet",
                    DamageTypes = new List<MyStringHash>
                    {
                        MyStringHash.GetOrCompute("Bullet"),
                        MyStringHash.GetOrCompute("Bolt")
                    }
                }
            },
            {
                Group.Rocket, new DamageGroup // Pt
                {
                    DisplayName = "Rocket",
                    IconMaterial = "CSEPersonalShield_DamageRocket",
                    DamageTypes = new List<MyStringHash>
                    {
                        MyStringHash.GetOrCompute("Rocket"),
                    }
                }
            },
            {
                Group.Explosion, new DamageGroup // U
                {
                    DisplayName = "Explosion",
                    IconMaterial = "CSEPersonalShield_DamageExplosion",
                    DamageTypes = new List<MyStringHash>
                    {
                        MyStringHash.GetOrCompute("Explosion")
                    }
                }
            },
        };
    }
}