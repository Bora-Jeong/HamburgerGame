﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer hand;
    [SerializeField]
    private GameObject redLight;
    [SerializeField]
    private GameObject darkLight;
    [SerializeField]
    private Animator robotHead_animator;
    [SerializeField]
    private Hammer hammer;

    private Animator animator;
    Hamburger curHamburger;
    private Ingredient curIngredient;
    private SpriteRenderer spriteRenderer;

    public bool isWorking { get; private set; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void StartWork(float speed)
    {
        isWorking = true;
        curHamburger = GameManager.instance.GetAiRecipe();
        animator.SetBool("Working", true);
        animator.speed = speed;
    }

    public void StopWork()
    {
        isWorking = false;
        curHamburger = null;
        hand.sprite = null;
        animator.SetBool("Working", false);
    }

    public void Pause() // 깡!!
    {
        StartCoroutine(StartPause());
    }

    IEnumerator StartPause()
    {
        isWorking = false;
        float speed = animator.speed;

        robotHead_animator.SetBool("broken", true);
        hammer.Show();
        animator.speed = 0;
        redLight.SetActive(true);
        darkLight.SetActive(true);

        yield return new WaitForSeconds(6f);

        animator.speed = speed;
        redLight.SetActive(false);
        darkLight.SetActive(false);
        robotHead_animator.SetBool("broken", false);

        isWorking = true;
    }

    public void GrabIngredient() // 재료 집기
    {
        if (curHamburger.ingredients.Count == 0)
        {
            GameManager.instance.ServeHamburger_ai();
            curHamburger = GameManager.instance.GetAiRecipe();
        }

        curIngredient = curHamburger.ingredients.Dequeue();
        hand.sprite = GameManager.instance.GetIngredientSprite(curIngredient, true);
    }

    public void OutIngredient() // 재료 놓기
    {
        Vector3 dest = GameManager.instance.aiHamburger.GetDestination(curIngredient, out GameObject go);
        go.transform.position = hand.transform.position;
        hand.sprite = null;

        LeanTween.moveY(go, dest.y, 0.3f);
    }

    void Update()
    {
        //if (!isWorking) return;

        //time += Time.deltaTime;

        //if(curHamburger == null) curHamburger = GameManager.instance.GetAiRecipe();

        //if (time > GameManager.instance.aiSpeed)
        //{
        //    time = 0;
        //    if (curHamburger.ingredients.Count == 0)
        //    {
        //        GameManager.instance.ServeHamburger_ai();
        //        curHamburger = GameManager.instance.GetAiRecipe();
        //    }
        //    else
        //    {
        //        GameManager.instance.aiHamburger.StackIngredient(curHamburger.ingredients.Dequeue());
        //    }          
     
        //}
    }

}
