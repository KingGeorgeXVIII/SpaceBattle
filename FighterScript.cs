using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FighterScript : MonoBehaviour {

////* States *////
	public enum SHIPAISTATES
	{
		INTRO,
		MOVE,
		TURN,
		CHASE,
		OUTRUN,
		EVADE,
		EMERGENCY180,
		//TODO DISABLED,
	}

	public SHIPAISTATES currentState = SHIPAISTATES.INTRO;

	Dictionary<SHIPAISTATES, Action> aiStates = new Dictionary<SHIPAISTATES, Action>();

	SHIPAISTATES lastState;
//////////***/////////


	public int layer;
	int layerMask;
	public int invLayerMask;

////General Ship Variables////
	[Header("General Stats")]
	public string teamColor;
	public float FieldOfView;

	[SerializeField] float maxHealth;
	[SerializeField] float currentHealth;
//////////***//////////



////Movement Variables////
	[Header("Speed Stats")]
	public float maxMoveSeed;
	float chaseSpeed;

	Vector3 velocity;
	Vector3 startForce;
//////////***//////////



////* Turning Variables *////
	[Header("Turning Stuff")]
	float firstTurnMin;
	float firstTurnMax;
	public bool battleIntro = true;
	public float minTurnTime;
	public float maxTurnTime;
	private float currentTurnTime;
	private float turnTimer;
	[SerializeField] float turnDampSpeed;
	float duration;
	[SerializeField] GameObject Emergency180;

	[Header("List of Turns")]
	[SerializeField] List<GameObject> turnList = new List<GameObject> ();

	private int selectedTurn;
	float turnProgress;
	GameObject currentSpline;
/////////***/////////



/////* Out of Bounds Emergency Turn Around *////
	public bool canDisableForceTurnAround;
	public float disableForceTurnAfter;
	float forceTurnTimer;
	[SerializeField] bool isDoingForcedTurn;

	public bool busyDoingForceTurn()
	{
		return isDoingForcedTurn;
	}

	public bool isOutOfBounds = true;
	GameObject turnAroudTarget;

	//If ship stays out of bounds to long, the ship is reset to its start location.
	[SerializeField] float resetLocationTimer;
	float resetTimer;
	Vector3 startLocation;
	Quaternion startRot;
/////////***/////////

	 

////* Chase *////
	[Header("Chase Stuff")]
	public GameObject chaseTarget;
	public bool isChasing = false;
/////////***/////////



////* Outrun and Evade *////
	//The ship's purseur
	GameObject evadeTarget;
	public bool isBeingChased = false;

	//The min and max amount of time before the ship does a turn while being chased
	[Header("Evade Stats")]
	public float minEvadeTurnTime;
	public float maxEvadeTurnTime;

	//The percentage change of evading the pursuer while turning.
	[Range(0, 1f)]
	public float turnEvadeChance;

	//The min and max amount of time before the ship does an evasive maneuver while being chased
	public float minManeuverTime;
	public float maxManeuverTime;

	//The percentage change of evading the pursuer while doing a maneuver
	[Range(0, 1f)]
	public float maneuverEvadeChance;

	float currentManeuverTime;
	float maneuverTimer;
	int selectedManuever;
	bool spawnManeuver;
	[SerializeField] bool isdoingManeuver;

	[Header("List of Maneuvers")]
	[SerializeField] List<GameObject> maneuverList = new List<GameObject> ();
/////////***/////////



////* Spline Variables* ////<summary>
/// Spline variables for turning and doing evasive maneuvers.
/// To help with the splines, I am using an asset pack from the Unity Asset Store
/// called "Dreamteck Splines"
/// Found Here:
/// https://assetstore.unity.com/packages/tools/dreamteck-splines-61926
	/// </summary>
	Dreamteck.Splines.SplineFollower followerScript;
	float splineLength;
	float distanceTraveled;
	float turnTime;
	float turnWatch;
	GameObject prefabSpline;
/////////***/////////



////* Squad Variables *//// <summary>
/// Each ship is part of a squad. Squadmates can interact with each other by
/// coming to each other's aid. When a squad has lost enough of its members,
/// the remaining members enter rage mode, where they are constantly shooting
/// and have a larger field of view.
/// </summary>
//The object that holds all of the squad data (i.e. other ships in squad)
	public SquadSpawnScript squadScript;
	public List<GameObject> squadList = new List<GameObject> ();
	[SerializeField] int startSquadSize;
	[SerializeField] int currentSquadSize;

	//Ships can occasionally call for help from a squad when they are being chased by an enemy.
	public bool canCallForHelp = true;
	public float cfhChance;
	public float callForHelpRange;
	GameObject closestShip;
	float shipDist;

	//Rage mode variables
	public bool rageModeActive = false;
	//When there is "rageModePercent" of squad mates remaining in the squad, Rage Mode is triggered
	[Range (0,1)]
	[SerializeField] float rageModePercent;
	//This is the actual number of shipps remaining when Rage Mode is triggered.
	//i.e. if percent is 40% and there are 10 ships in the squad rageModeTriggeredAt would be 4
	int rageModeTriggeredAt;
//////////***//////////


////* Death Variables *////
	public bool hasBeenDestroyed;
	//an explosion effect to spawn when ship is destroyed
	[SerializeField] GameObject explosion;
//////////***//////////



////* General Variables and References *////
	//The script that handles the ships sight.
	RadarScript radar;
	//The master script for the battle
	BattleControllerScript battleController;
	CameraController mainCam;
	Rigidbody rBody;

	[Header("Weapons")]
	//List of every weapon on ship
	[SerializeField] List<GameObject> weapons = new List<GameObject>();

	//Damage over time variables
	bool isTakingDamageOverTime = false;
	float dpsValue;
	float damageOverTimeLength;
	float dotTimer;
	//This is the fire effect that is active when taking damage over time.
	[SerializeField] GameObject dotFire;
//////////***//////////


	void Awake()
	{
		battleController = GameObject.Find ("Battle Controller").GetComponent<BattleControllerScript> ();

		//Adds ship to the master list of ships in the battle.
		if (teamColor == "Green") {
			battleController.greenAI.Add (this.gameObject);
		} else if (teamColor == "Purple") {
			battleController.purpleAI.Add (this.gameObject);
		} else if (teamColor == "Blue") {
			battleController.blueAI.Add (this.gameObject);
		}

		//Used to make the ships appear under the UI
		layerMask = 1 << layer;
		invLayerMask = ~(layerMask);

		mainCam = GameObject.FindWithTag ("MainCamera").GetComponent<CameraController> ();
		firstTurnMin = battleController.minFirstTurn;
		firstTurnMax = battleController.maxFirstTurn;
	}

	void Start () 
	{
		radar = GetComponentInChildren<RadarScript> ();
		rBody = GetComponent<Rigidbody> ();
		rBody.AddForce (transform.forward*maxMoveSeed, ForceMode.VelocityChange);
		turnAroudTarget = GameObject.Find ("TurnAroundTarget");
		startLocation = transform.position;
		startRot = transform.rotation;
		currentHealth = maxHealth;

		aiStates.Add (SHIPAISTATES.INTRO, new Action (IntroState));
		aiStates.Add (SHIPAISTATES.MOVE, new Action (MoveState));
		aiStates.Add (SHIPAISTATES.TURN, new Action (TurnState));
		aiStates.Add (SHIPAISTATES.CHASE, new Action (ChaseState));
		aiStates.Add (SHIPAISTATES.OUTRUN, new Action (OutRunState));
		aiStates.Add (SHIPAISTATES.EVADE, new Action (EvadeState));
		aiStates.Add (SHIPAISTATES.EMERGENCY180, new Action (Emergency180State));

		//Set when ship will do its first turn
		RandomizeTurnTime (firstTurnMin, firstTurnMax);

		startSquadSize = squadList.Count + 1;
		currentSquadSize = startSquadSize;
		rageModeTriggeredAt = (int) (startSquadSize * rageModePercent);
	}

	void FixedUpdate () 
	{
		//Run current state function
		aiStates [currentState].Invoke ();

		//If the ship is out of bounds, it is forced to do a 180 turn
		if (isOutOfBounds == true && isDoingForcedTurn == false && battleIntro == false)
			ForceTurnAround ();

		//If ship is out of bounds for to long, it resets to its start location
		if (isOutOfBounds == true && isDoingForcedTurn == true) 
		{
			resetTimer += Time.deltaTime;

			if (resetTimer >= resetLocationTimer) {
				isDoingForcedTurn = false;
				resetTimer = 0;
				ResetTurnVariables ();
				rBody.velocity = Vector3.zero;
				rBody.angularVelocity = Vector3.zero;


				for (int i = 0; i < weapons.Count; i++) 
				{
					if(weapons[i] != null)
						weapons [i].GetComponent<WeaponScript> ().canShoot = false;
				}
					
				battleIntro = true;
				isOutOfBounds = true;
				transform.position = startLocation;
				transform.rotation = startRot;
				ReApplyForce (maxMoveSeed);
				ChangeState(SHIPAISTATES.INTRO);
			}
			
		}

		//If ship is taking damage over time, applies damage
		if (isTakingDamageOverTime == true)
			ApplyDamageOverTime ();
	}

	void LateUpdate()
	{
		//Keeps the ship from doing weird things when it isn't supposed to
		if (isdoingManeuver == false) {
			//Prevents the ship from rising on the Y axis, allowing it to fly up above the battle.
			transform.position = new Vector3 (transform.position.x, startLocation.y, transform.position.z);
			//Keeps the ship from rotating onto its side or upside down
			transform.eulerAngles = new Vector3 (0, transform.eulerAngles.y, 0);
		}
	}

	//Used when ship first spawns into battle or if the ship stays out of bounds to long and resets.
	//This stat is basically the "charge into battle" state
	void IntroState()
	{
		if (battleIntro == false) {
			ChangeState (SHIPAISTATES.MOVE);
		}
	}

	//Handles the straigh forward movement of the ship
	void MoveState()
	{
		//Keeps track of time until next turn
		turnTimer += Time.deltaTime;

		//The turns and manuevers that the ships preform all use splines. I purchased a spline
		//pack from the Unity Asset Store. For the ships to move on the splines, they require a component
		//from the package to be attached to the ship. These components can only be associated with spline however.
		//So each time the ship completes a spline,the old component has to be romved and a new one added.
		//This removes the old component.
		if (followerScript != null)
			Destroy (this.gameObject.GetComponent<Dreamteck.Splines.SplineFollower> ());

		//When it is time to turn...
		if (turnTimer >= currentTurnTime) 
		{
			//Randomly select turn from list of turns
			selectedTurn = UnityEngine.Random.Range (0, turnList.Count);
			//reset turn timer for next time.
			turnTimer = 0;
			//Spawn selected turn spline
			GameObject prefabSpline = turnList [selectedTurn];
			currentSpline = GameObject.Instantiate (prefabSpline, transform.position, transform.rotation);
			//Adds new component that allows the ship to follow the spline
			followerScript = this.gameObject.AddComponent<Dreamteck.Splines.SplineFollower> ();
			followerScript.enabled = true;
			//Sets speed at which to follow the spline based on the current speed of the ship
			followerScript.followSpeed = rBody.velocity.magnitude;
			//Concects the spline follow component and the spline
			followerScript.computer = currentSpline.GetComponent<Dreamteck.Splines.SplineComputer> ();
			//Determine the distance the ship will travel while following the spline
			splineLength = currentSpline.GetComponent < Dreamteck.Splines.SplineComputer> ().CalculateLength ();
			//Finds the length of time it will take the ship to reach the end of the spline, by taking the
			//lenght of the time and dividing it by the ships speed. Time = distance/speed
			turnTime = splineLength / rBody.velocity.magnitude;
			//The "turnWatch" is basically a stop watch that keeps track of how long the ship has been on the spline
			turnWatch = 0;
			isdoingManeuver = true;
			ChangeState (SHIPAISTATES.TURN);
		}
	}

	void EndTurn()
	{
		//Resets the ships velocity to zero, then re-applies forward velocity.
		//The keeps the ship from behaving weirdly, like traveling sideways or backwards
		rBody.velocity = Vector3.zero;
		rBody.angularVelocity = Vector3.zero;
		ResetTurnVariables();
		ReApplyForce (maxMoveSeed);
		turnWatch = 0;

		//If the ship is not being chased, it returns to the move state, else it returns to outrun
		if (evadeTarget == null) {
			isBeingChased = false;
			ChangeState (SHIPAISTATES.MOVE);
		}
		else
			ChangeState (SHIPAISTATES.OUTRUN);
	}

	void TurnState()
	{
		//updates the time that the ship has been on the spline.
		turnWatch += Time.deltaTime;

		//Once the ship has been on the spline for the amount of time calculated by the
		//Time = Distance/Speed equation in the Move State, the ship leaves the spline.
		if (turnWatch >= turnTime) {
			isdoingManeuver = false;
			EndTurn ();
		}
	}

	void ChaseState()
	{
		//If the ship's target != null, follow target
		if (chaseTarget != null) 
		{
			Vector3 force = MoveTowardsTarget (chaseTarget.transform.position);
			velocity = force * chaseSpeed;
			Vector3 lerpVel = Vector3.Slerp (rBody.velocity, velocity, Time.deltaTime);
			rBody.velocity = lerpVel;
			transform.rotation = Quaternion.LookRotation (rBody.velocity);

			//If the target gets destroyed return to move state.
			if (chaseTarget.GetComponent<FighterScript> ().hasBeenDestroyed == true)
			{
				//If ship isn't in fight for life/last stand, disable weapons
				if (rageModeActive == false) {
					for (int i = 0; i < weapons.Count; i++) {
						if (weapons [i] != null)
							weapons [i].GetComponent<WeaponScript> ().canShoot = false;
					}
				}
					
				ChangeState (SHIPAISTATES.MOVE);
			}
		} 
		else if(chaseTarget == null)
		{
			//else go back to move state

			isChasing = false;

			//If ship isn't in fight for life/last stand, disable weapons
			if (rageModeActive == false) {
				for (int i = 0; i < weapons.Count; i++) {
					if (weapons [i] != null)
						weapons [i].GetComponent<WeaponScript> ().canShoot = false;
				}
			}
			ChangeState (SHIPAISTATES.MOVE);
		}
	}

	void OutRunState()
	{
		//This state is very similiar to the moves state. The only difference is that
		//this state includes a maneuver section. In this state, the ship can do fancy
		//flying manuevers to try to escape the enemy. The manuever section behaves
		//just like the turn section, it just uses a different list and different timer varaibles.
		turnTimer += Time.deltaTime;
		maneuverTimer += Time.deltaTime;

		if (turnTimer >= currentTurnTime) 
		{
			selectedTurn = UnityEngine.Random.Range (0, turnList.Count);
			turnTimer = 0;
			GameObject prefabSpline = turnList [selectedTurn];
			currentSpline = GameObject.Instantiate (prefabSpline, transform.position, transform.rotation);
			followerScript = this.gameObject.AddComponent<Dreamteck.Splines.SplineFollower> ();
			followerScript.enabled = true;
			followerScript.followSpeed = rBody.velocity.magnitude;
			followerScript.computer = currentSpline.GetComponent<Dreamteck.Splines.SplineComputer> ();
			splineLength = currentSpline.GetComponent < Dreamteck.Splines.SplineComputer> ().CalculateLength ();
			//print (splineLength);
			turnTime = splineLength / rBody.velocity.magnitude;
			isdoingManeuver = true;
			CheckForCallForHelp ();
			ChangeState (SHIPAISTATES.TURN);
		}

		if (maneuverTimer >= currentManeuverTime) 
		{
			selectedManuever = UnityEngine.Random.Range (0, maneuverList.Count);
			turnTimer = 0;
			maneuverTimer = 0;
			GameObject prefabSpline = maneuverList [selectedManuever];
			currentSpline = GameObject.Instantiate (prefabSpline, transform.position, transform.rotation);
			followerScript = this.gameObject.AddComponent<Dreamteck.Splines.SplineFollower> ();
			followerScript.enabled = true;
			followerScript.followSpeed = rBody.velocity.magnitude;
			followerScript.computer = currentSpline.GetComponent<Dreamteck.Splines.SplineComputer> ();
			splineLength = currentSpline.GetComponent < Dreamteck.Splines.SplineComputer> ().CalculateLength ();
			turnTime = splineLength / rBody.velocity.magnitude;
			isdoingManeuver = true;
			ChangeState (SHIPAISTATES.TURN);
		}
	}

	void EvadeState()
	{
		//the evade state currently doesn't do anything.
	}

	//The ship does a 180 if they are out of bounds
	void Emergency180State()
	{
		//The ship flies towards the center of the battle area
		Vector3 force = MoveTowardsTarget (turnAroudTarget.transform.position);
		velocity = force * maxMoveSeed;
		Vector3 lerpVel = Vector3.Slerp(rBody.velocity, velocity, Time.deltaTime);
		rBody.velocity = lerpVel;
		transform.rotation = Quaternion.LookRotation (rBody.velocity);

		//If back in bounds...
		if(isOutOfBounds == false)
		{
			//...the ship keeps moving straight for a short period of time, just to make sure
			//it won't make a turn and go back out of bounds as soon as it comes in.
			forceTurnTimer += Time.deltaTime;

			//ship returns to normal after so long
			if (forceTurnTimer >= disableForceTurnAfter) 
			{
				forceTurnTimer = 0;
				isDoingForcedTurn = false;
				resetTimer = 0;
				ResetTurnVariables ();

				if (isBeingChased == false)
				{
					//if not chasing a target, return to move state
					if (isChasing == false) {
						rBody.velocity = Vector3.zero;
						rBody.angularVelocity = Vector3.zero;
						ReApplyForce (maxMoveSeed);

						//Re/disable weapons
						if (rageModeActive == false) {
							for (int i = 0; i < weapons.Count; i++) {
								if (weapons [i] != null)
									weapons [i].GetComponent<WeaponScript> ().canShoot = false;
							}
						} else {
							for (int i = 0; i < weapons.Count; i++) {
								if (weapons [i] != null)
									weapons [i].GetComponent<WeaponScript> ().canShoot = true;
							}
						}
						ChangeState (SHIPAISTATES.MOVE);
					} else if (isChasing == true && chaseTarget != null)
						ChangeState (SHIPAISTATES.CHASE);
					else {
						isChasing = false;
						ChangeState (SHIPAISTATES.MOVE);
					}
				} 
				else if (isBeingChased == true) 
				{
					//if is chasing and also being chased...
					if(isChasing == true && chaseTarget != null)
						ChangeState (SHIPAISTATES.CHASE);
					else 
					{
						//return to outrun state
						if (evadeTarget != null) {
							rBody.velocity = Vector3.zero;
							rBody.angularVelocity = Vector3.zero;
							ReApplyForce (maxMoveSeed);

							if (rageModeActive == false) {
								for (int i = 0; i < weapons.Count; i++) {
									if (weapons [i] != null)
										weapons [i].GetComponent<WeaponScript> ().canShoot = false;
								}
							} else {
								for (int i = 0; i < weapons.Count; i++) {
									if (weapons [i] != null)
										weapons [i].GetComponent<WeaponScript> ().canShoot = true;
								}
							}
							isChasing = false;
							ChangeState (SHIPAISTATES.OUTRUN);
						}else
							ChangeState(SHIPAISTATES.MOVE);
					}
				}
			}
		}
	}

	//*****Custom Functions*****\\

	Vector3 MoveTowardsTarget(Vector3 target)
	{
		return (target - transform.position).normalized;
	}

	void ChangeState(SHIPAISTATES newState)
	{
		currentState = newState;
	}

	void ReApplyForce(float forceAmount)
	{
		rBody.AddForce (transform.forward*forceAmount, ForceMode.VelocityChange);
	}

	void RandomizeTurnTime(float min, float max)
	{
		currentTurnTime = UnityEngine.Random.Range (min, max);
	}

	void RandomizeManeuverTime(float min, float max)
	{
		currentManeuverTime = UnityEngine.Random.Range (min, max);
	}

	//When number of active ships gets low, Ships will start shooting more and their awareness increases
	public void StartLastStand()
	{
		rageModeActive = true;

		//doubles sight distance
		radar.sphereCol.radius *= 2;

		//makes ship starting shooting like mad
		for (int i = 0; i < weapons.Count; i++) {
			if (weapons [i] != null)
				weapons [i].GetComponent<WeaponScript> ().canShoot = true;
		}
	}

	public void SetBeingChased(GameObject chaserObject)
	{
		if (isBeingChased == false) {
			isBeingChased = true;

			//set ship that this ship is trying to escape from.
			evadeTarget = chaserObject;
			//Set when ship will do a turn and manuever
			RandomizeTurnTime (minTurnTime, maxTurnTime);
			RandomizeManeuverTime (minManeuverTime, maxManeuverTime);


			//activate/de-activate weapons
			if (rageModeActive == false) {
				for (int i = 0; i < weapons.Count; i++) {
					if (weapons [i] != null)
						weapons [i].GetComponent<WeaponScript> ().canShoot = false;
				}
			} else {
				for (int i = 0; i < weapons.Count; i++) {
					if (weapons [i] != null)
						weapons [i].GetComponent<WeaponScript> ().canShoot = true;
				}
			}
			//Change to outrun state
			ChangeState (SHIPAISTATES.OUTRUN);
			//stops doing a turn and resets turn variables
			ResetTurnVariables ();
			//resets velocity and applies forward velocity
			rBody.velocity = Vector3.zero;
			rBody.angularVelocity = Vector3.zero;
			ReApplyForce (maxMoveSeed);
		} else
			return;
	}

	//Check if shipped escaped from pursuer. The chance variable is a number between 0 and 1.
	//Currently the ship controls the evade chance, but each turn and maneuver will have its own evade chance
	//that will further increase the size of the chance variable.
	void CheckIfEvaded(float chance)
	{
		//pick a number bettween 0 and 1
		float percentNumber = UnityEngine.Random.Range (0, 1f);

		//If the number randomly picked is lower than the chance, the ship successfully evades its pursuer.
		//ie. if the random number was .6 and the ship had a 80% chance to escape, the evade would be successful.
		if (percentNumber <= chance) 
		{
			if (evadeTarget != null) 
			{
				//Tell the pursuer that they lost their target.
				evadeTarget.GetComponent<FighterScript> ().LostTarget ();
			}

			//Reset back to move state
			isBeingChased = false;
			ChangeState (SHIPAISTATES.MOVE);
			canCallForHelp = true;
		}
	}

	public void LostTarget()
	{
		chaseTarget = null;
		isChasing = false;

		if (rageModeActive == false) {
			for (int i = 0; i < weapons.Count; i++) {
				if (weapons [i] != null)
					weapons [i].GetComponent<WeaponScript> ().canShoot = false;
			}
		} else {
			for (int i = 0; i < weapons.Count; i++) {
				if (weapons [i] != null)
					weapons [i].GetComponent<WeaponScript> ().canShoot = true;
			}
		}
		ChangeState (SHIPAISTATES.MOVE);
	}

	//This function resets all of the variables and splines used for turning. This prevents
	//the ship from having more than one active spline, starting somewhere in the middle
	//of the next spawned spline, or moving to slow or fast along a new spline.
	public void ResetTurnVariables()
	{
		turnWatch = 0;
		isdoingManeuver = false;

		Destroy (this.gameObject.GetComponent<Dreamteck.Splines.SplineFollower> ());

		if (currentSpline != null)
			Destroy (currentSpline);
	}

	//Begin chasing a target (enemy ship)
	public void StartChase()
	{
		ResetTurnVariables ();

		//Activate weapons
		for (int i = 0; i < weapons.Count; i++) 
		{
			if(weapons[i] != null)
				weapons [i].GetComponent<WeaponScript> ().canShoot = true;
		}

		//Get reference to target's main script
		FighterScript chasedScript = chaseTarget.GetComponent<FighterScript> ();

		//If the targets max spped is lower than this ship's, this ship will slow down
		//and match the speed of its target.
		if (chasedScript.maxMoveSeed < maxMoveSeed)
			chaseSpeed = chasedScript.maxMoveSeed;
		else
			chaseSpeed = maxMoveSeed;

		ChangeState (SHIPAISTATES.CHASE);
	}

	//Forces ship to do a 180 turn. This is used when the ship leaves the battle area.
	public void ForceTurnAround()
	{
		ResetTurnVariables ();
		isDoingForcedTurn = true;
		ChangeState (SHIPAISTATES.EMERGENCY180);
	}

	//This is a simple obstacle avoidance function I am working on. Its still in development
	//and not currently being used.
	public void AvoidObstactle()
	{
		selectedTurn = UnityEngine.Random.Range (0, 2);
		turnTimer = 0;
		GameObject prefabSpline = turnList [selectedTurn];
		currentSpline = GameObject.Instantiate (prefabSpline, transform.position, transform.rotation);
		followerScript = this.gameObject.AddComponent<Dreamteck.Splines.SplineFollower> ();
		followerScript.enabled = true;
		followerScript.followSpeed = rBody.velocity.magnitude;
		followerScript.computer = currentSpline.GetComponent<Dreamteck.Splines.SplineComputer> ();
		splineLength = currentSpline.GetComponent < Dreamteck.Splines.SplineComputer> ().CalculateLength ();
		turnTime = splineLength / rBody.velocity.magnitude;
		turnWatch = 0;
		isdoingManeuver = true;
		ChangeState (SHIPAISTATES.TURN);
	}

	//This is the function that is called when the ship takes damage. It takes up to four variables,
	//all given by the enemy projectile when they collide.
	//damageAmount = The amount of damage that the intial collision with projectile does.
	//dot = Can the projectile do Damage Over Time?
	//dotValue = If the projectile does do damage over time, this is the total damage done, this is not dps.
	//dotTime = The length of time that the ship takes damage for.
	public void TakeDamage(float damageAmount, bool dot, float dotValue, float dotTime)
	{
		//Subtract initial impact damage from health.
		currentHealth -= damageAmount;

		//If projectile does damage over time, reset all dot values and start dot process.
		if (dot == true) {
			isTakingDamageOverTime = true;
			dpsValue = dotValue;
			damageOverTimeLength = dotTime;
			dotTimer = 0;
			dotFire.SetActive (true);
		}

		//Check if out of health
		CheckIfDead ();
	}

	//Used when taking damage over time
	void ApplyDamageOverTime()
	{
		//Keeps track of how long ship has been taking damage
		dotTimer += Time.deltaTime;

		//Stops taking damage once time limit has been reached
		if (dotTimer >= damageOverTimeLength) {
			isTakingDamageOverTime = false;
			dotFire.SetActive (false);
		}

		currentHealth -= dpsValue*Time.deltaTime;

		//Check if health is gone
		CheckIfDead ();
	}

	void CheckIfDead()
	{
		//What to do if ship runs out of health
		if (currentHealth <= 0 && hasBeenDestroyed == false) {
			//Disable special effects
			isTakingDamageOverTime = false;

			//Reset Turn Variables, this is incase ship is in the middle of a turn or manuever
			ResetTurnVariables ();
			if (currentSpline != null)
				Destroy (currentSpline);

			//Reset ship's current velocity and make ship travel in a straight line. Makes for a better, more realistic looking explosion.
			rBody.velocity = Vector3.zero;
			rBody.angularVelocity = Vector3.zero;
			ReApplyForce (maxMoveSeed);

			//Remove this ship from overal list of ships active in level.
			battleController.SubtractFromList (teamColor, this.gameObject);

			for (int s = 0; s < squadList.Count; s++) {
				FighterScript squadmateScript = squadList[s].GetComponent<FighterScript> ();
				squadmateScript.LostSquadMate (this.gameObject);
			}

			hasBeenDestroyed = true;

			//Reference to a script that creates a neat explosion effect, can be preformance heavey when lots of ships
			//are exploding at one tome though. These two lines are optional
			//GenerateDebrisOnDestroy blowUpScript = GetComponent<GenerateDebrisOnDestroy> ();
			//blowUpScript.SelfDestruct = true;

			//If not using the "GenerateDebrisOnDestroy" script, can use this to create a smaller explosion.
			Instantiate(explosion, transform.position, transform.rotation);

			//Turn off all weapons. This keeps the ship for shooting while exploding.
			for (int i = 0; i < weapons.Count; i++) 
			{
				if(weapons[i] != null)
					weapons [i].GetComponent<WeaponScript> ().canShoot = false;
			}

			//And finally, destroy the ship.
			Destroy (this.gameObject);
		}
	}

	public void LostSquadMate(GameObject fallenSquadMate)
	{
		squadList.Remove (fallenSquadMate);
		currentSquadSize = squadList.Count + 1;

		if (currentSquadSize <= rageModeTriggeredAt)
			StartLastStand ();
	}

	//Check if call for help was successful
	void CheckForCallForHelp()
	{
		float actualNumber = UnityEngine.Random.Range (0, 100);

		if (actualNumber < cfhChance) {
			CallForHelp ();
			canCallForHelp = false;
		}
	}

	//While being chased, the ship can call for a friendly ship to attempt to destroy the chaser.
	void CallForHelp()
	{
		//Reset closet ship
		closestShip = null;

		//Check for closest squad mate
		for (int i = 0; i < squadList.Count; i++) 
		{
			float dist;

			if (squadList [i] != null) {
				dist = Vector3.Distance (transform.position, squadList [i].transform.position);

				if (dist < callForHelpRange) 
				{
					if (shipDist != 0) 
					{
						if (dist < shipDist) 
						{
							shipDist = dist;
							closestShip = squadList [i];
						}
					} 
					else
						shipDist = dist;
					closestShip = squadList [i];
				}
			}
		}

		//If there is a squad mate in range, run them chase your pursuer.
		if(closestShip != null)
			closestShip.GetComponent<FighterScript> ().AssistSquadMate (evadeTarget);
	}

	//Set target to an enemy that is chasing a squad mate.
	public void AssistSquadMate(GameObject newTarget)
	{
		chaseTarget = newTarget;
		if (newTarget != null) {
			newTarget.GetComponent<FighterScript> ().evadeTarget = this.gameObject;
			newTarget.GetComponent<FighterScript> ().isBeingChased = true;
			StartChase ();
		}
	}

	//Focuses Camera on this ship
	void OnMouseDown()
	{
		mainCam.TrackObject (this.gameObject);
	}

	void OnTriggerEnter(Collider other)
	{
		//If re-entering battle area
		if (other.name == "Boundary Barrier") {
			isOutOfBounds = false;

			if (battleIntro == true)
				battleIntro = false;
		} 

		//If the ship collides with an enemy ship, take damage worth enemies max health
		if (other.transform.tag == "Ship") {
			FighterScript collScript = other.gameObject.GetComponent<FighterScript> ();
			if (collScript.teamColor != teamColor)
				TakeDamage (collScript.maxHealth, false, 0, 0);
		}
	}

	void OnTriggerExit(Collider other)
	{
		//If ship goes out of bounds
		if (other.name == "Boundary Barrier") {
			isOutOfBounds = true;
		}
	}
}