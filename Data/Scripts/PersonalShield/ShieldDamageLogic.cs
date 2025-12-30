using CSE.Data.Scripts.PersonalShield;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities.Inventory;
using VRage.Game.ModAPI;
using VRageMath;

namespace CSEPersonalShield
{
    public sealed class ShieldDamageLogic
    {
        private readonly GroupsComponent _groupsComponent;
        private readonly ShieldsComponent _shieldsComponent;

        public ShieldDamageLogic(GroupsComponent groupsComponent, ShieldsComponent shieldsComponent)
        {
            _groupsComponent = groupsComponent;
            _shieldsComponent = shieldsComponent;
        }

        public bool HandleBeforeDamage(
            IMyCharacter character,
            IMyPlayer player,
            ref MyDamageInformation info,
            out DamageHit hit)
        {
            hit = default(DamageHit);

            if (character == null || player == null)
            {
                return false;
            }

            if (info.Amount <= 0f)
            {
                return false;
            }

            MyInventory inventory;
            MyObjectBuilder_GasContainerObject bottle;
            ShieldsComponent.Shield shield;
            if (!_shieldsComponent.GetBestShieldInInventory(character, out inventory, out shield, out bottle))
            {
                return false;
            }

            var group = _groupsComponent.GetGroup(info.Type);

            hit.ShieldSubtypeId = shield.SubtypeId;
            hit.CharacterEntityId = character.EntityId;
            hit.VictimIdentityId = player.IdentityId;
            hit.Group = group.Enum;

            var incomingDamage = info.Amount;
            if (shield.AbsorbGroups == null || !shield.AbsorbGroups.Contains(group.Enum))
            {
                if (shield.BlockHpDamage)
                {
                    info.Amount = 0f;
                }

                DebugService.Damage("Hit (" + group.DisplayName + ") Shield: " + shield.SubtypeId +
                                    " | dmg: " + ToInt(incomingDamage) +
                                    " | type: " + info.Type +
                                    " | absorbed: NOT ABSORB" +
                                    " | passed: " + info.Amount +
                                    " | capacity: " + ToInt(bottle.GasLevel * 100f) + "%");

                return true;
            }

            var maxCapacity = shield.Capacity;
            if (maxCapacity <= 0f)
            {
                DebugService.Damage("Hit (" + group.DisplayName + ") Shield: " + shield.SubtypeId +
                                    " | dmg: " + ToInt(incomingDamage) +
                                    " | type: " + info.Type +
                                    " | absorbed: ZERO SHIELD CAPACITY" +
                                    " | passed: " + info.Amount +
                                    " | capacity: " + ToInt(bottle.GasLevel * 100f) + "%");

                return false;
            }

            if (shield.Overload != null && shield.Overload.ContainsKey(group.Enum))
            {
                var overload = maxCapacity * shield.Overload[group.Enum];
             
                if (incomingDamage > overload)
                {
                    DebugService.Damage("Overload (" + group.DisplayName + ") Shield: " + shield.SubtypeId +
                                        " | dmg: " + ToInt(incomingDamage) +
                                        " | type: " + info.Type +
                                        " | overload: " + ToInt(overload));
                    
                    incomingDamage = ToInt(overload);
                    info.Amount = incomingDamage;
                }
            }

            var gasLevel = MathHelper.Clamp(bottle.GasLevel, 0f, 1f);
            var capacityPoints = gasLevel * maxCapacity;

            var absorbed = incomingDamage;
            if (capacityPoints <= 0f)
            {
                absorbed = 0f;
            }
            else if (absorbed > capacityPoints)
            {
                absorbed = capacityPoints;
            }

            var remaining = incomingDamage - absorbed;
            if (remaining < 0f)
            {
                remaining = 0f;
            }

            if (absorbed > 0f)
            {
                capacityPoints -= absorbed;
                if (capacityPoints < 0f)
                {
                    capacityPoints = 0f;
                }

                bottle.GasLevel = MathHelper.Clamp(capacityPoints / maxCapacity, 0f, 1f);
                inventory.Refresh();

                hit.AbsorbedAny = true;
            }

            info.Amount = shield.BlockHpDamage ? 0f : remaining;

            DebugService.Damage("Hit (" + group.DisplayName + ") Shield: " + shield.SubtypeId +
                                " | dmg: " + ToInt(incomingDamage) +
                                " | type: " + info.Type +
                                " | absorbed: " + ToInt(absorbed) +
                                " | passed: " + ToInt(info.Amount) +
                                " | capacity: " + ToInt(bottle.GasLevel * 100f) + "%");

            return true;
        }

        private static int ToInt(float value)
        {
            return (int)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
        }

        public struct DamageHit
        {
            public bool AbsorbedAny;
            public string ShieldSubtypeId;
            public long CharacterEntityId;
            public long VictimIdentityId;
            public GroupDefinitions.Group Group;
        }
    }
}