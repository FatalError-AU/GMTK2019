using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FX
{
    public class SceneTransition : MonoBehaviour
    {
        private static SceneTransition instance;

        public static void LoadScene(string scene)
            => LoadScene(SceneUtility.GetBuildIndexByScenePath(scene));

        public static void LoadScene(int sceneId)
            => instance.StartCoroutine(instance._LoadScene(sceneId));

        private IEnumerator _LoadScene(int sceneId)
        {
            yield break;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            
        }
    }
}