using System;
using System.Collections.Generic;
using CSEPersonalShield;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using VRage.Game.ModAPI;

namespace CSE.Data.Scripts.PersonalShield
{
    public class ShieldsComponent
    {
        private readonly Dictionary<string, Shield> _shields;

        public ShieldsComponent()
        {
            _shields = new Dictionary<string, Shield>();

            foreach (var shield in ShielsdDefinitions.Shields)
            {
                _shields[shield.SubtypeId] = shield;
            }
        }

        public bool GetBestShieldInInventory(
            IMyCharacter character,
            out MyInventory inventory,
            out Shield shield,
            out MyObjectBuilder_GasContainerObject bottle)
        {
            inventory = null;
            shield = null;
            bottle = null;
            if (character == null)
            {
                return false;
            }

            var characterInventory = character.GetInventory();
            inventory = characterInventory as MyInventory;

            var items = inventory?.GetItems();
            if (items == null || items.Count == 0)
            {
                return false;
            }

            var bestRank = -999;
            Shield bestShield = null;
            MyObjectBuilder_GasContainerObject bestBottle = null;
            foreach (var item in items)
            {
                var itemBottle = item.Content as MyObjectBuilder_GasContainerObject;
                if (itemBottle == null)
                {
                    continue;
                }

                var subtypeId = itemBottle.SubtypeName;
                if (string.IsNullOrWhiteSpace(subtypeId))
                {
                    continue;
                }

                if (!_shields.ContainsKey(subtypeId))
                {
                    continue;
                }

                var itemShield = _shields[subtypeId];
                if (itemShield.Rank < bestRank)
                {
                    continue;
                }

                bestRank = itemShield.Rank;
                bestBottle = itemBottle;
                bestShield = itemShield;
            }

            if (bestBottle == null)
            {
                return false;
            }

            bottle = bestBottle;
            shield = bestShield;
            return true;
        }

        public class Shield
        {
            public string DisplayName;
            public string SubtypeId;
            public int Rank;
            public float Capacity;
            public float RechargePerSecond;
            public float RechargeCost;
            public HashSet<GroupDefinitions.Group> AbsorbGroups;
            public bool BlockHpDamage = false;
            public Dictionary<GroupDefinitions.Group, float> Overload;
        }
    }
}