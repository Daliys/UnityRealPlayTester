using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Core;
using RealPlayTester.Await;

namespace RealPlayTester.Tests.Verification
{
    [PlayModeTest(TimeScale = 1.0f)]
    public class PlayerMovementTests : RealPlayTest
    {
        private GameObject _player;
        private Rigidbody2D _rb;
        private Vector2 _startPos;
        private PlayerController _playerController;

        protected override async Task Run()
        {
            EnsurePlayerExists();
            ResetPlayerState();

            await Test_CardinalMovement();
            await Test_DiagonalMovement();
            await Test_SpecialKeyP();
            await Test_DiagonalQuadrants();
            await Test_LongMovement();
            await Test_MovementSequence();
            await Test_PhysicsStability();
            
            Log("All Player Movement tests finished!");
        }

        private void EnsurePlayerExists()
        {
            _player = GameObject.Find("Player");
            if (_player == null)
            {
                Log("Player not found in scene, creating one for testing...");
                _player = new GameObject("Player");
                _rb = _player.AddComponent<Rigidbody2D>();
                _rb.gravityScale = 0;
                _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                _playerController = _player.AddComponent<PlayerController>();
            }
            else
            {
                _rb = _player.GetComponent<Rigidbody2D>();
                _playerController = _player.GetComponent<PlayerController>();
            }

            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.gravityScale = 0;
            }

            if (_rb == null || _playerController == null)
            {
                Tester.Assert.Fail("Player object exists but is missing Rigidbody2D or PlayerController");
            }
        }

        private void ResetPlayerState()
        {
            _player.transform.position = Vector3.zero;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0;
        }

        public async Task Test_SpecialKeyP()
        {
            float moveDuration = 0.5f;
            float tolerance = 0.1f;
            
            _startPos = _player.transform.position;
            Log("Pressing P (Special Diagonal)...");
            await Tester.Interaction.Keyboard.Down(KeyCode.P);
            await Wait.Seconds(moveDuration);
            await Tester.Interaction.Keyboard.Up(KeyCode.P);
            await Wait.Frames(10);
            
            float deltaX = ((Vector2)_player.transform.position).x - _startPos.x;
            float deltaY = ((Vector2)_player.transform.position).y - _startPos.y;
            Log($"Moved by P Final: DeltaX = {deltaX}, DeltaY = {deltaY}");
            Tester.Assert.IsTrue(deltaX > tolerance && deltaY > tolerance, "Player should have moved diagonally (Forward+Right) when P is pressed");
        }

        public async Task Test_CardinalMovement()
        {
            float duration = 0.5f;
            float tolerance = 0.1f;
            
            await VerifyMovement(KeyCode.W, "Forward (Y+)", (p, s) => p.y - s.y > tolerance, duration);
            await VerifyMovement(KeyCode.A, "Left (X-)", (p, s) => p.x - s.x < -tolerance, duration);
            await VerifyMovement(KeyCode.S, "Backward (Y-)", (p, s) => p.y - s.y < -tolerance, duration);
            await VerifyMovement(KeyCode.D, "Right (X+)", (p, s) => p.x - s.x > tolerance, duration);
        }

        private async Task VerifyMovement(KeyCode key, string direction, System.Func<Vector2, Vector2, bool> validator, float duration)
        {
            _startPos = _player.transform.position;
            Log($"Testing {direction} movement with {key}...");
            
            await Tester.Interaction.Keyboard.Down(key);
            float startTime = Time.time;
            while (Time.time - startTime < duration)
            {
                await Wait.Frames(1);
                LogProgress(key, direction);
            }
            await Tester.Interaction.Keyboard.Up(key);
            await Wait.Frames(10);
            
            var endPos = (Vector2)_player.transform.position;
            Tester.Assert.IsTrue(validator(endPos, _startPos), $"Player failed to move correctly in direction: {direction}");
            Log($"{direction} movement verified. Final Delta: {endPos - (Vector2)_startPos}");
        }

        private void LogProgress(KeyCode key, string direction)
        {
            var currentPos = (Vector2)_player.transform.position;
            bool isHeld = Tester.Interaction.Keyboard.Held(key);
            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool directPressed = kb != null && ((UnityEngine.InputSystem.Controls.KeyControl)kb[key.ToString().ToLower()]).isPressed;
            Vector2 moveVal = _playerController.moveAction != null ? _playerController.moveAction.ReadValue<Vector2>() : Vector2.zero;
            Log($"{direction} Progress: {currentPos} - Held: {isHeld}, Direct: {directPressed}, Action: {moveVal}");
        }

