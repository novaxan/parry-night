using NUnit.Framework;

namespace NightShift.Tests
{
    public class ShotgunTests
    {
        static ShotgunModel Make(int loaded = 2, int reserve = 6) =>
            new ShotgunModel(tubeCapacity: 2, startLoaded: loaded, startReserve: reserve, pumpTime: 0.5f, reloadPerShell: 0.9f, parryCooldown: 0.6f);

        [Test]
        public void Fire_ConsumesShell_AndMustPumpBeforeNextShot()
        {
            var g = Make();
            Assert.IsTrue(g.TryFire());
            Assert.AreEqual(1, g.Loaded);
            Assert.IsFalse(g.TryFire(), "should be pumping");
            g.Tick(0.49f);
            Assert.IsFalse(g.TryFire());
            g.Tick(0.02f);
            Assert.IsTrue(g.TryFire());
            Assert.AreEqual(0, g.Loaded);
        }

        [Test]
        public void Fire_WithEmptyTube_Fails()
        {
            var g = Make(loaded: 0);
            Assert.IsFalse(g.TryFire());
        }

        [Test]
        public void Reload_InsertsOneShellPerInterval_UntilFull()
        {
            var g = Make(loaded: 0, reserve: 6);
            Assert.IsTrue(g.TryStartReload());
            g.Tick(0.89f);
            Assert.AreEqual(0, g.Loaded);
            g.Tick(0.02f);
            Assert.AreEqual(1, g.Loaded);
            Assert.AreEqual(5, g.Reserve);
            g.Tick(0.9f);
            Assert.AreEqual(2, g.Loaded);
            Assert.AreEqual(4, g.Reserve);
            Assert.AreEqual(ShotgunModel.GunState.Idle, g.Current);
        }

        [Test]
        public void Reload_StopsWhenReserveRunsOut()
        {
            var g = Make(loaded: 0, reserve: 1);
            g.TryStartReload();
            g.Tick(1f);
            Assert.AreEqual(1, g.Loaded);
            Assert.AreEqual(0, g.Reserve);
            Assert.AreEqual(ShotgunModel.GunState.Idle, g.Current);
            Assert.IsFalse(g.TryStartReload());
        }

        [Test]
        public void Fire_InterruptsReload()
        {
            var g = Make(loaded: 1, reserve: 6);
            g.TryStartReload();
            Assert.IsTrue(g.TryFire());
            Assert.AreEqual(0, g.Loaded);
            Assert.AreEqual(ShotgunModel.GunState.Pumping, g.Current);
            Assert.AreEqual(6, g.Reserve);
        }

        [Test]
        public void Parry_CancelsReload_AndHasCooldown()
        {
            var g = Make(loaded: 0, reserve: 6);
            g.TryStartReload();
            Assert.IsTrue(g.TryParry());
            Assert.AreEqual(ShotgunModel.GunState.Idle, g.Current);
            Assert.IsFalse(g.TryParry(), "cooldown");
            g.Tick(0.61f);
            Assert.IsTrue(g.TryParry());
        }

        [Test]
        public void Parry_WorksWithNoAmmo()
        {
            var g = Make(loaded: 0, reserve: 0);
            Assert.IsTrue(g.IsEmpty);
            Assert.IsTrue(g.TryParry());
        }
    }
}
