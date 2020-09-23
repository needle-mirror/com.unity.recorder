using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UnityEngine.Recorder.Tests
{
    class BaseFixture
    {
        protected readonly List<string> deleteFileList = new List<string>();
        protected GameObject camera;
        const string sceneName = "Scene";

        protected static bool NearlyEqual(float f1, float f2, float eps = 1e-5f)
        {
            return Mathf.Abs(f1 - f2) < eps;
        }

        protected static bool NearlyEqual(Vector3 v1, Vector3 v2, float eps = Vector3.kEpsilon)
        {
            return NearlyEqual(v1.x, v2.x, eps) &&
                NearlyEqual(v1.y, v2.y, eps) &&
                NearlyEqual(v1.z, v2.z, eps);
        }

        protected static bool NearlyEqual(Quaternion v1, Quaternion v2, float eps = Quaternion.kEpsilon)
        {
            return NearlyEqual(v1.x, v2.x, eps) &&
                NearlyEqual(v1.y, v2.y, eps) &&
                NearlyEqual(v1.z, v2.z, eps) &&
                NearlyEqual(v1.w, v2.w, eps) ||
                NearlyEqual(v1.x, -v2.x, eps) &&
                NearlyEqual(v1.y, -v2.y, eps) &&
                NearlyEqual(v1.z, -v2.z, eps) &&
                NearlyEqual(v1.w, -v2.w, eps);
        }

        [SetUp]
        public void SetUp()
        {
            var scene = SceneManager.CreateScene(sceneName);
            SceneManager.SetActiveScene(scene);

            camera = new GameObject("Cam");
            camera.AddComponent<Camera>();
            camera.transform.localPosition = new Vector3(0, 1, -10);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var asyncOperation = SceneManager.UnloadSceneAsync(sceneName);
            while (!asyncOperation.isDone)
            {
                yield return null;
            }

            foreach (var file in deleteFileList)
            {
                Assert.IsTrue(File.Exists(file));
                File.Delete(file);
                Assert.IsFalse(File.Exists(file));
            }

            deleteFileList.Clear();
        }
    }
}
