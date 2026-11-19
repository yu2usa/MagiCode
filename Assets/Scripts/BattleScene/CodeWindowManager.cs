using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class CodeWindowManager : MonoBehaviour
{
    [SerializeField] private float animDuration = 0.3f;
    [SerializeField] private Ease animEase = Ease.OutQuart;
    [SerializeField] public GameObject expandButton;
    [SerializeField] private GameObject collapseButton;
    // 折り畳み時のY座標。ウィンドウは画面右下に固定しているので、画面下端からの距離になる
    [SerializeField] private float collapsedY = 247f;

    private RectTransform _rect;
    // Start時のY座標 = 展開状態の基準値
    private float _expandedY;

    void Start()
    {
        _rect = GetComponent<RectTransform>();
        _expandedY = _rect.anchoredPosition.y;
    }

    // Codewindowを展開状態（元のY座標）に戻す
    public void expandWindow()
    {
        _rect.DOAnchorPosY(_expandedY, animDuration).SetEase(animEase);
        collapseButton.SetActive(true);
        expandButton.SetActive(false);
    }

    // Codewindowを折り畳み位置（画面下端基準のY座標）まで下げる
    public void collapseWindow()
    {
        _rect.DOAnchorPosY(collapsedY, animDuration).SetEase(animEase);
        collapseButton.SetActive(false);
        expandButton.SetActive(true);
    }
}
