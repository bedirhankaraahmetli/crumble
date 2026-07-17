using UnityEngine;

namespace Crumble.Core
{
    public enum GameState
    {
        Booting,
        Playing,
    }

    /// <summary>
    /// Entry point. Lives on the persistent _Bootstrap object together with the other
    /// managers and drives boot order: load the save, then hand off to gameplay.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : Singleton<GameManager>
    {
        public GameState State { get; private set; } = GameState.Booting;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return; // duplicate bootstrap being destroyed
            }

            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            SaveManager.Instance.LoadOrCreate();
            State = GameState.Playing;
        }
    }
}
