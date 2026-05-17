using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EditorWindowMovement : MonoBehaviour
{
    public GameObject outputArea;
    public GameObject inputArea;
    public GameObject openButton;
    public LineRenderer LineRenderer;

    private void Start()
    {
        inputArea.SetActive(false);
        outputArea.SetActive(false);
        LineRenderer.enabled = false;

    }

    public void OpenEditor()
    {
        inputArea.SetActive(true);
        outputArea.SetActive(true);
        LineRenderer.enabled = true;

        inputArea.transform.DOMove(new Vector3(302 / 48, 0, 0), 0.1f).SetEase(Ease.OutQuart);
        outputArea.transform.DOMove(new Vector3(-253 / 48, 0, 0), 0.1f).SetEase(Ease.OutQuart);
        openButton.SetActive(false);
        //  StartCoroutine(OpenEditorWindow());
    }

    public void CloseEditor()
    {
        LineRenderer.enabled = false;
        StartCoroutine(CloseEditorWindow());
    }

    IEnumerator OpenEditorWindow()
    {
        inputArea.transform.DOMove(new Vector3(302/ 48, 0, 0), 0.1f).SetEase(Ease.OutQuart);
        outputArea.transform.DOMove(new Vector3(-253 / 48, 0, 0), 0.1f).SetEase(Ease.OutQuart);
        openButton.SetActive(false);
        return null;
    }

    IEnumerator CloseEditorWindow()
    {
        inputArea.transform.DOMove(new Vector3(607/48, 0, 0), 0.1f).SetEase(Ease.OutQuart);
        outputArea.transform.DOMove(new Vector3(-591/48, 0, 0), 0.1f).SetEase(Ease.OutQuart);
        yield return new WaitForSeconds(0.1f);
        openButton.SetActive(true);
        inputArea.SetActive(false);
        outputArea.SetActive(false);
    }
}
