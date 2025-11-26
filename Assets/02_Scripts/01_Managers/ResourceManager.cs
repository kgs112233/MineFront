using System;
using UnityEngine;

// 게임 전체 자원을 관리하는 매니저
public class ResourceManager : MonoBehaviour
{
    // 어디서든 접근하기 위한 싱글톤 인스턴스
    public static ResourceManager Instance { get; private set; }

    [Header("Resource Settings")]
    [SerializeField]
    private int startResource = 20;    // 시작 자원 (인스펙터에서 조정 가능)

    // 현재 자원 수치를 저장하는 변수 (외부에서 직접 변경 금지)
    [SerializeField]
    private int currentResource;       // 실제 런타임 자원 값

    // 자원 값이 변경될 때 UI 등에 알려주기 위한 이벤트
    public event Action<int> OnResourceChanged;

    // 외부에서 현재 자원 값을 읽을 때 사용하는 프로퍼티 (읽기 전용)
    public int CurrentResource => currentResource;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ResourceManager] 중복 인스턴스가 감지되어 기존 객체를 유지합니다.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // 씬 전환 시에도 유지하고 싶으면 아래 한 줄 유지, 아니면 제거해도 됨
        DontDestroyOnLoad(gameObject);

        // 시작 자원으로 초기화
        currentResource = startResource;

        // 게임 시작 시 현재 자원 값을 한 번 알려줘서
        // UI가 초기 상태를 맞출 수 있도록 함
        NotifyResourceChanged();
    }

    // 자원을 증감시키는 함수 (양수/음수 모두 가능)
    public void AddResource(int amount)
    {
        // 자원 값 변경
        currentResource += amount;

        // 자원이 0 미만으로 내려가지 않도록 보호
        if (currentResource < 0)
        {
            currentResource = 0;
        }

        // 변경된 자원 값을 구독자들에게 알림 (UI 등)
        NotifyResourceChanged();
    }

    // 자원을 소비하려 할 때 사용하는 함수
    // 자원이 부족하면 false, 성공하면 true 반환
    public bool TrySpendResource(int amount)
    {
        if (currentResource < amount)
            return false;

        currentResource -= amount;
        NotifyResourceChanged();
        return true;
    }

    // 자원 변경 이벤트를 호출하는 내부 전용 함수
    private void NotifyResourceChanged()
    {
        OnResourceChanged?.Invoke(currentResource);
    }
}
