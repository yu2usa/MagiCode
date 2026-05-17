using UnityEngine;

public class ResetDataButton : MonoBehaviour
{
    // ボタンのOnClickに登録するメソッド
    public void OnResetButtonClicked()
    {
        ClearDataManager.ClearAllData();
    }
}