using System;

namespace NightShift
{
    /// <summary>
    /// Pure ammo / pump / reload / parry-cooldown rules for the shotgun. No Unity dependencies so it
    /// can be unit tested; <see cref="Shotgun"/> drives it every frame.
    /// </summary>
    public class ShotgunModel
    {
        public enum GunState { Idle, Pumping, Reloading }

        public int TubeCapacity { get; }
        public int Loaded { get; private set; }
        public int Reserve { get; private set; }
        public GunState Current { get; private set; } = GunState.Idle;
        public float ActionTimer { get; private set; }
        public float ParryCooldown { get; private set; }

        public readonly float PumpTime;
        public readonly float ReloadPerShell;
        public readonly float ParryCooldownTime;

        public event System.Action<int> ShellInserted;   // arg: shells now loaded
        public event System.Action PumpFinished;

        public ShotgunModel(int tubeCapacity = 2, int startLoaded = 2, int startReserve = 6,
            float pumpTime = 0.5f, float reloadPerShell = 0.9f, float parryCooldown = 0.6f)
        {
            TubeCapacity = tubeCapacity;
            Loaded = Math.Min(startLoaded, tubeCapacity);
            Reserve = startReserve;
            PumpTime = pumpTime;
            ReloadPerShell = reloadPerShell;
            ParryCooldownTime = parryCooldown;
        }

        public bool IsBusy => Current != GunState.Idle;
        public bool CanFire => Loaded > 0 && Current != GunState.Pumping;
        public bool CanReload => Current == GunState.Idle && Loaded < TubeCapacity && Reserve > 0;
        public bool CanParry => ParryCooldown <= 0f;
        public bool IsEmpty => Loaded == 0 && Reserve == 0;

        /// <summary>Fires one shell. Firing interrupts a reload. Returns false if it could not fire.</summary>
        public bool TryFire()
        {
            if (!CanFire) return false;
            Loaded--;
            Current = GunState.Pumping;
            ActionTimer = PumpTime;
            return true;
        }

        public bool TryStartReload()
        {
            if (!CanReload) return false;
            Current = GunState.Reloading;
            ActionTimer = ReloadPerShell;
            return true;
        }

        /// <summary>Parrying shoves with the gun: it cancels a reload and starts a cooldown so it can't be spammed.</summary>
        public bool TryParry()
        {
            if (!CanParry) return false;
            if (Current == GunState.Reloading) Current = GunState.Idle;
            ParryCooldown = ParryCooldownTime;
            return true;
        }

        public void AddReserve(int shells) => Reserve += Math.Max(0, shells);

        public void Tick(float dt)
        {
            if (ParryCooldown > 0f) ParryCooldown = Math.Max(0f, ParryCooldown - dt);
            if (Current == GunState.Idle) return;

            ActionTimer -= dt;
            if (ActionTimer > 0f) return;

            if (Current == GunState.Pumping)
            {
                Current = GunState.Idle;
                PumpFinished?.Invoke();
            }
            else if (Current == GunState.Reloading)
            {
                Loaded++;
                Reserve--;
                ShellInserted?.Invoke(Loaded);
                if (Loaded < TubeCapacity && Reserve > 0)
                    ActionTimer += ReloadPerShell;   // keep feeding shells
                else
                    Current = GunState.Idle;
            }
        }
    }
}
