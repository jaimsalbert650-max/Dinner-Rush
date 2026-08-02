using UnityEngine;

namespace DinnerRush
{
    /// <summary>
    /// The single per-frame driver for every enemy in the scene.
    ///
    /// Each enemy used to carry four components with their own `Update`/`LateUpdate`
    /// (<see cref="EnemyAnimator"/>, <see cref="HitFlash"/>, <see cref="EnemyContactDamage"/>,
    /// <see cref="EnemyHealthBar"/>). Unity dispatches those one at a time, crossing from native
    /// code into managed script for each — at the 150-enemy cap that is ~600 dispatches every
    /// frame, and two of them (HitFlash, the health bar) did nothing but early-out almost always.
    ///
    /// Enemies are already tracked in a static registry for the separation pass, so one Update
    /// here walks it and calls the same work directly. Same behaviour, same look — the per-enemy
    /// cost is now a C# call instead of an engine message.
    ///
    /// Installs itself, so nothing has to be wired in the scene and it works in any scene the
    /// player reaches.
    /// </summary>
    public sealed class EnemyTicker : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("EnemyTicker");
            go.AddComponent<EnemyTicker>();
            DontDestroyOnLoad(go);   // survives Lobby -> Game; AfterSceneLoad only fires for the first scene
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            EnemyMovement.TickAll(dt);
            EnemyProjectile.TickAll(dt);
        }

        private void FixedUpdate() => EnemyMovement.FixedTickAll();

        private void LateUpdate() => EnemyHealthBar.TickShown();
    }
}
