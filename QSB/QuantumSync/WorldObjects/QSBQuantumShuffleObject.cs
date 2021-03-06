﻿using OWML.Utils;
using UnityEngine;

namespace QSB.QuantumSync.WorldObjects
{
	internal class QSBQuantumShuffleObject : QSBQuantumObject<QuantumShuffleObject>
	{
		public override void Init(QuantumShuffleObject shuffleObject, int id)
		{
			ObjectId = id;
			AttachedObject = shuffleObject;
			base.Init(shuffleObject, id);
		}

		public void ShuffleObjects(int[] indexArray)
		{
			var shuffledObjects = AttachedObject.GetValue<Transform[]>("_shuffledObjects");
			var localPositions = AttachedObject.GetValue<Vector3[]>("_localPositions");
			for (var i = 0; i < shuffledObjects.Length; i++)
			{
				shuffledObjects[i].localPosition = localPositions[indexArray[i]];
			}
		}
	}
}