using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;  // New Input System 사용

// 게임 모드 정의
public enum GameMode
{
    Explore,    // 탐색 모드 (지금까지 하던 기본 모드)
    Build       // 건설 모드 (2주차에 실제 기능 추가 예정)
}

public class GameManager : MonoBehaviour
{
    // 전역 어디서든 GameManager에 접근하기 위한 싱글톤 인스턴스
    public static GameManager Instance { get; private set; }

    [Header("Managers")]
    // 다른 매니저들을 참조하기 위한 변수 (Inspector에서 연결)
    public ResourceManager resourceManager;
    public TileManager tileManager; // 나중에 TileManager를 연결할 예정

    [Header("Game Mode")]
    [SerializeField] private GameMode currentMode = GameMode.Explore; // 현재 모드
    [SerializeField] private TextMeshProUGUI modeText;                // HUD에 표시할 모드 텍스트

    // 모드 변경을 다른 시스템에 알려주기 위한 이벤트 (나중에 TileInputController에서 구독 가능)
    public System.Action<GameMode> OnModeChanged;

    public GameMode CurrentMode => currentMode;  // 읽기 전용 프로퍼티

    private void Awake()
    {
        // 싱글톤 초기화: 이미 다른 GameManager가 있으면 자신을 파괴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 씬이 바뀌어도 이 오브젝트는 유지되도록 설정
        DontDestroyOnLoad(gameObject);

        // 필수 매니저들이 연결되어 있는지 간단히 체크
        if (resourceManager == null)
        {
            Debug.LogWarning("[GameManager] ResourceManager가 연결되어 있지 않습니다.");
        }

        if (tileManager == null)
        {
            Debug.LogWarning("[GameManager] TileManager가 연결되어 있지 않습니다.");
        }

        // 시작 시 현재 모드 UI 갱신
        UpdateModeText();
    }

    private void Update()
    {
        // New Input System이 활성화되지 않은 환경 보호
        if (Keyboard.current == null)
            return;

        // Space 키로 모드 토글 (탐색 ↔ 건설)
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ToggleMode();
        }
    }

    // 모드 토글 함수 (탐색 ↔ 건설)
    public void ToggleMode()
    {
        if (currentMode == GameMode.Explore)
        {
            currentMode = GameMode.Build;
        }
        else
        {
            currentMode = GameMode.Explore;
        }

        Debug.Log($"[GameManager] 모드 변경: {currentMode}");

        // HUD 텍스트 갱신
        UpdateModeText();

        // 모드 변경 이벤트 알림
        OnModeChanged?.Invoke(currentMode);
    }

    // HUD에 현재 모드 텍스트 표시
    private void UpdateModeText()
    {
        if (modeText == null)
            return;

        switch (currentMode)
        {
            case GameMode.Explore:
                modeText.text = "모드: 탐색";
                break;

            case GameMode.Build:
                modeText.text = "모드: 건설";
                break;
        }
    }

    // 필요하면 외부에서 강제로 모드를 설정하는 함수도 제공 가능
    public void SetMode(GameMode mode)
    {
        if (currentMode == mode)
            return;

        currentMode = mode;
        UpdateModeText();
        OnModeChanged?.Invoke(currentMode);
    }
}
