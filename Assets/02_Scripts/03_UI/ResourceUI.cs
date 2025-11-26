using UnityEngine;
using TMPro;

public class ResourceUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resourceText;
    private ResourceManager rm;

    private void Start()
    {
        // ResourceManager 인스턴스 가져오기
        rm = ResourceManager.Instance;
        if (rm == null)
        {
            Debug.LogError("[ResourceUI] ResourceManager.Instance 찾을 수 없음!");
            return;
        }

        // 초기 UI 적용
        UpdateResourceText(rm.CurrentResource);

        // 이벤트 등록: 자원이 바뀌면 UI 자동 갱신
        rm.OnResourceChanged += UpdateResourceText;
    }

    private void OnDestroy()
    {
        if (rm != null)
            rm.OnResourceChanged -= UpdateResourceText;
    }

    private void UpdateResourceText(int value)
    {
        resourceText.text = $"자원: {value}";
    }
}
