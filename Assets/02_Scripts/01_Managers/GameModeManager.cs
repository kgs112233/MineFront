using System;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;      // New Input System (Keyboard)



public class GameModeManager : MonoBehaviour
{
    // 다른 스크립트에서 쉽게 접근하기 위한 싱글톤
    public static GameModeManager Instance { get; private set; }

    // 현재 모드 (초기값: 탐색)
    public GameMode CurrentMode { get; private set; } = GameMode.Explore;

    // 모드가 바뀔 때 알림을 주기 위한 이벤트 (나중에 TileInputController에서 구독 가능)
    public event Action<GameMode> OnModeChanged;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI modeText;   // HUD에 표시할 "현재 모드" 텍스트

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameModeManager] 중복 인스턴스가 감지되어 기존 인스턴스를 사용합니다.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 시작 시 UI 갱신
        UpdateModeText();
    }

    private void Update()
    {
        // New Input System과 함께 Keyboard.current 사용 (이미 프로젝트에서 Mouse.current 사용 중)
        if (UnityEngine.InputSystem.Keyboard.current == null)
            return;

        // Space 키로 모드 토글
        if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ToggleMode();
        }
    }

    // 모드 토글 함수 (탐색 ↔ 건설)
    public void ToggleMode()
    {
        if (CurrentMode == GameMode.Explore)
        {
            CurrentMode = GameMode.Build;
        }
        else
        {
            CurrentMode = GameMode.Explore;
        }

        Debug.Log($"[GameModeManager] 모드 변경: {CurrentMode}");

        // UI 갱신
        UpdateModeText();

        // 구독자에게 알림 (나중에 필요하면 사용)
        OnModeChanged?.Invoke(CurrentMode);
    }

    // HUD 텍스트 갱신
    private void UpdateModeText()
    {
        if (modeText == null)
            return;

        switch (CurrentMode)
        {
            case GameMode.Explore:
                modeText.text = "모드: 탐색";
                break;
            case GameMode.Build:
                modeText.text = "모드: 건설";
                break;
        }
    }
}
