using Unity.Collections;
using Unity.Netcode;

public enum TeamID
{
    Red = 0,
    Blue = 1,
}

public enum PlayerClassID
{
    Tank = 0,
    DPS = 1,
}

public struct PlayerData : INetworkSerializable
{
    public FixedString32Bytes PlayerName;
    public TeamID TeamID;
    public PlayerClassID ClassID;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref TeamID);
        serializer.SerializeValue(ref ClassID);
    }

    public override string ToString()
    {
        return $"PlayerName: {PlayerName}, TeamID: {TeamID}, ClassID: {ClassID}";
    }
}