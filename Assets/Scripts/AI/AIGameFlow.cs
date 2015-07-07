using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIGameFlow : MonoBehaviour {

	#region Singleton
	
	private static AIGameFlow instance;
	
	public static AIGameFlow Instance {
		get {
			if(instance == null) {
				instance = GameObject.FindObjectOfType<AIGameFlow>();
			}
			
			return instance;
		}
	}
	
	#endregion

	public const char GS_PLAYER = 'P';
	public const char GS_EMPTY = 'E';
	public const char GS_MOVE = 'M';
	public const char GS_AI = 'A';

	[HideInInspector] public int height, width;

	public static GameStateUnit activegsUnit;

	public List<GameStateUnit> turnOrderList;
	public int curTurnOrderIndex;
	
	private GameStateUnit[,] gameState;
	private GameStateUnit[] turnOrderArray;	//A static version of the initial turn order, used to create the dynamic list below

	#region Setup related

	void Start() {
		instance = this;
	}

	void SetupGameState() {
		height = GridController.Instance.gridHeight;
		width = GridController.Instance.gridWidth;

		gameState = new GameStateUnit[height, width];
		turnOrderArray = new GameStateUnit[GameFlow.Instance.UnitTurnOrderList.Count];

		for(int i = 0; i < height; i++) {
			for(int j = 0; j < width; j++) {
				if(!GridController.Instance.tileArray[i,j].occupied) {
					gameState[i,j] = new GameStateUnit(GS_EMPTY, i, j, null);
				} else {
					GameObject occupier = GridController.Instance.tileArray[i,j].occupier;
					GameStateUnit gsUnit = null;
					AIUnit tmpUnit = null;

					if(occupier.GetComponent<Unit>().classType == ClassType.WARRIOR) {
						tmpUnit = new AIWarrior();
					}

					if(occupier.tag == "PlayerUnit") {
						tmpUnit.possibleTarget = GS_AI;
						gsUnit = new GameStateUnit(GS_PLAYER, i, j, tmpUnit);
					} else {
						tmpUnit.possibleTarget = GS_PLAYER;
						gsUnit = new GameStateUnit(GS_AI, i, j, tmpUnit);
					}

					CopyStats(occupier.GetComponent<Unit>(), gsUnit.occupier);
					gameState[i,j] = gsUnit;

					//Add the gsUnit to turn order
					turnOrderArray[GameFlow.Instance.UnitTurnOrderList.IndexOf(occupier)] = gsUnit;
				}
			}
		}

		MCTS.Instance.GetMove(new MCTSNode(null, Action.ATTACK, null));
	}

	void CopyStats(Unit from, AIUnit to) {
		to.possibleMoves = from.possibleMoves;
		to.attackRange = from.attackRange;
		to.health = from.health;
		to.damage = from.damage;
	}

	#endregion

	#region Gameflow related

	public int StartNextTurn() {

		int isGameOver = IsGameOver();

		if(isGameOver == 1 || isGameOver == 0)
			return isGameOver;

		if(++curTurnOrderIndex >= turnOrderList.Count)
			curTurnOrderIndex = 0;

		activegsUnit = turnOrderList[curTurnOrderIndex];

		return isGameOver;
	}

	public void KillUnit(GameStateUnit gs) {
		gs.state = GS_EMPTY;
		gs.occupier = null;
		int tmpIndex = turnOrderList.IndexOf(gs);
		
		if(tmpIndex < curTurnOrderIndex)
			curTurnOrderIndex--;
		
		turnOrderList.RemoveAt(tmpIndex);
	}

	int IsGameOver() {
		bool foundPlayer = false;
		bool foundAI = false;

		foreach(GameStateUnit gs in turnOrderList) {
			if(gs.state == AIGameFlow.GS_PLAYER)
				foundPlayer = true;
			else if(gs.state == AIGameFlow.GS_AI)
				foundAI = true;
		}

		//Continue
		if(foundPlayer && foundAI)
			return -1;

		//If found player the AI lost
		if(foundPlayer)
			return 0;

		//Else the AI won
		if(foundAI)
			return 1;

		//Else the compiler complains
		return -99;
	}

	#endregion

	#region Helpers/Misc 

	//For resetting the MCTS - also resetting the turn order list here
	public GameStateUnit[,] GetCopyOfGameState() {

		GameStateUnit[,] rArray = new GameStateUnit[height, width];

		for(int i = 0; i < height; i++) {
			for(int j = 0; j < width; j++) {
				rArray[i,j] = new GameStateUnit(gameState[i,j]);
			}
		}

		//Resetting the turnOrderList
		curTurnOrderIndex = GameFlow.Instance.CurTurnIndex - 1; //I need to subtract one because I increment right away
		turnOrderList = new List<GameStateUnit>();
		for(int i = 0; i < turnOrderArray.Length; i++) {
			turnOrderList.Add(rArray[turnOrderArray[i].h, turnOrderArray[i].w]);
		}

		//Start the process
		StartNextTurn();

		return rArray;
	}

	public static void ClearGameState(GameStateUnit[,] gs) {
		for(int i = 0; i < gs.GetLength(0); i++) {
			for(int j = 0; j < gs.GetLength(1); j++) {
				if(gs[i,j].state == GS_MOVE)
					gs[i,j].state = GS_EMPTY;
			}
		}
	}

	public static void PrintGameState(GameStateUnit[,] gs) {

		for(int i = 0; i < gs.GetLength(0); i++) {
			string printString = "";

			for(int j = 0; j < gs.GetLength(1); j++) {
				printString += gs[i,j].state + " ";
			}

			Debug.Log(printString);
		}
	}

	public static void PrintUnits(GameStateUnit[,] gs, int h, int w) {
		for(int i = 0; i < h; i++) {
			for(int j = 0; j < w; j++) {
				if(gs[i,j].state == GS_AI || gs[i,j].state == GS_PLAYER) {
					Debug.Log("Health " + gs[i,j].occupier.health);
					Debug.Log("Possible Moves " + gs[i,j].occupier.possibleMoves);
					Debug.Log("Attack range " + gs[i,j].occupier.attackRange);
					Debug.Log("Damage " + gs[i,j].occupier.damage);
					Debug.Log("Possible target " + gs[i,j].occupier.possibleTarget);
				}
			}
		}
	}

	void OnGUI() {
		if(GUI.Button(new Rect(0,0,100,20), "Setup")) {
			SetupGameState();
		}

		if(GUI.Button(new Rect(100,0,100,20), "Start")) {
//			StartProcess();
		}

		if(GUI.Button(new Rect(200,0,100,20), "Print gs")) {
			PrintGameState(gameState);
		}

		if(GUI.Button(new Rect(300,0,100,20), "Units")) {
			PrintUnits(gameState, height, width);
		}
	}

	#endregion
}

public class GameStateUnit {

	public GameStateUnit(char state, int h, int w, AIUnit occupier) {
		this.state = state;
		this.h = h;
		this.w = w;
		this.occupier = occupier;
	}

	public GameStateUnit(GameStateUnit other) {
		this.state = other.state;
		this.h = other.h;
		this.w = other.w;
		if(other.occupier != null)
			this.occupier = other.occupier.Copy(this);
	} 

	public char state;
	public readonly int h, w;
	public AIUnit occupier = null;
}
