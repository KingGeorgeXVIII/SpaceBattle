using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleControllerScript : MonoBehaviour {

	[Header("Ship's First Intro Turn")]
	public float minFirstTurn;
	public float maxFirstTurn;

	[Header("Team Stuff")]
	public List<GameObject> greenAI = new List<GameObject> ();
	public List<GameObject> purpleAI = new List<GameObject> ();
	public List<GameObject> blueAI = new List<GameObject> ();

	public string teamOne = "Empty";
	public int teamOneRemaining;
	public string teamTwo = "Empty";
	public int teamTwoRemaining;

	[SerializeField] string currentAiType;
	[SerializeField] List<string> aiTypes = new List<string> ();

	WaveSetUpScript waveScript;
	SquadSpawnTester waveHandlerScript;
	BattleSceneUIHandler battleUIScript;

	[SerializeField] int minShipRemaining;

	//UI
	[SerializeField] Text teamOneNameDisplay;
	[SerializeField] Text teamTwoNameDisplay;
	[SerializeField] Text teamOneFleetCount;
	[SerializeField] Text teamTwoFleetCount;

	void Awake () 
	{
		waveScript = GameObject.Find ("Wave Info Handler").GetComponent<WaveSetUpScript> ();
		waveHandlerScript = GetComponent<SquadSpawnTester> ();
		battleUIScript = GameObject.FindObjectOfType <BattleSceneUIHandler> ();
		teamOne = waveScript.playerColor;
		teamTwo = waveScript.aiColor;

		if (teamOne == "Green") {
			teamOneNameDisplay.color = Color.green;
			teamOneFleetCount.color = Color.green;
		}
		else if (teamOne == "Purple") {
			teamOneNameDisplay.color = Color.magenta;
			teamOneFleetCount.color = Color.magenta;
		}
		else if (teamOne == "Blue") {
			teamOneNameDisplay.color = Color.blue;
			teamOneFleetCount.color = Color.blue;
		}

		if (teamTwo == "Green") {
			teamTwoNameDisplay.color = Color.green;
			teamTwoFleetCount.color = Color.green;
		}
		else if (teamTwo == "Purple") {
			teamTwoNameDisplay.color = Color.magenta;
			teamTwoFleetCount.color = Color.magenta;
		}
		else if (teamTwo == "Blue") {
			teamTwoNameDisplay.color = Color.blue;
			teamTwoFleetCount.color = Color.blue;
		}
	}

	void Start()
	{
		if (waveScript.teamTwoIsAi == true) {
			currentAiType = aiTypes[UnityEngine.Random.Range (0, aiTypes.Count - 1)];
		}

		UpdateTeamRemaningCounts ();
	}

	public void UpdateTeamRemaningCounts()
	{
		if (teamOne == "Green")
			teamOneRemaining = greenAI.Count;
		else if(teamOne == "Purple")
			teamOneRemaining = purpleAI.Count;
		else if(teamOne == "Blue")
			teamOneRemaining = blueAI.Count;

		if (teamTwo == "Green")
			teamTwoRemaining = greenAI.Count;
		else if(teamTwo == "Purple")
			teamTwoRemaining = purpleAI.Count;
		else if(teamTwo == "Blue")
			teamTwoRemaining = blueAI.Count;

		teamOneFleetCount.text = teamOneRemaining.ToString();
		teamTwoFleetCount.text = teamTwoRemaining.ToString ();


		//Check to see if game is over
		if (waveHandlerScript.waveNumber >= 2) {

			if (teamOneRemaining == 0) 
			{
				Debug.Log ("Team 2 Won!");
				battleUIScript.ActivateEndGameUI (2);
			} 
			else if (teamTwoRemaining == 0) 
			{
				Debug.Log ("Team 1 Won!");
				battleUIScript.ActivateEndGameUI (1);
			}
		}
	}

	public void SubtractFromList(string color, GameObject ship)
	{
		if (color == "Green")
			greenAI.Remove (ship);
		else if (color == "Purple")
			purpleAI.Remove (ship);
		else if (color == "Blue")
			blueAI.Remove (ship);

		UpdateTeamRemaningCounts();
	}
}