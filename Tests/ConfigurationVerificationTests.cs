using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Core;
using RealPlayTester.Await;

namespace RealPlayTester.Tests.Verification
{
    [PlayModeTest]
    public class ConfigurationVerificationTests : RealPlayTest
    {
        private GameObject _player;
        private Vector2 _startPos;

        protected override async Task Run()
        {
            _player = GameObject.Find("Player");
            if (_player == null)
            {
                _player = new GameObject("Player_ConfigTest");
                _player.AddComponent<Rigidbody2D>().gravityScale = 0;
                _player.AddComponent<PlayerController>();
            }

            await Test_PhysicsToggles();
            Log("Configuration Verification Finalized!");
        }

        public async Task Test_PhysicsToggles()
        {
            if (!Application.isBatchMode)
            {
                Log("Skipping PhysicsToggle test - only valid in BatchMode");
                return;
            }

            // 1. Test ENABLED (Default)
            RealPlaySettings.AutoSimulatePhysics2D = true;
            _startPos = _player.transform.position;
            await Tester.Interaction.Keyboard.Down(KeyCode.D);
            await Wait.Seconds(0.5f);
            await Tester.Interaction.Keyboard.Up(KeyCode.D);
            await Wait.Frames(5);
            
            float distEnabled = Vector2.Distance(_player.transform.position, _startPos);
            Log($"Distance (AutoSimulate=true): {distEnabled}");
            Tester.Assert.IsTrue(distEnabled > 0.1f, "Player should move when physics simulation is enabled");

            // 2. Test DISABLED
            RealPlaySettings.AutoSimulatePhysics2D = false;
            // Note: We need to manually set simulation mode back if we want to prove it's NOT ticking
            Physics2D.simulationMode = SimulationMode2D.Script; 
            
            _startPos = _player.transform.position;
            await Tester.Interaction.Keyboard.Down(KeyCode.D);
            await Wait.Seconds(0.5f);
            await Tester.Interaction.Keyboard.Up(KeyCode.D);
            await Wait.Frames(5);

            float distDisabled = Vector2.Distance(_player.transform.position, _startPos);
            Log($"Distance (AutoSimulate=false): {distDisabled}");
            Tester.Assert.IsTrue(distDisabled < 0.01f, "Player should NOT move when physics simulation is disabled");
            
            // Restore for other tests
            RealPlaySettings.AutoSimulatePhysics2D = true;
        }
    }
}
