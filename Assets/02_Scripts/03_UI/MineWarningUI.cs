using UnityEngine;

// 지뢰를 밟았을 때 잠깐 동안 경고 UI를 보여주는 스크립트
public class MineWarningUI : MonoBehaviour
{
    [Header("경고 패널 오브젝트")]
    [SerializeField] private GameObject warningPanel;  // MineWarningPanel을 연결

    [Header("표시 시간(초)")]
    [SerializeField] private float showDuration = 1.5f; // 몇 초 동안 표시할지

    private float timer = 0f;   // 남은 시간
    private bool isShowing = false; // 현재 표시 중인지 여부

    private void Awake()
    {
        // 시작할 때는 항상 꺼둠
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
    }

    // 외부에서 호출하는 함수: 경고를 한 번 보여주기
    public void ShowOnce()
    {
        if (warningPanel == null)
        {
            Debug.LogWarning("[MineWarningUI] warningPanel이 설정되지 않았습니다.");
            return;
        }

        warningPanel.SetActive(true); // 패널 켜기
        timer = showDuration;         // 남은 시간 초기화
        isShowing = true;             // 표시 중 상태로 전환
    }

    private void Update()
    {
        if (!isShowing)
            return;

        // 매 프레임마다 남은 시간 감소
        timer -= Time.deltaTime;

        // 시간이 다 되면 패널을 끄고 상태 초기화
        if (timer <= 0f)
        {
            warningPanel.SetActive(false);
            isShowing = false;
        }
    }
}
