using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerAnim : MonoBehaviour
{
    [SerializeField] Animator playerAnim;
    [SerializeField] Animator slashAnim;
    [SerializeField] private GameObject slash;

    void Update()
    {
        
    }

    public void walk()
    {
        playerAnim.SetTrigger("StartWalk");
    }

    public void Slash()
    {
        slashAnim.SetTrigger("StartSlash");
    }
}
