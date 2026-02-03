#if UNITY_EDITOR
using UnityEngine;

namespace ArcadeRacer.Audio
{
    /// <summary>
    /// Génère des AudioClips placeholder pour tester le système audio. 
    /// À SUPPRIMER quand tu as de vrais sons !
    /// </summary>
    public class AudioPlaceholder : MonoBehaviour
    {
        [ContextMenu("Generate Placeholder Clips")]
        void GeneratePlaceholders()
        {
            Debug.LogWarning("[AudioPlaceholder] Cette fonction ne génère pas de vrais clips.  Télécharge des sons gratuits sur Freesound.org !");
        }
    }
}
#endif