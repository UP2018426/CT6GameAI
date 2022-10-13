using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// This is our Zombie Character. It requires a Zombie Blackboard (ZombieBB) component
/// </summary>
[RequireComponent(typeof(ZombieBB))]
public class Zombie : MonoBehaviour {

    public float MoveSpeed = 10.0f;

    private Vector3 MoveLocation;
    private bool IsMoving = false;

    private BTNode BTRootNode;
    // Use this for initialization
    void Start()
    {
        MoveLocation = transform.position;

        //CREATING OUR ZOMBIE BEHAVIOUR TREE

        //Get reference to Zombie Blackboard
        ZombieBB bb = GetComponent<ZombieBB>();

        //Create our root selector
        Selector rootChild = new Selector(bb); // selectors will execute it's children 1 by 1 until one of them succeeds
        BTRootNode = rootChild;

        //Flee Sequence
        CompositeNode fleeSequence = new Sequence(bb); // The sequence of actions to take when Fleeing
        FleeDecorator fleeRoot = new FleeDecorator(fleeSequence, bb); // defines the condition required to enter the flee sequence (see FleeDecorator)
        fleeSequence.AddChild(new CalculateFleeLocation(bb)); // calculate a destination to flee to (just random atm)
        fleeSequence.AddChild(new ZombieMoveTo(bb, this)); // move to the calculated destination
        fleeSequence.AddChild(new ZombieWaitTillAtLocation(bb, this)); // wait till we reached destination
        fleeSequence.AddChild(new ZombieStopMovement(bb, this)); // stop movement
        fleeSequence.AddChild(new DelayNode(bb, 2.0f)); // wait for 2 seconds

        //Fight sequence
        CompositeNode FightSequence = new Sequence(bb); // The sequence of actions to take when Fighting
        FightDecorator fightRoot = new FightDecorator(FightSequence, bb); //defines the condition required to enter the fight sequence(see FightDecorator)
        
        //Defining a sequence for when the Zombie is to do it's combo attack, this is a nested within our FightSequence
        Sequence ZombieCombo = new Sequence(bb); 
        ZombieCombo.AddChild(new ZombieClawPlayer(bb)); // claw the player
        ZombieCombo.AddChild(new DelayNode(bb, 0.8f)); // wait for 0.8 seconds
        ZombieCombo.AddChild(new ZombieBitePlayer(bb)); // bite the player
        ZombieCombo.AddChild(new DelayNode(bb, 1.5f)); // wait for 1.5 seconds

        FightSequence.AddChild(new ZombieMoveToPlayer(bb, this)); // constantly move to player until within range
        FightSequence.AddChild(new ZombieStopMovement(bb, this)); // stop movement
        FightSequence.AddChild(ZombieCombo); // perform combo sequence


        //Adding to root selector
        rootChild.AddChild(fleeRoot);
        rootChild.AddChild(fightRoot);

        //Execute our BT every 0.1 seconds
        InvokeRepeating("ExecuteBT", 0.1f, 0.1f);
    }

    // Update is called once per frame
    void Update()
    {
        if (IsMoving)
        {
            Vector3 dir = MoveLocation - transform.position;
            transform.position += dir.normalized * MoveSpeed * Time.deltaTime;
        }
    }

    public void ZombieMoveTo(Vector3 MoveLocation)
    {
        IsMoving = true;
        this.MoveLocation = MoveLocation;
    }

    public void StopMovement()
    {
        IsMoving = false;
    }

    public void ExecuteBT()
    {
        BTRootNode.Execute();
    }
}

/// <summary>
/// Flee sequence stuff
/// </summary>
public class FleeDecorator : ConditionalDecorator
{
    ZombieBB zBB;
    public FleeDecorator(BTNode WrappedNode, Blackboard bb) : base(WrappedNode, bb)
    {
        zBB = (ZombieBB)bb;
    }

    /// <summary>
    /// Perhaps you could make this modular? Take in a int in the consutructor and use that instead of 10 as the hard-coded value
    /// You can then use an InverterDecorator to invert the results when checking between Fleeing and Fighting!
    /// </summary>
    public override bool CheckStatus()
    {
        return zBB.PlayerHealth > 10;
    }
}

