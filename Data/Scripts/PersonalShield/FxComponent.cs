using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Audio;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace CSEPersonalShield
{
    public sealed class FxComponent
    {
        private readonly Dictionary<long, FxState> _fxByCharacter = new Dictionary<long, FxState>(32);
        private MySoundPair _soundPair;

        private struct FxState
        {
            public int ExpireTick;
            public int LastRefreshTick;
            public int LastSoundTick;
        }

        public void Init()
        {
            _fxByCharacter.Clear();

            if (MyAPIGateway.Utilities == null || MyAPIGateway.Utilities.IsDedicated)
            {
                return;
            }

            try
            {
                _soundPair = new MySoundPair(Config.FxHitSoundCue);
            }
            catch
            {
                _soundPair = null;
            }
        }

        public void Dispose()
        {
            _fxByCharacter.Clear();
            _soundPair = null;
        }

        public void Update(int tick)
        {
            if (MyAPIGateway.Utilities == null || MyAPIGateway.Utilities.IsDedicated)
            {
                return;
            }

            Cleanup(tick);
        }

        public void OnFx(long characterEntityId, long victimIdentityId, int tick)
        {
            if (MyAPIGateway.Utilities == null || MyAPIGateway.Utilities.IsDedicated)
            {
                return;
            }

            FxState state;
            var has = _fxByCharacter.TryGetValue(characterEntityId, out state);

            if (has)
            {
                if (tick - state.LastRefreshTick < Config.FxRefreshMinTicks)
                {
                    state.ExpireTick = tick + Config.FxDurationTicks;
                    _fxByCharacter[characterEntityId] = state;
                    return;
                }
            }

            state.ExpireTick = tick + Config.FxDurationTicks;
            state.LastRefreshTick = tick;

            if (!has)
            {
                state.LastSoundTick = -Config.FxSoundCooldownTicks;
            }

            var localId = GetLocalIdentityId();
            var shouldPlaySound = localId != 0 && localId == victimIdentityId;

            if (shouldPlaySound)
            {
                if (tick - state.LastSoundTick >= Config.FxSoundCooldownTicks)
                {
                    state.LastSoundTick = tick;
                    PlayLocalSound();
                }
            }

            _fxByCharacter[characterEntityId] = state;
        }

        private void Cleanup(int tick)
        {
            if (_fxByCharacter.Count == 0)
            {
                return;
            }

            var toRemove = new List<long>(8);

            foreach (var kv in _fxByCharacter)
            {
                if (tick > kv.Value.ExpireTick)
                {
                    toRemove.Add(kv.Key);
                }
            }

            foreach (var id in toRemove)
            {
                _fxByCharacter.Remove(id);
            }
        }

        private static long GetLocalIdentityId()
        {
            try
            {
                var session = MyAPIGateway.Session;
                if (session == null)
                {
                    return 0;
                }

                var lp = session.LocalHumanPlayer;
                if (lp != null)
                {
                    return lp.IdentityId;
                }

                var p = session.Player;
                if (p != null)
                {
                    return p.IdentityId;
                }
            }
            catch
            {
                // ignored
            }

            return 0;
        }

        private void PlayLocalSound()
        {
            if (string.IsNullOrWhiteSpace(Config.FxHitSoundCue))
            {
                return;
            }

            if (_soundPair == null)
            {
                try
                {
                    _soundPair = new MySoundPair(Config.FxHitSoundCue);
                }
                catch
                {
                    _soundPair = null;
                    return;
                }
            }

            try
            {
                var lp = MyAPIGateway.Session != null ? MyAPIGateway.Session.LocalHumanPlayer : null;
                var ch = lp?.Character;

                if (ch == null)
                {
                    return;
                }

                var ent = ch as IMyEntity;
                var myEnt = ent as MyEntity;

                if (myEnt == null)
                {
                    FallbackPlaySoundAtPosition(ent.GetPosition());
                    return;
                }

                var emitter = new MyEntity3DSoundEmitter(myEnt);
                emitter.PlaySound(_soundPair);
            }
            catch
            {
                try
                {
                    var lp = MyAPIGateway.Session != null ? MyAPIGateway.Session.LocalHumanPlayer : null;
                    var ch = lp?.Character;
                    var pos = ch?.GetPosition() ?? Vector3D.Zero;
                    FallbackPlaySoundAtPosition(pos);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static void FallbackPlaySoundAtPosition(Vector3D pos)
        {
            try
            {
                MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(Config.FxHitSoundCue, pos);
            }
            catch
            {
                // ignored
            }
        }
    }
}
