using UnityEngine;
using TMPro;

// 한 칸 위에 표시되는 숫자 UI를 관리하는 스크립트
public class TileNumberView : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;  // 숫자를 표시할 TMP

    // 외부에서 숫자를 설정할 때 호출하는 함수
    public void SetNumber(int count)
    {
        // 0은 숫자를 표시하지 않음
        if (count <= 0)
        {
            text.text = "";
            return;
        }

        text.text = count.ToString();

        // 숫자에 따라 색상 다르게 설정 (원하는 대로 조정 가능)
        switch (count)
        {
            case 1:
                text.color = Color.blue;
                break;
            case 2:
                text.color = Color.green;
                break;
            case 3:
                text.color = Color.red;
                break;
            case 4:
                text.color = new Color(0.5f, 0f, 0.5f); // 보라색
                break;
            default:
                text.color = Color.black;
                break;
        }
    }
}
