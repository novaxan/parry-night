using NUnit.Framework;
using UnityEngine;

namespace NightShift.Tests
{
    public class RobotStateTests
    {
        RobotData data;

        [SetUp]
        public void SetUp()
        {
            data = ScriptableObject.CreateInstance<RobotData>();
            data.maxHealth = 3f;
            data.moveInterval = 5f;
            data.startAggression = 20;
            data.lurkTime = new Vector2(4f, 4f);
            data.telegraphTime = 0.8f;
            data.lungeTime = 0.5f;
            data.parryWindow = 0.25f;
            data.stunTime = 2f;
            data.stunnedDamageMultiplier = 3f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(data);

        RobotBrain Make(float roll = 0f) => new RobotBrain(data, 4, () => roll);

        /// <summary>Brain at the opening, run through lurk + telegraph into the start of the lunge.</summary>
        RobotBrain AtLungeStart()
        {
            var b = Make();
            b.Place(b.OpeningIndex);
            b.Tick(4.01f);
            Assert.AreEqual(RobotState.Telegraph, b.State);
            b.Tick(0.81f);
            Assert.AreEqual(RobotState.Lunge, b.State);
            return b;
        }

        [Test]
        public void Roaming_AdvancesOnSuccessfulRoll_AndReachesOpening()
        {
            var b = Make(roll: 0f);
            b.Place(0);
            b.Tick(5.01f); Assert.AreEqual(1, b.RouteIndex);
            b.Tick(5f); Assert.AreEqual(2, b.RouteIndex);
            b.Tick(5f);
            Assert.AreEqual(3, b.RouteIndex);
            Assert.AreEqual(RobotState.AtOpening, b.State);
        }

        [Test]
        public void Roaming_StaysPut_WhenAggressionIsZero()
        {
            data.startAggression = 0;
            var b = Make(roll: 0f);
            b.Place(0);
            b.Tick(50f);
            Assert.AreEqual(0, b.RouteIndex);
        }

        [Test]
        public void Parry_TooEarly_Fails()
        {
            var b = AtLungeStart();
            b.Tick(0.1f);   // 0.4 s left; the window is the last 0.25 s
            Assert.IsFalse(b.TryParry());
            Assert.AreEqual(RobotState.Lunge, b.State);
        }

        [Test]
        public void Parry_InWindow_Stuns()
        {
            var b = AtLungeStart();
            b.Tick(0.3f);   // 0.2 s left
            Assert.IsTrue(b.InParryWindow);
            Assert.IsTrue(b.TryParry());
            Assert.AreEqual(RobotState.Stunned, b.State);
        }

        [Test]
        public void MissedParry_Attacks()
        {
            var b = AtLungeStart();
            b.Tick(0.51f);
            Assert.AreEqual(RobotState.Attacked, b.State);
        }

        [Test]
        public void Parry_DuringTelegraph_Fails()
        {
            var b = Make();
            b.Place(b.OpeningIndex);
            b.Tick(4.01f);
            Assert.IsFalse(b.TryParry());
        }

        [Test]
        public void ShotWhileStunned_DealsTripleDamage_AndKills()
        {
            var b = AtLungeStart();
            b.Tick(0.3f);
            b.TryParry();
            float dmg = b.TakeShot(1f);
            Assert.AreEqual(3f, dmg);
            Assert.AreEqual(RobotState.Dead, b.State);
            Assert.IsFalse(b.IsAlive);
        }

        [Test]
        public void ShotWhileLurking_KnocksBackOneRoom()
        {
            var b = Make();
            b.Place(b.OpeningIndex);
            float dmg = b.TakeShot(1f);
            Assert.AreEqual(1f, dmg);
            Assert.AreEqual(2f, b.Health);
            Assert.AreEqual(RobotState.Roaming, b.State);
            Assert.AreEqual(b.OpeningIndex - 1, b.RouteIndex);
        }

        [Test]
        public void ShotDuringLunge_DamagesButDoesNotStopIt()
        {
            var b = AtLungeStart();
            b.TakeShot(1f);
            Assert.AreEqual(RobotState.Lunge, b.State);
            b.Tick(0.51f);
            Assert.AreEqual(RobotState.Attacked, b.State);
        }

        [Test]
        public void ShotWhileRoaming_HasNoEffect()
        {
            var b = Make();
            b.Place(1);
            Assert.AreEqual(0f, b.TakeShot(1f));
            Assert.AreEqual(3f, b.Health);
        }

        [Test]
        public void StunWearsOff_KnocksBackTwoRooms()
        {
            var b = AtLungeStart();
            b.Tick(0.3f);
            b.TryParry();
            b.Tick(2.01f);
            Assert.AreEqual(RobotState.Roaming, b.State);
            Assert.AreEqual(b.OpeningIndex - 2, b.RouteIndex);
        }
    }
}
