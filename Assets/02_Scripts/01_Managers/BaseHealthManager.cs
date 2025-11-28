using TMPro;
using UnityEngine;

/// <summary>
/// 기지 HP를 관리하는 매니저
/// </summary>
public class BaseHealthManager : MonoBehaviour
{
    public static BaseHealthManager Instance { get; private set; }

    [Header("Base HP Settings")]
    [Tooltip("기지의 최대 HP")]
    public int maxHp = 20;

    [Tooltip("현재 HP (게임 시작 시 maxHp로 초기화)")]
    public int currentHp;

    [Header("UI")]
    [Tooltip("기지 HP를 표시할 텍스트 (선택 사항)")]
    [SerializeField] private TextMeshProUGUI hpText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        currentHp = maxHp;
        UpdateUI();
    }

    /// <summary>
    /// 기지가 피해를 받을 때 호출
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0)
            return;

        currentHp -= amount;
        if (currentHp < 0)
            currentHp = 0;

        Debug.Log($"[BaseHealth] 기지 피해: -{amount}, 남은 HP: {currentHp}");

        UpdateUI();

        if (currentHp <= 0)
        {
            OnBaseDestroyed();
        }
    }

    private void UpdateUI()
    {
        if (hpText != null)
        {
            hpText.text = $"Base HP: {currentHp}/{maxHp}";
        }
    }

    // 기지가 파괴되었을 때 처리 (지금은 로그만)
    private void OnBaseDestroyed()
    {
        Debug.Log("[BaseHealth] 기지가 파괴되었습니다. 게임 오버 연출 예정.");
        // 추후: GameManager와 연동해서 게임 오버 화면 띄우기
    }
}
