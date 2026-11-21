using System;

namespace NightShift
{
    public enum RobotState { Roaming, AtOpening, Telegraph, Lunge, Stunned, Attacked, Dead }

    /// <summary>
    /// Pure state machine for a robot walking a linear route of rooms toward one office opening.
    /// The last route node is the opening itself. No Unity dependencies so it can be unit tested.
    /// </summary>
    public class RobotBrain
    {
        public RobotData Data { get; }
        public int RouteLength { get; }
        public int RouteIndex { get; private set; }
        public RobotState State { get; private set; } = RobotState.Roaming;
        public float Health { get; private set; }
        public float StateTimer { get; private set; }
        public int Aggression { get; set; }

        float moveTimer;
        readonly Func<float> random01;

        public event Action<RobotState> StateChanged;
        public event Action<int> Moved;   // arg: new route index

        public RobotBrain(RobotData data, int routeLength, Func<float> random01)
        {
            if (routeLength < 2) throw new ArgumentException("Route needs at least a start room and an opening.");
            Data = data;
            RouteLength = routeLength;
            this.random01 = random01;
            Health = data.maxHealth;
            Aggression = data.startAggression;
            moveTimer = data.moveInterval;
        }

        public int OpeningIndex => RouteLength - 1;
        public bool IsAlive => State != RobotState.Dead;
        /// <summary>True while the robot stands in the opening and can be shot or parried.</summary>
        public bool IsAtOpening => State is RobotState.AtOpening or RobotState.Telegraph or RobotState.Lunge or RobotState.Stunned;
        /// <summary>True during the last <see cref="RobotData.parryWindow"/> seconds of the lunge.</summary>
        public bool InParryWindow => State == RobotState.Lunge && StateTimer <= Data.parryWindow;

        /// <summary>
        /// 0 when the telegraph starts, 1 the instant the parry window opens; -1 when not attacking.
        /// Drives the shrinking parry ring around the crosshair.
        /// </summary>
        public float ParryCue
        {
            get
            {
                float lead = Data.telegraphTime + Data.lungeTime - Data.parryWindow;
                if (lead <= 0f) return State is RobotState.Telegraph or RobotState.Lunge ? 1f : -1f;
                float elapsed = State switch
                {
                    RobotState.Telegraph => Data.telegraphTime - StateTimer,
                    RobotState.Lunge => Data.telegraphTime + (Data.lungeTime - StateTimer),
                    _ => -1f
                };
                return elapsed < 0f ? -1f : Math.Min(1f, elapsed / lead);
            }
        }

        public void Tick(float dt)
        {
            switch (State)
            {
                case RobotState.Roaming:
                    moveTimer -= dt;
                    if (moveTimer > 0f) break;
                    moveTimer += Data.moveInterval;
                    if (random01() * 20f < Aggression) Advance();
                    break;

                case RobotState.AtOpening:
                    StateTimer -= dt;
                    if (StateTimer <= 0f) SetState(RobotState.Telegraph, Data.telegraphTime);
                    break;

                case RobotState.Telegraph:
                    StateTimer -= dt;
                    if (StateTimer <= 0f) SetState(RobotState.Lunge, Data.lungeTime);
                    break;

                case RobotState.Lunge:
                    StateTimer -= dt;
                    if (StateTimer <= 0f) SetState(RobotState.Attacked, 0f);
                    break;

                case RobotState.Stunned:
                    StateTimer -= dt;
                    if (StateTimer <= 0f) KnockBack(2);
                    break;
            }
        }

        /// <summary>Called when the player parries toward this robot's opening. Only works inside the window.</summary>
        public bool TryParry()
        {
            if (!InParryWindow) return false;
            SetState(RobotState.Stunned, Data.stunTime);
            return true;
        }

        /// <summary>
        /// A shotgun blast. Lurking robots get knocked back a room; a robot that is already telegraphing
        /// or lunging is committed and keeps coming (you must parry). Stunned robots take bonus damage.
        /// Returns the damage dealt (0 if the robot wasn't hittable).
        /// </summary>
        public float TakeShot(float baseDamage)
        {
            if (!IsAtOpening) return 0f;
            float dmg = State == RobotState.Stunned ? baseDamage * Data.stunnedDamageMultiplier : baseDamage;
            Health -= dmg;
            if (Health <= 0f)
            {
                Health = 0f;
                SetState(RobotState.Dead, 0f);
            }
            else if (State is RobotState.AtOpening or RobotState.Stunned)
            {
                KnockBack(1);
            }
            return dmg;
        }

        /// <summary>Forces a position on the route (used for setup and tests).</summary>
        public void Place(int routeIndex)
        {
            RouteIndex = Math.Clamp(routeIndex, 0, OpeningIndex);
            if (RouteIndex == OpeningIndex) EnterOpening();
            else SetState(RobotState.Roaming, 0f);
            Moved?.Invoke(RouteIndex);
        }

        void Advance()
        {
            RouteIndex++;
            Moved?.Invoke(RouteIndex);
            if (RouteIndex >= OpeningIndex) EnterOpening();
        }

        void EnterOpening()
        {
            float lurk = Data.lurkTime.x + random01() * (Data.lurkTime.y - Data.lurkTime.x);
            SetState(RobotState.AtOpening, lurk);
        }

        void KnockBack(int rooms)
        {
            RouteIndex = Math.Max(0, RouteIndex - rooms);
            moveTimer = Data.moveInterval;
            SetState(RobotState.Roaming, 0f);
            Moved?.Invoke(RouteIndex);
        }

        void SetState(RobotState s, float timer)
        {
            State = s;
            StateTimer = timer;
            StateChanged?.Invoke(s);
        }
    }
}
