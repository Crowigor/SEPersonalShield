using System;
using System.Collections.Generic;
using System.Linq;
using CSEPersonalShield;
using VRage.Utils;

namespace CSE.Data.Scripts.PersonalShield
{
    public class GroupsComponent
    {
        private readonly Dictionary<GroupDefinitions.Group, DamageGroup> _groups;
        private readonly Dictionary<MyStringHash, GroupDefinitions.Group> _damages;

        public GroupsComponent()
        {
            _groups = new Dictionary<GroupDefinitions.Group, DamageGroup>();
            _damages = new Dictionary<MyStringHash, GroupDefinitions.Group>();
            foreach (GroupDefinitions.Group selector in Enum.GetValues(typeof(GroupDefinitions.Group)))
            {
                var group = GroupDefinitions.Groups.ContainsKey(selector)
                    ? GroupDefinitions.Groups[selector]
                    : new DamageGroup
                    {
                        DisplayName = selector.ToString(),
                        DamageTypes = new List<MyStringHash>(),
                        IconMaterial = ""
                    };

                group.Enum = selector;

                _groups[selector] = group;

                foreach (var damage in group.DamageTypes)
                {
                    _damages[damage] = selector;
                }
            }
        }

        public DamageGroup GetGroup(GroupDefinitions.Group selector)
        {
            DamageGroup result;
            return _groups.TryGetValue(selector, out result) ? result : _groups[GroupDefinitions.Group.Other];
        }

        public DamageGroup GetGroup(MyStringHash damage)
        {
            var selector = _damages.ContainsKey(damage) ? _damages[damage] : GroupDefinitions.Group.Other;

            return GetGroup(selector);
        }

        public List<DamageGroup> GetList(bool other = false)
        {
            if (other)
            {
                return _groups.Values.ToList();
            }

            var result = new List<DamageGroup>();
            foreach (var group in _groups.Values)
            {
                if (group.Enum != GroupDefinitions.Group.Other)
                {
                    result.Add(group);
                }
            }

            return result;
        }
    }

    public class DamageGroup
    {
        public GroupDefinitions.Group Enum;
        public string DisplayName;
        public List<MyStringHash> DamageTypes;
        public string IconMaterial;
    }
}