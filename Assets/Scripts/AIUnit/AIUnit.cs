﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class AIUnit {

	public int possibleMoves;
	public int attackRange;
	public int health;
	public int damage;
	public AttackDirection attackDirection;

	//If this for instance is a player unit it can attack AI and vice versa
	public char possibleTarget;

	public GameStateUnit curgsUnit;

	#region Methods to be overridden
	
	public abstract void Attack(GameStateUnit moveTo, GameStateUnit attack);
	public abstract List<MCTSNode> GetPossibleMoves(MCTSNode parent);
	public abstract int GetPossibleMovesDefaultPolicy(List<MCTSNode> defaultList);
	public abstract AIUnit Copy(GameStateUnit unit);

	#endregion
	
	public void Move (GameStateUnit to) {

		//If it's the same place anyways
		if(to.h == curgsUnit.h && to.w == curgsUnit.w)
			return;

		to.state = curgsUnit.state;
		to.occupier = curgsUnit.occupier;

		curgsUnit.state = AIGameFlow.GS_EMPTY;
		curgsUnit.occupier = null;

		curgsUnit = to;
	}

	public void TakeDamage (int damage) {
		health -= damage;

		if(health <= 0) {
			AIGameFlow.Instance.KillUnit(this);
		}
	}
}
