using QSB.Utility;
using System.Runtime.InteropServices;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB.TransformSync
{
    class NomaiOrbTransformSync : NetworkBehaviour
    {
        [SyncVar(hook ="OnChange")]
        private int _Index;

        void OnChange(int value)
        {
            DebugLog.DebugWrite("onchange " + value);
        }

        public int Index
        {
            get
            {
                DebugLog.DebugWrite("GET TEST, VALUE OF " + _Index);
                return _Index;
            }
            [param: In]
            set
            {
                SetSyncVar(value, ref _Index, 1);
                DebugLog.DebugWrite($"SET TEST OF {GetComponent<NetworkIdentity>()?.netId.Value} TO {value}");
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool forceAll)
        {
            DebugLog.DebugWrite("ONSERIALISE " + GetComponent<NetworkIdentity>()?.netId.Value);
            bool flag = base.OnSerialize(writer, forceAll);
            if (forceAll)
            {
                DebugLog.DebugWrite("* Forceall, write string");
                GeneratedNetworkCode.Write(writer, Index);
                return true;
            }
            else
            {
                bool flag2 = false;
                if ((syncVarDirtyBits & 1U) > 0U)
                {
                    if (!flag2)
                    {
                        DebugLog.DebugWrite("* writing dirty bits");
                        writer.WritePackedUInt32(syncVarDirtyBits);
                        flag2 = true;
                    }
                    DebugLog.DebugWrite("* write string");
                    GeneratedNetworkCode.Write(writer, Index);
                }
                if (!flag2)
                {
                    DebugLog.DebugWrite("* writing dirty bits");
                    writer.WritePackedUInt32(syncVarDirtyBits);
                }
                return (flag2 || flag);
            }
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            DebugLog.DebugWrite("ONDESERIALISE " + netId.Value);
            base.OnDeserialize(reader, initialState);
            if (initialState)
            {
                Index = GeneratedNetworkCode.Read<int>(reader);
            }
            else
            {
                int num = (int)reader.ReadPackedUInt32();
                if ((num & 1) != 0)
                {
                    Index = GeneratedNetworkCode.Read<int>(reader);
                }
            }
        }

        public override void OnStartClient()
        {
            DebugLog.DebugWrite("ORB START WITH INDEX " + Index);
        }
    }
}