public class CalculateFleeLocation : BTNode
{
    private ZombieBB zBB;

    public CalculateFleeLocation(Blackboard bb) : base(bb)
    {
        zBB = (ZombieBB)bb;
    }

    public override BTStatus Execute()
    {
        Debug.Log("Calculating move to location");
        zBB.MoveToLocation = new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f));
        return BTStatus.SUCCESS;
    }
}

public class ZombieMoveTo : BTNode
{
    private ZombieBB zBB;
    private Zombie zombieRef;

    public ZombieMoveTo(Blackboard bb, Zombie zombay) : base(bb)
    {
        zBB = (ZombieBB)bb;
        zombieRef = zombay;
    }

    public override BTStatus Execute()
    {
        Debug.Log("Moving to location");
        zombieRef.ZombieMoveTo(zBB.MoveToLocation);
        return BTStatus.SUCCESS;
    }
}

public class ZombieWaitTillAtLocation : BTNode
{
    private ZombieBB zBB;
    private Zombie zombieRef;

    public ZombieWaitTillAtLocation(Blackboard bb, Zombie zombay) : base(bb)
    {
        zBB = (ZombieBB)bb;
        zombieRef = zombay;
    }

    public override BTStatus Execute()
    {
        BTStatus rv = BTStatus.RUNNING;
        if ((zombieRef.transform.position - zBB.MoveToLocation).magnitude <= 1.0f)
        {
            Debug.Log("Reached target");
            rv = BTStatus.SUCCESS;
        }
        return rv;
    }
}

///Fight sequence stuff
public class FightDecorator : ConditionalDecorator
{
    ZombieBB zBB;
    public FightDecorator(BTNode WrappedNode, Blackboard bb) : base(WrappedNode, bb)
    {
        zBB = (ZombieBB)bb;
    }

    public override bool CheckStatus()
    {
        return zBB.PlayerHealth <= 10;
    }
}

public class ZombieMoveToPlayer : BTNode
{
    private ZombieBB zBB;
    private Zombie zombieRef;
    bool FirstRun = true;
    public ZombieMoveToPlayer(Blackboard bb, Zombie zombay) : base(bb)
    {
        zBB = (ZombieBB)bb;
        zombieRef = zombay;
    }

    public override BTStatus Execute()
    {
        if (FirstRun)
        {
            FirstRun = false;
            Debug.Log("Moving to player"); 
            // perhaps the BTNode should have some "start" function that
            // can be overridden in child classes so we don't have to do this?
        }
        BTStatus rv = BTStatus.RUNNING;
        zombieRef.ZombieMoveTo(zBB.PlayerLocation);
        if ((zombieRef.transform.position - zBB.PlayerLocation).magnitude <= 1.0f)
        {
            Debug.Log("Reached the player");
            rv = BTStatus.SUCCESS;
            FirstRun = true;
        }
        return rv;
    }

    public override void Reset()
    {
        base.Reset();
        FirstRun = true;
    }
}

public class ZombieClawPlayer : BTNode
{
    public ZombieClawPlayer(Blackboard bb) : base(bb)
    {
    }

    public override BTStatus Execute()
    {
        BTStatus rv = BTStatus.SUCCESS;
        Debug.Log("Zombie Clawing");
        return rv;
    }
}

public class ZombieBitePlayer : BTNode
{
    public ZombieBitePlayer(Blackboard bb) : base(bb)
    {
    }

    public override BTStatus Execute()
    {
        BTStatus rv = BTStatus.SUCCESS;
        Debug.Log("Zombie Biting");
        return rv;
    }
}

public class ZombieStopMovement : BTNode
{
    private Zombie zombieRef;
    public ZombieStopMovement(Blackboard bb, Zombie zombay) : base(bb)
    {
        zombieRef = zombay;
    }

    public override BTStatus Execute()
    {
        zombieRef.StopMovement();
        return BTStatus.SUCCESS;
    }
}
