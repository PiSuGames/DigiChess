using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls champion animations
/// </summary>
public class ChampionAnimation : MonoBehaviour
{

    public GameObject characterModel;
    public Animator animator;
    public ChampionController championController;
    private bool levelup;
    private bool levelup2;


    private Vector3 lastFramePosition;
    /// Start is called before the first frame update
    void Start()
    {
        //get character model
        characterModel = this.transform.Find("character").gameObject;
       
        //get animator
        animator = characterModel.GetComponent<Animator>();
        championController = this.transform.GetComponent<ChampionController>();

        levelup = false;
        levelup2 = false;
    }

    /// Update is called once per frame
    void Update()
    {

        //Check LVL
        if(championController.lvl == 2 && levelup == false)
        {
            characterModel = this.transform.Find("evo1").gameObject;
            animator = characterModel.GetComponent<Animator>();

            levelup = true;
        }

        if (championController.lvl == 3 && levelup2 == false)
        {
            characterModel = this.transform.Find("evo2").gameObject;
            animator = characterModel.GetComponent<Animator>();

            levelup2 = true;
        }

        //calculate speed
        float movementSpeed = (this.transform.position - lastFramePosition).magnitude / Time.deltaTime;

        //set movement speed on animator controller
        animator.SetFloat("movementSpeed", movementSpeed);

        //store last frame position
        lastFramePosition = this.transform.position;
    }

    /// <summary>
    /// tells animation to attack or stop attacking
    /// </summary>
    /// <param name="b"></param>
    public void DoAttack(bool b)
    {
        animator.SetBool("isAttacking", b);

    }

    /// <summary>
    /// Called when attack animation finished
    /// </summary>
    public void OnAttackAnimationFinished()
    {
        animator.SetBool("isAttacking", false);

        championController.OnAttackAnimationFinished();

        //Debug.Log("OnAttackAnimationFinished");

    }

    /// <summary>
    /// sets animation state
    /// </summary>
    /// <returns></returns>
    public void IsAnimated(bool b)
    {
        animator.enabled = b;
    }
}
