using easyar;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ImageTracking_CloudRecognition
{
    public class CloudRecognizer : MonoBehaviour
    {
        [SerializeField] ARSession Session;
        [SerializeField] bool UseOfflineCache = true;
        [SerializeField] string OfflineCachePath;
        [SerializeField] GameObject videoPlayer;
        [SerializeField] GameObject screen04;
        [SerializeField] GameObject homeButton;

        private CloudRecognizerFrameFilter cloudRecognizer;
        private ImageTrackerFrameFilter tracker;
        private List<GameObject> targetObjs = new List<GameObject>();
        private List<string> loadedCloudTargetUids = new List<string>();
        private int cachedTargetCount;
        private ResolveInfo resolveInfo;
        private float autoResolveRate = 1f;
        private bool isTracking;
        private bool resolveOn;

        private void Awake()
        {
            tracker = Session.GetComponentInChildren<ImageTrackerFrameFilter>();
            cloudRecognizer = Session.GetComponentInChildren<CloudRecognizerFrameFilter>();

            if (UseOfflineCache)
            {
                if (string.IsNullOrEmpty(OfflineCachePath))
                {
                    OfflineCachePath = Application.persistentDataPath + "/CloudRecognizerSample";
                }
                if (!Directory.Exists(OfflineCachePath))
                {
                    Directory.CreateDirectory(OfflineCachePath);
                }
                if (Directory.Exists(OfflineCachePath))
                {
                    var targetFiles = Directory.GetFiles(OfflineCachePath);
                    foreach (var file in targetFiles)
                    {
                        if (Path.GetExtension(file) == ".etd")
                        {
                            LoadOfflineTarget(file);
                        }
                    }
                }
            }
        }

        private void Start()
        {
            StartAutoResolve(autoResolveRate);
        }

        private void Update()
        {
            AutoResolve();
        }

        private void OnDestroy()
        {
            foreach (var obj in targetObjs)
            {
                Destroy(obj);
            }
        }

        public void ClearAll()
        {
            if (Directory.Exists(OfflineCachePath))
            {
                var targetFiles = Directory.GetFiles(OfflineCachePath);
                foreach (var file in targetFiles)
                {
                    if (Path.GetExtension(file) == ".etd")
                    {
                        File.Delete(file);
                    }
                }
            }
            foreach (var obj in targetObjs)
            {
                Destroy(obj);
            }
            targetObjs.Clear();
            loadedCloudTargetUids.Clear();
            cachedTargetCount = 0;
        }

        public void StartAutoResolve(float resolveRate)
        {
            if (Session != null && resolveInfo == null)
            {
                autoResolveRate = resolveRate;
                resolveInfo = new ResolveInfo();
                resolveOn = true;
            }
        }

        public void StopResolve()
        {
            if (Session != null)
            {
                resolveInfo = null;
                resolveOn = false;
            }
        }

        private void AutoResolve()
        {
            var time = Time.time;
            if (!resolveOn || isTracking || resolveInfo.Running || time - resolveInfo.ResolveTime < autoResolveRate)
            {
                return;
            }

            resolveInfo.Running = true;

            cloudRecognizer.Resolve((frame) =>
            {
                resolveInfo.ResolveTime = time;
            }, (result) =>
            {
                if (resolveInfo == null)
                {
                    return;
                }

                resolveInfo.Index++;
                resolveInfo.Running = false;
                resolveInfo.CostTime = Time.time - resolveInfo.ResolveTime;
                resolveInfo.CloudStatus = result.getStatus();
                resolveInfo.TargetName = "-";
                resolveInfo.UnknownErrorMessage = result.getUnknownErrorMessage();

                var target = result.getTarget();
                if (target.OnSome)
                {
                    using (var targetValue = target.Value)
                    {
                        resolveInfo.TargetName = targetValue.name();

                        if (!loadedCloudTargetUids.Contains(targetValue.uid()))
                        {
                            LoadCloudTarget(targetValue.Clone());
                        }
                    }
                }
            });
        }

        private void LoadCloudTarget(ImageTarget target)
        {
            var uid = target.uid();
            loadedCloudTargetUids.Add(uid);
            var go = new GameObject(uid);
            targetObjs.Add(go);
            var targetController = go.AddComponent<ImageTargetController>();
            targetController.SourceType = ImageTargetController.DataSource.Target;
            targetController.TargetSource = target;
            LoadTargetIntoTracker(targetController);

            targetController.TargetLoad += (loadedTarget, result) =>
            {
                if (!result)
                {
                    Debug.LogErrorFormat("target {0} load failed", uid);
                    return;
                }
                AddCubeOnTarget(targetController);
            };

            if (UseOfflineCache && Directory.Exists(OfflineCachePath))
            {
                if (target.save(OfflineCachePath + "/" + target.uid() + ".etd"))
                {
                    cachedTargetCount++;
                }
            }
        }

        private void LoadOfflineTarget(string file)
        {
            var go = new GameObject(Path.GetFileNameWithoutExtension(file) + "-offline");
            targetObjs.Add(go);
            var targetController = go.AddComponent<ImageTargetController>();
            targetController.SourceType = ImageTargetController.DataSource.TargetDataFile;
            targetController.TargetDataFileSource.PathType = PathType.Absolute;
            targetController.TargetDataFileSource.Path = file;
            LoadTargetIntoTracker(targetController);

            targetController.TargetLoad += (loadedTarget, result) =>
            {
                if (!result)
                {
                    Debug.LogErrorFormat("target data {0} load failed", file);
                    return;
                }
                loadedCloudTargetUids.Add(loadedTarget.uid());
                cachedTargetCount++;
                AddCubeOnTarget(targetController);
            };
        }

        private void LoadTargetIntoTracker(ImageTargetController controller)
        {
            controller.Tracker = tracker;
            controller.TargetFound += () =>
            {
                isTracking = true;
                ChangeTabs(true);
            };
            controller.TargetLost += () =>
            {
                isTracking = false;
                ChangeTabs(false);
            };
        }

        private void AddCubeOnTarget(ImageTargetController controller)
        {
            videoPlayer.transform.parent = controller.transform;
            videoPlayer.transform.localPosition = new Vector3(0, 0, -0.1f);
            videoPlayer.transform.eulerAngles = new Vector3(0, 0, 0);
            videoPlayer.transform.localScale = new Vector3(1f, 1f / controller.Target.aspectRatio(), 1f);
        }

        private class ResolveInfo
        {
            public int Index = 0;
            public bool Running = false;
            public float ResolveTime = 0;
            public float CostTime = 0;
            public string TargetName = "-";
            public Optional<string> UnknownErrorMessage;
            public CloudRecognizationStatus CloudStatus = CloudRecognizationStatus.UnknownError;
        }

        private void ChangeTabs(bool choice)
        {
            screen04.gameObject.SetActive(!choice);
            homeButton.gameObject.SetActive(choice);
        }
    }
}