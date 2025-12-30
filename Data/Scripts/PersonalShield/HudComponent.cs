using Draygo.API;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using CSE.Data.Scripts.PersonalShield;
using Sandbox.Game;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace CSEPersonalShield
{
    public sealed class HudComponent
    {
        private const float HudPositionX = 10f;
        private const float HudPositionY = 300f;

        private static readonly string StatusIconMaterial = "CSEPersonalShield_HudShield";
        private static readonly Color StatusIconColor = new Color(186, 238, 249, 255);

        private static readonly string StatusIconChargeMaterial = "CSEPersonalShield_HudCharge";
        private static readonly Color StatusIconChargeColor0 = new Color(41, 54, 62, 255);
        private static readonly Color StatusIconChargeColor1 = new Color(255, 180, 0, 255);
        private static readonly Color StatusIconChargeColorError = new Color(255, 60, 60, 255);

        private static readonly Color DamageIconColorEnable = new Color(186, 238, 249, 255);
        private static readonly Color DamageIconColorDisable = new Color(93, 117, 125, 255);
        private static readonly Color DamageIconColorDamage = new Color(255, 0, 0, 255);

        private readonly GroupsComponent _groupsComponent;
        private readonly ShieldsComponent _shieldsComponent;
        private readonly HudAPIv2 _api;

        private bool _ready;

        private HudAPIv2.BillBoardHUDMessage _background;
        private HudAPIv2.HUDMessage _name;
        private HudAPIv2.BillBoardHUDMessage _statusIcon;
        private HudAPIv2.HUDMessage _capacity;
        private HudAPIv2.BillBoardHUDMessage _bar;
        private HudAPIv2.BillBoardHUDMessage _barFill;

        private readonly Dictionary<GroupDefinitions.Group, HudAPIv2.BillBoardHUDMessage> _damageIcons =
            new Dictionary<GroupDefinitions.Group, HudAPIv2.BillBoardHUDMessage>();

        private HudAPIv2.BillBoardHUDMessage _damageIconsBackground;

        private Vector2D _position;
        private float _offsetX;
        private float _offsetY;
        private bool _layoutDirty;

        private string _lastShieldSubtypeId;
        private float _lastBottleGasLevel = -1f;

        private GroupDefinitions.Group _lastDamageGroup = GroupDefinitions.Group.Other;
        private DateTime _lastDamageUtc = DateTime.MinValue;

        private bool _wasChargingLastFrame;
        private bool _chargeAnimFlip;
        private DateTime _chargeAnimNextSwitchUtc = DateTime.MinValue;

        private bool _hasRechargeStateFromServer;
        private RechargeState _rechargeStateFromServer = RechargeState.Off;

        public HudComponent(GroupsComponent groupsComponent, ShieldsComponent shieldsComponent)
        {
            _groupsComponent = groupsComponent;
            _shieldsComponent = shieldsComponent;
            _api = new HudAPIv2(OnRegistered);
        }

        public void SetOffsets(float x, float y)
        {
            _offsetX = x;
            _offsetY = y;
            _layoutDirty = true;
        }

        public void NotifyRecharge(long victimIdentityId, RechargeState state)
        {
            var local = MyAPIGateway.Session != null ? MyAPIGateway.Session.LocalHumanPlayer : null;
            if (local == null)
            {
                return;
            }

            if (local.IdentityId != victimIdentityId)
            {
                return;
            }

            _hasRechargeStateFromServer = true;
            _rechargeStateFromServer = state;
        }

        public void Update()
        {
            if (_api == null || !_api.Heartbeat)
            {
                return;
            }

            if (!_ready)
            {
                RegisterHud();
                _ready = true;
            }

            var local = MyAPIGateway.Session != null ? MyAPIGateway.Session.LocalHumanPlayer : null;
            if (local == null || local.Character == null)
            {
                SetAllVisible(false);
                ResetState();
                return;
            }

            MyInventory inventory;
            ShieldsComponent.Shield shield;
            MyObjectBuilder_GasContainerObject bottle;

            if (!_shieldsComponent.GetBestShieldInInventory(local.Character, out inventory, out shield, out bottle) ||
                shield == null || bottle == null)
            {
                SetAllVisible(false);
                ResetState();
                return;
            }

            var needUpdate = false;

            if (!string.Equals(_lastShieldSubtypeId, bottle.SubtypeName, StringComparison.Ordinal))
            {
                _lastShieldSubtypeId = bottle.SubtypeName;

                var displayName = GetLocalizedShieldNameFromDefinition(bottle.SubtypeName, shield.DisplayName);
                _name.Message.Clear().Append(displayName);

                needUpdate = true;
            }

            var fill = MathHelper.Clamp(bottle.GasLevel, 0f, 1f);

            var currentLevel = (int)Math.Round(fill * 1000f, MidpointRounding.AwayFromZero);
            var lastLevel = (int)Math.Round(_lastBottleGasLevel * 1000f, MidpointRounding.AwayFromZero);

            if (currentLevel != lastLevel)
            {
                _lastBottleGasLevel = fill;

                var currentCapacity = (int)Math.Round(fill * shield.Capacity, MidpointRounding.AwayFromZero);
                var maxCapacity = (int)Math.Round(shield.Capacity, MidpointRounding.AwayFromZero);

                _capacity.Message.Clear().Append(currentCapacity).Append(" / ").Append(maxCapacity);

                needUpdate = true;
            }

            if (needUpdate || _layoutDirty)
            {
                _layoutDirty = false;
                ApplyLayout(fill);
            }

            var now = DateTime.UtcNow;
            var damageAlpha = ComputeAlpha(now, _lastDamageUtc, 0.60, 0.35);
            var rechargeState = _hasRechargeStateFromServer ? _rechargeStateFromServer : RechargeState.Off;

            UpdateStatusIcon(now, damageAlpha, rechargeState, fill);
            UpdateDamageIcons(shield.AbsorbGroups, damageAlpha);

            SetAllVisible(true);
        }

        private void RegisterHud()
        {
            _api.OnScreenDimensionsChanged = OnScreenDimensionsChanged;
            OnScreenDimensionsChanged();

            var groups = _groupsComponent.GetList();
            var iconsWidth = 5;
            iconsWidth += groups.Count * 48;
            iconsWidth += (groups.Count - 2) * 10;
            iconsWidth += 10 + 5 + 4;

            _background = new HudAPIv2.BillBoardHUDMessage
            {
                Offset = Vector2.Zero,
                Width = iconsWidth,
                Height = 142,
                Material = MyStringId.GetOrCompute("CSEPersonalShield_HudBackground"),
                Blend = MyBillboard.BlendTypeEnum.PostPP,
                Options = HudAPIv2.Options.Pixel,
                BillBoardColor = new Color(20, 20, 20, 150),
                Visible = false
            };

            _name = new HudAPIv2.HUDMessage
            {
                Offset = new Vector2D(10, 10),
                Scale = 16,
                Message = new StringBuilder(""),
                InitialColor = new Color(255, 255, 255, 255),
                ShadowColor = new Color(0, 0, 0, 220),
                Blend = MyBillboard.BlendTypeEnum.PostPP,
                Font = HudAPIv2.DefaultFont,
                Options = HudAPIv2.Options.Pixel,
                Visible = false
            };

            _statusIcon = new HudAPIv2.BillBoardHUDMessage
            {
                Offset = new Vector2D(7, 35),
                Width = 36,
                Height = 36,
                Material = MyStringId.GetOrCompute(StatusIconMaterial),
                Blend = MyBillboard.BlendTypeEnum.PostPP,
                Options = HudAPIv2.Options.Pixel,
                BillBoardColor = StatusIconColor,
                Visible = false
            };

            var barWidth = iconsWidth - 50;
            _bar = new HudAPIv2.BillBoardHUDMessage
            {
                Offset = new Vector2D(45, 35),
                Height = 15,
                Width = barWidth,
                Material = MyStringId.GetOrCompute("CSEPersonalShield_HudBar"),
                Blend = MyBillboard.BlendTypeEnum.PostPP,
                Options = HudAPIv2.Options.Pixel,
                BillBoardColor = new Color(56, 65, 74, 50),
                Visible = false
            };

            _barFill = new HudAPIv2.BillBoardHUDMessage
            {
                Offset = new Vector2D(45, 35),
                Height = 15,
                Material = MyStringId.GetOrCompute("CSEPersonalShield_HudBar"),
                Blend = MyBillboard.BlendTypeEnum.PostPP,
                Options = HudAPIv2.Options.Pixel,
                BillBoardColor = new Color(56, 65, 74, 50),
                Visible = false
            };

            _capacity = new HudAPIv2.HUDMessage
            {
                Offset = new Vector2D(45, 57),
                Message = new StringBuilder(""),
                InitialColor = new Color(255, 255, 255, 255),
                ShadowColor = new Color(0, 0, 0, 220),
                Blend = MyBillboard.BlendTypeEnum.PostPP,
                Font = HudAPIv2.DefaultFont,
                Options = HudAPIv2.Options.Pixel,
                Visible = false,
                Scale = 14
            };

            _damageIconsBackground = new HudAPIv2.BillBoardHUDMessage
            {
                Offset = new Vector2(0, 80),
                Width = iconsWidth,
                Height = 58,
                Material = MyStringId.GetOrCompute("CSEPersonalShield_HudBackground"),
                Blend = MyBillboard.BlendTypeEnum.PostPP,
                Options = HudAPIv2.Options.Pixel,
                BillBoardColor = new Color(20, 20, 20, 50),
                Visible = false
            };

            _damageIcons.Clear();
            var iconX = 5;
            var first = true;

            foreach (var damageGroup in groups)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    iconX += 10;
                }

                _damageIcons[damageGroup.Enum] = new HudAPIv2.BillBoardHUDMessage
                {
                    Width = 48,
                    Height = 48,
                    Offset = new Vector2D(iconX, 87),
                    Material = MyStringId.GetOrCompute(damageGroup.IconMaterial),
                    Blend = MyBillboard.BlendTypeEnum.PostPP,
                    Options = HudAPIv2.Options.Pixel,
                    Visible = false
                };

                iconX += 48;
            }

            ApplyLayout();
        }

        private void ApplyLayout(float fill = 0f)
        {
            if (_background == null || _name == null || _statusIcon == null || _bar == null || _barFill == null ||
                _capacity == null || _damageIcons.Count == 0)
            {
                return;
            }

            _position = new Vector2D(HudPositionX + _offsetX, HudPositionY + _offsetY);

            _background.Origin = _position;
            _background.Offset = Vector2D.Zero;

            _name.Origin = _position;
            _statusIcon.Origin = _position;

            var capacityLength = _capacity.GetTextLength();
            var capacityX = (int)Math.Abs(_bar.Offset.X) + (int)Math.Abs(_bar.Width) / 2 -
                            (int)Math.Abs(capacityLength.X) / 2;

            _capacity.Origin = _position;
            _capacity.Offset = new Vector2D(capacityX, _capacity.Offset.Y);

            _bar.Origin = _position;
            _barFill.Origin = _position;

            if (fill >= 0.5f)
            {
                _barFill.BillBoardColor = new Color(186, 238, 249, 255);
            }
            else if (fill >= 0.25f)
            {
                _barFill.BillBoardColor = new Color(230, 130, 60, 210);
            }
            else
            {
                _barFill.BillBoardColor = new Color(230, 0, 0, 210);
            }

            _barFill.uvEnabled = true;
            _barFill.uvOffset = Vector2.Zero;
            _barFill.uvSize = new Vector2(fill, 1f);

            var barFillWidth = _bar.Width * fill;
            if (barFillWidth < 1f)
            {
                barFillWidth = 1f;
            }

            _barFill.Width = barFillWidth;

            _damageIconsBackground.Origin = _position;

            foreach (var damageIcon in _damageIcons.Values)
            {
                damageIcon.Origin = _position;
            }
        }

        private void UpdateStatusIcon(DateTime now, float damageAlpha, RechargeState rechargeState, float fill)
        {
            if (_statusIcon == null)
            {
                return;
            }

            if (damageAlpha > 0f || fill >= 1f || rechargeState == RechargeState.Off)
            {
                _wasChargingLastFrame = false;

                _statusIcon.Material = MyStringId.GetOrCompute(StatusIconMaterial);
                _statusIcon.BillBoardColor = StatusIconColor;
                return;
            }

            _statusIcon.Material = MyStringId.GetOrCompute(StatusIconChargeMaterial);

            if (rechargeState == RechargeState.BlockedLowEnergy)
            {
                _wasChargingLastFrame = false;
                _statusIcon.BillBoardColor = StatusIconChargeColorError;
                return;
            }

            if (!_wasChargingLastFrame)
            {
                _wasChargingLastFrame = true;
                _chargeAnimFlip = false;
                _chargeAnimNextSwitchUtc = now.AddSeconds(0.25);
            }
            else if (now >= _chargeAnimNextSwitchUtc)
            {
                _chargeAnimFlip = !_chargeAnimFlip;
                _chargeAnimNextSwitchUtc = now.AddSeconds(0.25);
            }

            _statusIcon.BillBoardColor = _chargeAnimFlip ? StatusIconChargeColor0 : StatusIconChargeColor1;
        }

        private void UpdateDamageIcons(HashSet<GroupDefinitions.Group> absorbGroups, float damageAlpha)
        {
            if (_damageIcons.Count == 0)
            {
                return;
            }

            foreach (var pair in _damageIcons)
            {
                var icon = pair.Value;
                if (icon == null)
                {
                    continue;
                }

                var enabled = absorbGroups != null && absorbGroups.Contains(pair.Key);
                icon.BillBoardColor = enabled ? DamageIconColorEnable : DamageIconColorDisable;
            }

            if (damageAlpha <= 0f)
            {
                return;
            }

            if (absorbGroups == null || !absorbGroups.Contains(_lastDamageGroup))
            {
                return;
            }

            HudAPIv2.BillBoardHUDMessage flashIcon;
            if (!_damageIcons.TryGetValue(_lastDamageGroup, out flashIcon) || flashIcon == null)
            {
                return;
            }

            flashIcon.BillBoardColor = LerpColor(DamageIconColorEnable, DamageIconColorDamage, damageAlpha);
        }

        private void ResetState()
        {
            _lastShieldSubtypeId = null;
            _lastBottleGasLevel = -1f;

            _wasChargingLastFrame = false;
            _chargeAnimFlip = false;
            _chargeAnimNextSwitchUtc = DateTime.MinValue;

            _hasRechargeStateFromServer = false;
            _rechargeStateFromServer = RechargeState.Off;
        }

        private static float ComputeAlpha(DateTime now, DateTime startUtc, double holdSeconds, double fadeSeconds)
        {
            if (startUtc == DateTime.MinValue)
            {
                return 0f;
            }

            var age = (now - startUtc).TotalSeconds;
            if (age < 0)
            {
                return 0f;
            }

            if (age <= holdSeconds)
            {
                return 1f;
            }

            var fadeAge = age - holdSeconds;
            if (fadeAge >= fadeSeconds)
            {
                return 0f;
            }

            var t = 1.0 - fadeAge / fadeSeconds;
            if (t < 0)
            {
                t = 0;
            }

            if (t > 1)
            {
                t = 1;
            }

            return (float)t;
        }

        private static Color LerpColor(Color fromColor, Color toColor, float blendFactor)
        {
            blendFactor = MathHelper.Clamp(blendFactor, 0f, 1f);

            var r = (byte)MathHelper.Clamp(fromColor.R + (toColor.R - fromColor.R) * blendFactor, 0f, 255f);
            var g = (byte)MathHelper.Clamp(fromColor.G + (toColor.G - fromColor.G) * blendFactor, 0f, 255f);
            var b = (byte)MathHelper.Clamp(fromColor.B + (toColor.B - fromColor.B) * blendFactor, 0f, 255f);
            var a = (byte)MathHelper.Clamp(fromColor.A + (toColor.A - fromColor.A) * blendFactor, 0f, 255f);

            return new Color(r, g, b, a);
        }

        private static string GetLocalizedShieldNameFromDefinition(string subtypeId, string fallbackKey)
        {
            if (!string.IsNullOrWhiteSpace(subtypeId))
            {
                try
                {
                    var defId = new MyDefinitionId(typeof(MyObjectBuilder_GasContainerObject), subtypeId);
                    var def = MyDefinitionManager.Static != null
                        ? MyDefinitionManager.Static.GetPhysicalItemDefinition(defId)
                        : null;

                    if (def != null && !string.IsNullOrWhiteSpace(def.DisplayNameText))
                    {
                        return def.DisplayNameText;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            if (string.IsNullOrWhiteSpace(fallbackKey))
            {
                return !string.IsNullOrWhiteSpace(subtypeId) ? subtypeId : "Shield";
            }

            try
            {
                var text = MyTexts.GetString(MyStringId.GetOrCompute(fallbackKey));
                if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, fallbackKey, StringComparison.Ordinal))
                {
                    return text;
                }
            }
            catch
            {
                // ignored
            }

            return !string.IsNullOrWhiteSpace(subtypeId) ? subtypeId : "Shield";
        }

        private void SetAllVisible(bool visible)
        {
            if (_background != null)
            {
                _background.Visible = visible;
            }

            if (_name != null)
            {
                _name.Visible = visible;
            }

            if (_statusIcon != null)
            {
                _statusIcon.Visible = visible;
            }

            if (_bar != null)
            {
                _bar.Visible = visible;
            }

            if (_barFill != null)
            {
                _barFill.Visible = visible;
            }

            if (_capacity != null)
            {
                _capacity.Visible = visible;
            }

            foreach (var icon in _damageIcons.Values)
            {
                if (icon != null)
                {
                    icon.Visible = visible;
                }
            }

            if (_damageIconsBackground != null)
            {
                _damageIconsBackground.Visible = visible;
            }
        }

        public void Dispose()
        {
            if (_api != null)
            {
                _api.OnScreenDimensionsChanged = null;
            }
        }

        public void NotifyDamage(long victimIdentityId, GroupDefinitions.Group group)
        {
            var local = MyAPIGateway.Session != null ? MyAPIGateway.Session.LocalHumanPlayer : null;
            if (local == null)
            {
                return;
            }

            if (local.IdentityId != victimIdentityId)
            {
                return;
            }

            _lastDamageGroup = group;
            _lastDamageUtc = DateTime.UtcNow;
        }

        private void OnRegistered()
        {
        }

        private void OnScreenDimensionsChanged()
        {
        }
    }
}
