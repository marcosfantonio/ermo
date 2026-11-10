using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Ermo
{
    // Preloads one additive destination; the same player, camera, capture/history
    // and output/UI survive the crossing. No loading screen, second player or fade.
    [DefaultExecutionOrder(40)]
    public sealed class WaterPassage : MonoBehaviour
    {
        public enum PassageState { Exploring, Climbing, Diving, Emerging, WaterWorld }
        public PassageState State { get; private set; }
        public float Immersion { get; private set; }
        public WaterImmersionEffects Effects { get; private set; }
        public bool InWaterWorld => State == PassageState.WaterWorld || State == PassageState.Emerging;
        private static AsyncOperation preload;
        private FirstPersonWalker walker;
        private GameObject sourceEnvironment;
        private WaterTowerAccess ladder, candidate;
        private float climbHeight, elapsed, entryPitch, sourceYaw;
        private Vector3 entryPosition, deepPosition, destinationDeep;
        private GUIStyle prompt;
        private Vector3 previousHead;
        private bool mantling;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPreload() => preload = null;

        public void Initialize(FirstPersonWalker player, GameObject source, WorldSettings settings)
        {
            walker = player; sourceEnvironment = source;
            Effects = gameObject.AddComponent<WaterImmersionEffects>(); Effects.Initialize(settings);
            previousHead = player.View.transform.position;
            if (WaterWorld.Instance == null && (preload == null || preload.isDone))
                preload = SceneManager.LoadSceneAsync(WaterWorld.ScenePath,LoadSceneMode.Additive);
        }

        private void Update()
        {
            if (walker == null) return;
            Effects.Tick(Immersion,walker.MenuOpen,Time.deltaTime);
            if (walker.MenuOpen || walker.WakingUp || !walker.enabled) return;
            if (State == PassageState.Climbing) { TickLadder(); return; }
            if (State == PassageState.Diving || State == PassageState.Emerging) { TickCrossing(); return; }
            if (State == PassageState.WaterWorld) return;
            candidate = null;
            foreach (var tower in WaterTowerAccess.Live)
            {
                if (tower.NearLadder(walker.transform.position)) candidate = tower;
                if (tower.ContainsHead(walker.View.transform.position) && previousHead.y > tower.SurfaceCenter.y && walker.VerticalSpeed < 0)
                {
                    BeginDive(tower); return;
                }
            }
            if (candidate != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                AttachLadder(candidate);
        }

        private void LateUpdate() { if (walker != null) previousHead = walker.View.transform.position; }

        public bool AttachLadder(WaterTowerAccess tower)
        {
            if (tower == null || !tower.NearLadder(walker.transform.position) || !walker.BeginTraversal(false)) return false;
            ladder = tower; State = PassageState.Climbing;
            mantling = false;
            climbHeight = Mathf.Clamp(tower.transform.InverseTransformPoint(walker.transform.position).y,.05f,WaterTowerAccess.RimHeight);
            return true;
        }

        private void TickLadder()
        {
            if (ladder == null) { walker.EndTraversal(); State = PassageState.Exploring; return; }
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            {
                walker.EndTraversal(keyboard.spaceKey.wasPressedThisFrame); State = PassageState.Exploring; ladder = null; return;
            }
            float input = (keyboard.wKey.isPressed ? 1 : 0)-(keyboard.sKey.isPressed ? 1 : 0);
            AdvanceClimb(input,Time.deltaTime);
        }

        public void AdvanceClimb(float input, float dt)
        {
            if (State != PassageState.Climbing || ladder == null) return;
            climbHeight = Mathf.Clamp(climbHeight+input*2.4f*dt/ladder.transform.lossyScale.y,.05f,WaterTowerAccess.RimHeight+.18f);
            Vector3 target = mantling ? ladder.TopLanding : ladder.LadderPoint(climbHeight);
            walker.MoveOnLadder(Vector3.ClampMagnitude(target-walker.transform.position,dt*6));
            if (mantling && Vector3.Distance(walker.transform.position,target) < .06f)
            {
                walker.EndTraversal(); State = PassageState.Exploring; ladder = null;
            }
            else if (!mantling && input > 0 && climbHeight >= WaterTowerAccess.RimHeight+.17f && Vector3.Distance(walker.transform.position,target) < .08f)
                mantling = true;
            else if (input < 0 && climbHeight <= .06f)
            {
                walker.EndTraversal(); State = PassageState.Exploring; ladder = null;
            }
        }

        public bool BeginDive(WaterTowerAccess tower)
        {
            if (State != PassageState.Exploring || tower == null || !walker.BeginTraversal(true)) return false;
            ladder = tower; candidate = null; elapsed = 0; State = PassageState.Diving;
            entryPosition = walker.transform.position; entryPitch = walker.LookPitch; sourceYaw = walker.transform.eulerAngles.y;
            // The lower water is a second, real mesh surface. End the source-side
            // dive just beneath it, then emerge upward through the destination sea.
            deepPosition = entryPosition;
            deepPosition.y = tower.BottomSurfaceCenter.y-1.72f*tower.transform.lossyScale.y;
            Immersion = .85f; Effects.Enter();
            return true;
        }

        private void TickCrossing()
        {
            elapsed += Time.deltaTime;
            if (State == PassageState.Diving)
            {
                float t = Mathf.Clamp01(elapsed/1.2f), ease = t*t*(3-2*t);
                Immersion = Mathf.Lerp(.85f,1,Mathf.SmoothStep(0,1,t));
                // First half is an ordinary fall through water; over the second
                // half the body/camera turn over as gravity changes sides.
                float inversion = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,1,t));
                walker.SetTraversalPose(Vector3.Lerp(entryPosition,deepPosition,ease),
                    Mathf.Lerp(entryPitch,-entryPitch,inversion),180*inversion);
                // If loading is slower than the physical dive, hold under water,
                // never show a loading screen or teleport into missing colliders.
                if (t >= 1 && WaterWorld.Instance != null) TransferToWaterWorld();
            }
            else
            {
                float t = Mathf.Clamp01(elapsed/1.15f), ease = t*t*(3-2*t);
                Vector3 arrival = WaterWorld.Instance.Arrival;
                walker.SetTraversalPose(Vector3.Lerp(destinationDeep,arrival,ease),Mathf.Lerp(-entryPitch,-5,ease),180*(1-ease));
                float headDepth = WaterWorld.Instance.SurfaceY-walker.View.transform.position.y;
                Immersion = Mathf.SmoothStep(0,1,Mathf.Clamp01((headDepth+.08f)/1.4f));
                if (headDepth <= 0) Effects.Emerge();
                if (t >= 1)
                {
                    Immersion = 0; walker.EndTraversal(); State = PassageState.WaterWorld;
                }
            }
        }

        private void OnDisable()
        {
            if (Effects != null) Effects.Tick(0,true,0);
        }

        private void TransferToWaterWorld()
        {
            var destination = WaterWorld.Instance;
            transform.SetParent(null,true);
            SceneManager.MoveGameObjectToScene(gameObject,destination.gameObject.scene);
            sourceEnvironment.SetActive(false);
            SceneManager.SetActiveScene(destination.gameObject.scene);
            destination.ActivateEnvironment();
            var rural = walker.GetComponent<RuralAudio>();
            if (rural != null) { rural.Breeze.Stop(); rural.Footsteps.Stop(); rural.enabled = false; }
            destinationDeep = destination.transform.position+Vector3.down*4;
            walker.transform.rotation = Quaternion.Euler(0,sourceYaw,0);
            walker.SetTraversalPose(destinationDeep,-entryPitch,180);
            elapsed = 0; State = PassageState.Emerging;
        }

        private void OnGUI()
        {
            if (walker == null || walker.MenuOpen || (candidate == null && State != PassageState.Climbing)) return;
            if (prompt == null) prompt = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
            string text = State == PassageState.Climbing ? "W / S subir e descer • E soltar • Espaço saltar" : "E — usar escada";
            GUI.Box(new Rect((Screen.width-430)*.5f,Screen.height*.78f,430,36),text,prompt);
        }
    }
}
