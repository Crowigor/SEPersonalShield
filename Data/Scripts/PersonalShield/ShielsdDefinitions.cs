using System;
using System.Collections.Generic;
using CSE.Data.Scripts.PersonalShield;

namespace CSEPersonalShield
{
    public static class ShielsdDefinitions
    {
        public static readonly List<ShieldsComponent.Shield> Shields = new List<ShieldsComponent.Shield>
        {
            new ShieldsComponent.Shield
            {
                SubtypeId = "CSEPersonalShield_Admin",
                DisplayName = "DisplayName_CSEPersonalShield_Admin",
                Rank = 99,
                Capacity = 10000,
                RechargePerSecond = 1000f,
                RechargeCost = 0.50f,
                BlockHpDamage = true,
                AbsorbGroups = new HashSet<GroupDefinitions.Group>
                {
                    GroupDefinitions.Group.Tools,
                    GroupDefinitions.Group.Collision,
                    GroupDefinitions.Group.Creature,
                    GroupDefinitions.Group.Bullet,
                    GroupDefinitions.Group.Rocket,
                    GroupDefinitions.Group.Explosion
                },
                Overload = new Dictionary<GroupDefinitions.Group, float>
                {
                    { GroupDefinitions.Group.Collision, 0.3f },
                    { GroupDefinitions.Group.Rocket, 0.3f },
                    { GroupDefinitions.Group.Explosion, 0.9f }
                }
            },
            new ShieldsComponent.Shield
            {
                SubtypeId = "CSEPersonalShield_Rank5",
                DisplayName = "DisplayName_CSEPersonalShield_Rank5",
                Rank = 5,
                Capacity = 3600f,
                RechargePerSecond = 360f,
                RechargeCost = 0.20f,
                AbsorbGroups = new HashSet<GroupDefinitions.Group>
                {
                    GroupDefinitions.Group.Tools,
                    GroupDefinitions.Group.Collision,
                    GroupDefinitions.Group.Creature,
                    GroupDefinitions.Group.Bullet,
                    GroupDefinitions.Group.Rocket,
                    GroupDefinitions.Group.Explosion
                },
                Overload = new Dictionary<GroupDefinitions.Group, float>
                {
                    { GroupDefinitions.Group.Collision, 0.3f },
                    { GroupDefinitions.Group.Rocket, 0.3f },
                    { GroupDefinitions.Group.Explosion, 0.9f }
                }
            },
            new ShieldsComponent.Shield
            {
                SubtypeId = "CSEPersonalShield_Rank4",
                DisplayName = "DisplayName_CSEPersonalShield_Rank4",
                Rank = 4,
                Capacity = 2200f,
                RechargePerSecond = 220f,
                RechargeCost = 0.20f,
                AbsorbGroups = new HashSet<GroupDefinitions.Group>
                {
                    GroupDefinitions.Group.Tools,
                    GroupDefinitions.Group.Collision,
                    GroupDefinitions.Group.Creature,
                    GroupDefinitions.Group.Bullet,
                    GroupDefinitions.Group.Rocket
                },
                Overload = new Dictionary<GroupDefinitions.Group, float>
                {
                    { GroupDefinitions.Group.Collision, 0.3f },
                    { GroupDefinitions.Group.Rocket, 0.5f }
                }
            },
            new ShieldsComponent.Shield
            {
                SubtypeId = "CSEPersonalShield_Rank3",
                DisplayName = "DisplayName_CSEPersonalShield_Rank3",
                Rank = 3,
                Capacity = 1400f,
                RechargePerSecond = 140f,
                RechargeCost = 0.20f,
                AbsorbGroups = new HashSet<GroupDefinitions.Group>
                {
                    GroupDefinitions.Group.Tools,
                    GroupDefinitions.Group.Collision,
                    GroupDefinitions.Group.Creature,
                    GroupDefinitions.Group.Bullet
                },
                Overload = new Dictionary<GroupDefinitions.Group, float>
                {
                    { GroupDefinitions.Group.Collision, 0.5f }
                }
            },
            new ShieldsComponent.Shield
            {
                SubtypeId = "CSEPersonalShield_Rank2",
                DisplayName = "DisplayName_CSEPersonalShield_Rank2",
                Rank = 2,
                Capacity = 800f,
                RechargePerSecond = 80f,
                RechargeCost = 0.20f,
                AbsorbGroups = new HashSet<GroupDefinitions.Group>
                {
                    GroupDefinitions.Group.Tools,
                    GroupDefinitions.Group.Collision,
                    GroupDefinitions.Group.Creature
                },
                Overload = new Dictionary<GroupDefinitions.Group, float>
                {
                    { GroupDefinitions.Group.Collision, 0.5f }
                }
            },
            new ShieldsComponent.Shield
            {
                SubtypeId = "CSEPersonalShield_Rank1",
                DisplayName = "DisplayName_CSEPersonalShield_Rank1",
                Rank = 1,
                Capacity = 400f,
                RechargePerSecond = 40f,
                RechargeCost = 0.20f,
                AbsorbGroups = new HashSet<GroupDefinitions.Group>
                {
                    GroupDefinitions.Group.Tools,
                    GroupDefinitions.Group.Collision
                },
                Overload = new Dictionary<GroupDefinitions.Group, float>
                {
                    { GroupDefinitions.Group.Collision, 0.5f }
                }
            },
            new ShieldsComponent.Shield
            {
                SubtypeId = "CSEPersonalShield_Rank0",
                DisplayName = "DisplayName_CSEPersonalShield_Rank0",
                Rank = 0,
                Capacity = 200f,
                RechargePerSecond = 20f,
                RechargeCost = 0.20f,
                AbsorbGroups = new HashSet<GroupDefinitions.Group>
                {
                    GroupDefinitions.Group.Tools
                }
            }
        };
    }
}