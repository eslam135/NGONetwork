using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
public enum TeamID
{
    Red = 0,
    Blue = 1,
}

public struct PlayerData : INetworkSerializable
{
    public FixedString32Bytes PlayerName;
    public TeamID TeamID;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            var fastBufferWriter = serializer.GetFastBufferWriter();
            fastBufferWriter.WriteValueSafe(PlayerName);
            fastBufferWriter.WriteValueSafe(TeamID);
        }
        else if (serializer.IsReader)
        {
            var fastBufferReader = serializer.GetFastBufferReader();
            fastBufferReader.ReadValueSafe(out PlayerName);
            fastBufferReader.ReadValueSafe(out TeamID);

        }
    }

    public override string ToString()
    {
        return $"PlayerName: {PlayerName} , TeamId : {TeamID}";
    }
}
