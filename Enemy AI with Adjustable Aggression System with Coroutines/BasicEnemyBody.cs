using UnityEngine;

public class BasicEnemyBody : MonoBehaviour
{
    GameObject _swordObj;
    GameObject _parentObj;
    BasicEnemyScript _parentScript;

    void Start()
    {
        _swordObj = transform.Find("EnemyWeapon").gameObject;
        _parentObj = transform.parent.gameObject;
        _parentScript = _parentObj.GetComponent<BasicEnemyScript>();
    }
    
    private void ColliderOn()
    {
        _swordObj.GetComponent<Collider>().enabled = true;
    }

    private void ColliderOff()
    {
        _swordObj.GetComponent<Collider>().enabled = false;
    }

    private void AttackEnd()
    {
        _parentScript.AttackEnd();
    }

    private void StaggerEnd()
    {
        _parentScript.StaggerEnd();
    }

    private void GuardDownEnd()
    {
        _parentScript.GuardDownHandler();
    }

    private void TurnSpeedZero()
    {
        _parentScript.TurnSpeedHandler(0);
    }

    private void TurnSpeedSlow()
    {
        _parentScript.TurnSpeedHandler(1);
    }

    private void TurnSpeedFull()
    {
        _parentScript.TurnSpeedHandler(2);
    }

    private void AttackStepZero()
    {
        _parentScript.AttackStepSpeedHandler(0);
    }
    
    private void AttackStepSlow()
    {
        _parentScript.AttackStepSpeedHandler(1);
    }

    private void AttackStepFull()
    {
        _parentScript.AttackStepSpeedHandler(2);
    }
}
