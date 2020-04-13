using Pathfinding;
using RAIN.Action;
using RAIN.Core;
using UnityEngine;

public class AIMoveOnPath : AIBase
{
    private const float BLOCK_TIME = 1f;

    private const float WAYPOINT_DIST = 0.3f;

    private Path path;

    private int waypointIdx;

    private Vector3 previousNodePos;

    private Vector3 nextNodePos;

    private Vector3 lastValidPosition;

    private Vector2 flatPrevPos;

    private Vector2 flatNextPos;

    private Vector2 flatCurPos;

    private Animator animator;

    private Vector3 lastPosition;

    private float blockedTimer;

    private int tempStratPoints;

    private bool moveEnding;

    public override void Start(AI ai)
    {
        base.Start(ai);
        base.actionName = "MoveOnPath";
        path = unitCtrlr.AICtrlr.currentPath;
        path.Claim(unitCtrlr.gameObject);
        animator = unitCtrlr.animator;
        waypointIdx = 0;
        previousNodePos = unitCtrlr.transform.position;
        nextNodePos = path.vectorPath[waypointIdx];
        moveEnding = false;
        float num = unitCtrlr.unit.Movement * unitCtrlr.unit.Movement;
        unitCtrlr.SetFixed(fix: false);
        if (!unitCtrlr.Engaged)
        {
            unitCtrlr.ClampToNavMesh();
        }
        unitCtrlr.GetComponent<Rigidbody>().isKinematic = false;
        unitCtrlr.startPosition = unitCtrlr.transform.position;
        lastValidPosition = unitCtrlr.transform.position;
        lastPosition = unitCtrlr.transform.position;
        blockedTimer = 0f;
        success = (unitCtrlr.unit.CurrentStrategyPoints > 0);
        tempStratPoints = 0;
    }