        public async Task Test_DiagonalMovement()
        {
            float moveDuration = 0.5f;
            float tolerance = 0.05f;

            // Forward + Right (W+D)
            _startPos = _player.transform.position;
            Log("Pressing W+D...");
            await Tester.Interaction.Keyboard.Down(KeyCode.W);
            await Tester.Interaction.Keyboard.Down(KeyCode.D);
            await Wait.Seconds(moveDuration);
            await Tester.Interaction.Keyboard.Up(KeyCode.W);
            await Tester.Interaction.Keyboard.Up(KeyCode.D);
            await Wait.Frames(10);

            float deltaX = ((Vector2)_player.transform.position).x - _startPos.x;
            float deltaY = ((Vector2)_player.transform.position).y - _startPos.y;
            Log($"Moved Diagonal (W+D): DeltaX={deltaX}, DeltaY={deltaY}");
            Tester.Assert.IsTrue(deltaX > tolerance && deltaY > tolerance, "Should move NE");
        }

        public async Task Test_DiagonalQuadrants()
        {
            float moveDuration = 0.5f;
            float tolerance = 0.1f;

            // O (Up-Left)
            _startPos = _player.transform.position;
            Log("Pressing O (Up-Left)...");
            await Tester.Interaction.Keyboard.Down(KeyCode.O);
            await Wait.Seconds(moveDuration);
            await Tester.Interaction.Keyboard.Up(KeyCode.O);
            await Wait.Frames(10);
            Vector2 delta = (Vector2)_player.transform.position - _startPos;
            Log($"Moved by O: {delta}");
            Tester.Assert.IsTrue(delta.x < -tolerance && delta.y > tolerance, "O should move NW");

            // K (Down-Left)
            _startPos = _player.transform.position;
            Log("Pressing K (Down-Left)...");
            await Tester.Interaction.Keyboard.Down(KeyCode.K);
            await Wait.Seconds(moveDuration);
            await Tester.Interaction.Keyboard.Up(KeyCode.K);
            await Wait.Frames(10);
            delta = (Vector2)_player.transform.position - _startPos;
            Log($"Moved by K: {delta}");
            Tester.Assert.IsTrue(delta.x < -tolerance && delta.y < -tolerance, "K should move SW");

            // L (Down-Right)
            _startPos = _player.transform.position;
            Log("Pressing L (Down-Right)...");
            await Tester.Interaction.Keyboard.Down(KeyCode.L);
            await Wait.Seconds(moveDuration);
            await Tester.Interaction.Keyboard.Up(KeyCode.L);
            await Wait.Frames(10);
            delta = (Vector2)_player.transform.position - _startPos;
            Log($"Moved by L: {delta}");
            Tester.Assert.IsTrue(delta.x > tolerance && delta.y < -tolerance, "L should move SE");
        }

        public async Task Test_LongMovement()
        {
            Log("Testing Long Movement (4 seconds)...");
            _startPos = _player.transform.position;
            await Tester.Interaction.Keyboard.Down(KeyCode.D);
            await Wait.Seconds(4.0f);
            await Tester.Interaction.Keyboard.Up(KeyCode.D);
            await Wait.Frames(10);
            float dist = Vector2.Distance(_player.transform.position, _startPos);
            Log($"Long Move Distance: {dist}");
            // At speed 3, 4s should be ~12 units.
            Tester.Assert.IsTrue(dist > 10f, $"Player should have moved far (Expected >10, actual {dist})");
        }

        public async Task Test_MovementSequence()
        {
            Log("Testing Movement Sequence (Right -> Up -> P)...");
            _startPos = _player.transform.position;
            
            await Tester.Interaction.Keyboard.Down(KeyCode.D);
            await Wait.Seconds(1.0f);
            await Tester.Interaction.Keyboard.Up(KeyCode.D);
            
            await Tester.Interaction.Keyboard.Down(KeyCode.W);
            await Wait.Seconds(1.0f);
            await Tester.Interaction.Keyboard.Up(KeyCode.W);
            
            await Tester.Interaction.Keyboard.Down(KeyCode.P);
            await Wait.Seconds(1.0f);
            await Tester.Interaction.Keyboard.Up(KeyCode.P);
            
            await Wait.Frames(10);
            Vector2 current = _player.transform.position;
            Log($"Sequence Final Position: {current}");
            // Calculated at speed 3: (1*3, 0) + (0, 1*3) + (1*2.12, 1*2.12) = (5.12, 5.12) approx
            Tester.Assert.IsTrue(current.x > 4.5f && current.y > 4.5f, $"Sequence movement failed. Pos: {current}");
        }

        public async Task Test_PhysicsStability()
        {
            Log("Checking Physics Stability...");
            await Wait.Seconds(0.5f);
            Vector2 posBefore = _player.transform.position;
            await Wait.Seconds(0.5f);
            Vector2 posAfter = _player.transform.position;
            
            float drift = Vector2.Distance(posBefore, posAfter);
            Log($"Stability Drift: {drift}");
            Tester.Assert.IsTrue(drift < 0.01f, "Player should not drift when no keys are pressed");
        }
    }
}
