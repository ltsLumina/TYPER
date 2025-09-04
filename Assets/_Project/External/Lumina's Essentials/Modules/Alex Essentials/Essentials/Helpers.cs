#region
using System;
using UnityEngine;
using static UnityEngine.Object;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
#endregion

namespace Lumina.Essentials.Modules
{
    /// <summary>
    ///     General helper methods that don't fit into any other category.
    /// </summary>
    public static class Helpers
    {
        #region Camera
        static Camera cameraMain;

        /// <summary>
        ///     Allows you to call camera.main without it being an expensive call, as we cache it here after the first call.
        ///     <example>Helpers.Camera.transform.position.etc </example>
        /// </summary>
        public static Camera CameraMain
        {
            get
            {
                if (cameraMain == null) cameraMain = Camera.main;
                return cameraMain;
            }
        }
        #endregion

        #region Audio
        /// <summary>
        ///     Plays the given audio clip on the given audio source with a random pitch between the given min and max pitch.
        /// </summary>
        /// <param name="audioClip"></param>
        /// <param name="audioSource"></param>
        /// <param name="minPitch"></param>
        /// <param name="maxPitch"></param>
        public static void PlayRandomPitch(this AudioClip audioClip, AudioSource audioSource, float minPitch, float maxPitch)
        {
            float randomPitch = Random.Range(minPitch, maxPitch);
            audioSource.pitch = randomPitch;
            audioSource.PlayOneShot(audioClip);
        }
        // End of Audio //
        #endregion

        #region Miscellaneous
        /// <summary>
        /// Returns the last child in a tree of transforms.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static Transform GetLastChild(this Transform parent)
        {
            if (parent.childCount == 0) return parent;

            Transform lastChild = parent.GetChild(parent.childCount - 1);
            while (lastChild.childCount > 0) lastChild = lastChild.GetChild(lastChild.childCount - 1);
            return lastChild;
        }
        
        /// <summary>
        ///     Destroys all children of the given transform.
        ///     Can be used as extension method.
        /// </summary>
        /// <param name="parent"></param>
        public static void DestroyAllChildren(this Transform parent)
        {
            foreach (Transform child in parent) { Destroy(child.gameObject); }
        }

        /// <summary>
        ///     Overload of the RandomVector method that takes a Vector2 instead of a Vector3.
        /// </summary>
        /// <param name="Vector2"> The Vector2 to be used as the base for the random Vector2.</param>
        /// <param name="min"> The minimum value for the random Vector2.</param>
        /// <param name="max"> The maximum value for the random Vector2.</param>
        /// <returns>Returns a random Vector2 between the given min and max values.</returns>
        public static Vector2 RandomVector(this Vector2 Vector2, float min, float max) => new (Random.Range(min, max), Random.Range(min, max));

        /// <summary>
        ///     Overload of the RandomVector method that takes a Vector3 instead of a Vector2.
        /// </summary>
        /// <param name="Vector3"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>Returns a random Vector2 between the given min and max values.</returns>
        public static Vector3 RandomVector
            (this Vector3 Vector3, float min, float max) => new (Random.Range(min, max), Random.Range(min, max), Random.Range(min, max));

        /// <summary>
        ///     Because I'm used to typing FindObjectOfType() instead of FindAnyObjectByType.
        /// </summary>
        /// <returns></returns>
        public static T FindObjectOfType<T>()
            where T : Object => FindAnyObjectByType<T>();

        /// <summary>
        ///     Even lazier version of FindObjectOfType.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Find<T>()
            where T : Object => FindAnyObjectByType<T>();

        public static Object Find(Type type)
        {
            Object[] objectsOfType = FindObjectsByType(type, FindObjectsSortMode.None);
            return objectsOfType.Length != 0 ? objectsOfType[0] : (Object) null;
        }

        public static T[] FindMultiple<T>(FindObjectsSortMode sortMode = FindObjectsSortMode.None) where T : Object => FindObjectsByType<T>(sortMode);
        
        public static Object[] FindMultiple(Type type, FindObjectsSortMode sortMode = FindObjectsSortMode.None) => FindObjectsByType(type, sortMode);
        
        public static T GetParentComponent<T>(this Component component) where T : Component => component.GetComponentInParent<T>();
        public static Component GetParentComponent(this Component component, Type type) => component.GetComponentInParent(type);
        
        public static Component GetChildComponent<T>(this Component component) where T : Component => component.GetComponentInChildren<T>();
        public static Component GetChildComponent(this Component component, Type type) => component.GetComponentInChildren(type);
        #endregion
    }
}
