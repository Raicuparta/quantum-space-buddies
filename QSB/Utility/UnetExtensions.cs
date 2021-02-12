using QSB.Player;
using QSB.TransformSync;
using QuantumUNET;

namespace QSB.Utility
{
	public static class UnetExtensions
	{
		public static PlayerInfo GetPlayer(this QSBNetworkConnection connection)
		{
			var go = connection.PlayerControllers[0].Gameobject;
			var controller = go.GetComponent<PlayerTransformSync>();
			return QSBPlayerManager.GetPlayer(controller.NetId.Value);
		}
	}
}