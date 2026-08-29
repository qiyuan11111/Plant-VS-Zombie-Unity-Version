using PvZ.Config;
using PvZ.Core;
using UnityEngine;

namespace PvZ.Bootstrap
{
    /// <summary>
    /// Loads and initializes the scene's shared asset catalog.
    /// </summary>
    public sealed class GameBootstrap : SceneSingleton<GameBootstrap>
    {
        public GameConfigObject Catalog { get; private set; }

        protected override void OnSingletonAwake()
        {
            Catalog = Resources.Load<GameConfigObject>(nameof(GameConfigObject));
        }

        protected override bool ValidateReferences()
        {
            return RequireReference(Catalog, $"Resources/{nameof(GameConfigObject)}");
        }

        protected override void OnReferencesValidated()
        {
            Catalog.Init();
        }
    }
}
