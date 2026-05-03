using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct PlayerData : INetworkSerializable
{
    public FixedString32Bytes PlayerName;
    public TeamID teamID;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            var fastBufferWriter = serializer.GetFastBufferWriter();
            fastBufferWriter.WriteValueSafe(PlayerName);
            fastBufferWriter.WriteValueSafe(teamID);
        }
        else if (serializer.IsReader)
        {
            var fastBufferReader = serializer.GetFastBufferReader();
            fastBufferReader.ReadValueSafe(out PlayerName);
            fastBufferReader.ReadValueSafe(out teamID);

        }
    }

    public override string ToString()
    {
        return $"PlayerName: {PlayerName} , TeamId : {teamID}";
    }
}
