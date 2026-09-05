using System;
using System.IO;
using UnityEngine;

namespace YWJ.Player.Progress
{
    public class PlayerProgressManager : MonoBehaviour
    {
        public PlayerProgressData ProgressData { get; private set; }

        public PlayerProgress CurrentProgress =>
            ProgressData.CurrentProgress;

        public event Action<PlayerProgress> PlayerProgressChanged;

        private const string SaveFileName =
            "PlayerProgress.json";

        private string SaveFilePath =>
            Path.Combine(
                Application.persistentDataPath,
                SaveFileName);

        private void Awake()
        {
            LoadProgress();
        }

        public bool IsCurrentProgress(
            PlayerProgress progress)
        {
            return CurrentProgress == progress;
        }

        public void SetProgress(
            PlayerProgress progress)
        {
            if (CurrentProgress == progress)
            {
                return;
            }

            ProgressData.CurrentProgress = progress;

            SaveProgress();

            PlayerProgressChanged?.Invoke(progress);

            Debug.Log(
                $"[PlayerProgressManager] 진행도 변경: {progress}",
                this);
        }

        public void ResetProgress()
        {
            ProgressData = CreateDefaultProgress();

            SaveProgress();

            PlayerProgressChanged?.Invoke(
                ProgressData.CurrentProgress);

            Debug.Log(
                "[PlayerProgressManager] 진행도 초기화",
                this);
        }

        private void LoadProgress()
        {
            if (!File.Exists(SaveFilePath))
            {
                ProgressData = CreateDefaultProgress();

                SaveProgress();

                Debug.Log(
                    $"[PlayerProgressManager] 새 저장 파일 생성: " +
                    $"{SaveFilePath}",
                    this);

                return;
            }

            try
            {
                string json =
                    File.ReadAllText(SaveFilePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogWarning(
                        "[PlayerProgressManager] 저장 파일이 비어 있습니다.",
                        this);

                    ProgressData =
                        CreateDefaultProgress();

                    SaveProgress();
                    return;
                }

                ProgressData =
                    JsonUtility.FromJson<PlayerProgressData>(
                        json);

                if (ProgressData == null)
                {
                    Debug.LogWarning(
                        "[PlayerProgressManager] " +
                        "저장 데이터를 읽지 못했습니다.",
                        this);

                    ProgressData =
                        CreateDefaultProgress();

                    SaveProgress();
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[PlayerProgressManager] 진행도 로드 실패: " +
                    $"{exception.Message}",
                    this);

                ProgressData =
                    CreateDefaultProgress();
            }
        }

        private void SaveProgress()
        {
            try
            {
                string directoryPath =
                    Path.GetDirectoryName(SaveFilePath);

                if (!string.IsNullOrWhiteSpace(
                        directoryPath) &&
                    !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(
                        directoryPath);
                }

                string json =
                    JsonUtility.ToJson(
                        ProgressData,
                        true);

                File.WriteAllText(
                    SaveFilePath,
                    json);

                Debug.Log(
                    $"[PlayerProgressManager] 저장 완료: " +
                    $"{SaveFilePath}",
                    this);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[PlayerProgressManager] 진행도 저장 실패: " +
                    $"{exception.Message}",
                    this);
            }
        }

        private static PlayerProgressData
            CreateDefaultProgress()
        {
            return new PlayerProgressData
            {
                DataVersion = 1,
                CurrentProgress =
                    PlayerProgress.OpeningCutScene
            };
        }

        public void DeleteSaveFile()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                }

                ProgressData =
                    CreateDefaultProgress();

                PlayerProgressChanged?.Invoke(
                    ProgressData.CurrentProgress);

                Debug.Log(
                    "[PlayerProgressManager] 저장 파일 삭제",
                    this);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[PlayerProgressManager] 저장 파일 삭제 실패: " +
                    $"{exception.Message}",
                    this);
            }
        }

        [ContextMenu("Open Save Folder")]
        private void OpenSaveFolder()
        {
            Application.OpenURL(
                Application.persistentDataPath);
        }

#if UNITY_EDITOR
        [ContextMenu("Reset Player Progress")]
        private void ResetPlayerProgressInEditor()
        {
            ResetProgress();
        }

        [ContextMenu("Delete Player Progress Save")]
        private void DeletePlayerProgressSaveInEditor()
        {
            DeleteSaveFile();
        }

        [ContextMenu("Print Save Path")]
        private void PrintSavePath()
        {
            Debug.Log(
                $"[PlayerProgressManager] 저장 경로: " +
                $"{SaveFilePath}",
                this);
        }
#endif
    }
}