    public override ActionResult Execute(AI ai)
    {
        if (!moveEnding)
        {
            if (!success)
            {
                unitCtrlr.AICtrlr.failedMove++;
                return (ActionResult)2;
            }
            PandoraSingleton<MissionManager>.Instance.RefreshFoWTargetMoving(unitCtrlr);
            unitCtrlr.UpdateActionStatus(notice: false);
            unitCtrlr.AICtrlr.UpdateVisibility();
            unitCtrlr.CheckEngaged(applyEnchants: true);
            if (unitCtrlr.Engaged)
            {
                EndMove();
                return (ActionResult)0;
            }
            if (unitCtrlr.AICtrlr.movingToSearchPoint)
            {
                for (int i = 0; i < unitCtrlr.interactivePoints.Count; i++)
                {
                    if (unitCtrlr.interactivePoints[i] is SearchPoint && (SearchPoint)unitCtrlr.interactivePoints[i] == unitCtrlr.AICtrlr.targetSearchPoint)
                    {
                        EndMove();
                        return (ActionResult)0;
                    }
                }
            }
            if (unitCtrlr.AICtrlr.targetDestructible != null)
            {
                float num = Vector3.SqrMagnitude(unitCtrlr.transform.position - unitCtrlr.AICtrlr.targetDestructible.transform.position);
                if (num <= 2.25f)
                {
                    EndMove();
                    return (ActionResult)0;
                }
                SkillId skillId = (!unitCtrlr.HasClose()) ? SkillId.BASE_SHOOT : SkillId.BASE_ATTACK;
                ActionStatus action = unitCtrlr.GetAction(skillId);
                for (int j = 0; j < action.Destructibles.Count; j++)
                {
                    if (action.Destructibles[j] == unitCtrlr.AICtrlr.targetDestructible)
                    {
                        EndMove();
                        return (ActionResult)0;
                    }
                }
            }
            float num2 = unitCtrlr.unit.Movement * unitCtrlr.unit.Movement;
            float num3 = PandoraUtils.FlatSqrDistance(unitCtrlr.startPosition, unitCtrlr.transform.position);
            if (num3 > num2)
            {
                if (unitCtrlr.unit.tempStrategyPoints >= unitCtrlr.unit.CurrentStrategyPoints)
                {
                    unitCtrlr.transform.position = lastValidPosition;
                    EndMove();
                    return (ActionResult)0;
                }
                tempStratPoints++;
                unitCtrlr.startPosition = unitCtrlr.transform.position;
                num3 = PandoraUtils.FlatSqrDistance(unitCtrlr.startPosition, unitCtrlr.transform.position);
            }
            unitCtrlr.unit.tempStrategyPoints = tempStratPoints + ((num3 > 1f) ? 1 : 0);
            if (Vector3.SqrMagnitude(unitCtrlr.transform.position - lastPosition) < 0.0064f)
            {
                blockedTimer += Time.deltaTime;
                if (blockedTimer >= 1f)
                {
                    unitCtrlr.AICtrlr.failedMove++;
                    EndMove();
                    PandoraDebug.LogWarning("AI movement end because it was blocked", "AI");
                    return (ActionResult)0;
                }
            }
            else
            {
                blockedTimer = 0f;
            }
            lastValidPosition = unitCtrlr.transform.position;
            lastPosition = unitCtrlr.transform.position;
            flatPrevPos.x = previousNodePos.x;
            flatPrevPos.y = previousNodePos.z;
            flatNextPos.x = nextNodePos.x;
            flatNextPos.y = nextNodePos.z;
            ref Vector2 reference = ref flatCurPos;
            Vector3 position = unitCtrlr.transform.position;
            reference.x = position.x;
            ref Vector2 reference2 = ref flatCurPos;
            Vector3 position2 = unitCtrlr.transform.position;
            reference2.y = position2.z;
            if ((unitCtrlr.interactivePoints.Count == 0 && Vector2.SqrMagnitude(flatPrevPos - flatCurPos) > Vector2.SqrMagnitude(flatPrevPos - flatNextPos)) || (unitCtrlr.interactivePoints.Count != 0 && Vector2.SqrMagnitude(flatNextPos - flatCurPos) < 0.3f))
            {
                if (waypointIdx >= path.vectorPath.Count - 1)
                {
                    EndMove();
                    return (ActionResult)0;
                }
                waypointIdx++;
                previousNodePos = nextNodePos;
                nextNodePos = path.vectorPath[waypointIdx];
                int num4 = -1;
                for (int k = 0; k < path.path.Count; k++)
                {
                    if (path.path[k].position == new Int3(nextNodePos))
                    {
                        num4 = k;
                        break;
                    }
                }
                if (num4 != -1 && unitCtrlr.interactivePoints.Count > 0 && num4 > 0 && path.path[num4 - 1].GraphIndex == 1 && path.path[num4].GraphIndex == 1)
                {
                    for (int l = 0; l < unitCtrlr.interactivePoints.Count; l++)
                    {
                        if (!(unitCtrlr.interactivePoints[l] is ActionZone))
                        {
                            continue;
                        }
                        ActionZone actionZone = (ActionZone)unitCtrlr.interactivePoints[l];
                        for (int m = 0; m < actionZone.destinations.Count; m++)
                        {
                            ActionDestination actionDestination = actionZone.destinations[m];
                            if (new Int3(actionDestination.destination.transform.position) == path.path[num4].position)
                            {
                                unitCtrlr.interactivePoint = actionZone;
                                unitCtrlr.activeActionDest = actionDestination;
                                unitCtrlr.ValidMove();
                                CleanUp();
                                SkillId skillId2 = SkillId.NONE;
                                switch (actionDestination.actionId)
                                {
                                    case UnitActionId.CLIMB_3M:
                                    case UnitActionId.CLIMB_6M:
                                    case UnitActionId.CLIMB_9M:
                                    case UnitActionId.CLIMB:
                                        skillId2 = SkillId.BASE_CLIMB;
                                        break;
                                    case UnitActionId.JUMP_3M:
                                    case UnitActionId.JUMP_6M:
                                    case UnitActionId.JUMP_9M:
                                    case UnitActionId.JUMP:
                                        skillId2 = SkillId.BASE_JUMPDOWN;
                                        break;
                                    case UnitActionId.LEAP:
                                        skillId2 = SkillId.BASE_LEAP;
                                        break;
                                }
                                ActionStatus action2 = unitCtrlr.GetAction(skillId2);
                                if (action2.Available)
                                {
                                    unitCtrlr.SendInteractiveAction(skillId2, actionZone);
                                    return (ActionResult)0;
                                }
                                return (ActionResult)2;
                            }
                        }
                    }
                }
            }
            unitCtrlr.SetAnimSpeed(1f);
            Quaternion quaternion = default(Quaternion);
            quaternion.SetLookRotation(nextNodePos - unitCtrlr.transform.position, Vector3.up);
            Vector3 eulerAngles = quaternion.eulerAngles;
            eulerAngles.x = 0f;
            eulerAngles.z = 0f;
            unitCtrlr.transform.rotation = Quaternion.Euler(eulerAngles);
            return (ActionResult)1;
        }
        return (ActionResult)1;
    }

    public override void Stop(AI ai)
    {
        base.Stop(ai);
        CleanUp();
    }

    public virtual void GotoNextState()
    {
        unitCtrlr.StateMachine.ChangeState(42);
    }

    protected void EndMove()
    {
        moveEnding = true;
        CleanUp();
        unitCtrlr.AICtrlr.atDestination = true;
        if (!unitCtrlr.Engaged)
        {
            unitCtrlr.ClampToNavMesh();
        }
        unitCtrlr.ValidMove();
        unitCtrlr.SetFixed(fix: true);
        if (!unitCtrlr.wasEngaged && unitCtrlr.Engaged)
        {
            unitCtrlr.SendEngaged(unitCtrlr.transform.position, unitCtrlr.transform.rotation);
        }
        else
        {
            GotoNextState();
        }
    }

    private void CleanUp()
    {
        unitCtrlr.AICtrlr.movingToSearchPoint = false;
        unitCtrlr.RestoreAlliesNavCutterSize();
        if (path != null)
        {
            unitCtrlr.AICtrlr.currentPath = null;
            path.Release(unitCtrlr.gameObject);
            path = null;
        }
        if (!unitCtrlr.GetComponent<Rigidbody>().isKinematic)
        {
            unitCtrlr.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
        unitCtrlr.SetAnimSpeed(0f);
    }
}
