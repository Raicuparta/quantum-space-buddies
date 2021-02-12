using UnityEngine;

namespace QSB
{
	public delegate void InputEvent();

	public class QSBInputManager : MonoBehaviour
	{
		public static event InputEvent ChertTaunt;
		public static event InputEvent EskerTaunt;
		public static event InputEvent RiebeckTaunt;
		public static event InputEvent GabbroTaunt;
		public static event InputEvent FeldsparTaunt;
		public static event InputEvent ExitTaunt;

		// TODO : finish instruments - disabled for 0.7.0 release
		/*
		public void Update()
		{
			if (Input.GetKey(KeyCode.T))
			{
				// Listed order is from sun to dark bramble
				if (Input.GetKeyDown(KeyCode.Alpha1))
				{
					ChertTaunt?.Invoke();
				}
				else if (Input.GetKeyDown(KeyCode.Alpha2))
				{
					EskerTaunt?.Invoke();
				}
				else if (Input.GetKeyDown(KeyCode.Alpha5))
				{
					RiebeckTaunt?.Invoke();
				}
				else if (Input.GetKeyDown(KeyCode.Alpha4))
				{
					GabbroTaunt?.Invoke();
				}
				else if (Input.GetKeyDown(KeyCode.Alpha3))
				{
					FeldsparTaunt?.Invoke();
				}
			}

			if (OWInput.GetValue(InputLibrary.moveXZ, InputMode.None) != Vector2.zero
				|| OWInput.GetValue(InputLibrary.jump, InputMode.None) != 0f)
			{
				ExitTaunt?.Invoke();
			}
		}
		*/
	}
}