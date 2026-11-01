using Menus.Protocol.Packets.Clientbound;
using Menus.Protocol.Packets.Serverbound;
using Microsoft.Extensions.Logging;
using Void.Minecraft.Events;
using Void.Minecraft.Network;
using Void.Minecraft.Players.Extensions;
using Void.Proxy.Api.Events;
using Void.Proxy.Api.Network;
using Void.Proxy.Api.Network.Channels;
using Void.Proxy.Api.Players.Contexts;

namespace Menus.Services;

internal class PacketService(ILogger<PacketService> logger, IPlayerContext playerContext) : IEventListener
{
  [Subscribe]
  private void OnPhaseChanged(PhaseChangedEvent @event)
  {
    Action<INetworkChannel>? handler = @event.Phase switch
    {
      Phase.Play => @event.Side switch {
        Side.Server => RegisterPlayForServerPackets,
        Side.Client => RegisterPlayForClientPackets,
        _ => null
      },
      _ => null
    };

    handler?.Invoke(@event.Channel);
  }

  private void RegisterPlayForServerPackets(INetworkChannel channel)
  {
    playerContext.Player.RegisterPacket<SetContainerSlotClientboundPacket>(channel, Operation.Read, SetContainerSlotClientboundPacket.Mappings);
    playerContext.Player.RegisterPacket<SetContainerContentClientboundPacket>(channel, Operation.Read, SetContainerContentClientboundPacket.Mappings);
    playerContext.Player.RegisterPacket<SetContainerPropertyClientboundPacket>(channel, Operation.Read, SetContainerPropertyClientboundPacket.Mappings);

    playerContext.Player.RegisterPacket<OpenContainerClientboundPacket>(channel, Operation.Read, OpenContainerClientboundPacket.Mappings);
    playerContext.Player.RegisterPacket<CloseContainerClientboundPacket>(channel, Operation.Read, CloseContainerClientboundPacket.Mappings);
    playerContext.Player.RegisterPacket<CloseContainerServerboundPacket>(channel, Operation.Write, CloseContainerServerboundPacket.Mappings);

    playerContext.Player.RegisterPacket<ClickContainerServerboundPacket>(channel, Operation.Write, ClickContainerServerboundPacket.Mappings);

    if (playerContext.Player.ProtocolVersion >= ProtocolVersion.MINECRAFT_1_21_2)
      playerContext.Player.RegisterPacket<SetPlayerInventorySlotClientboundPacket>(channel, Operation.Read, SetPlayerInventorySlotClientboundPacket.Mappings);

    logger.LogInformation("Registered play packets to server for {Player}", playerContext.Player);
  }

  private void RegisterPlayForClientPackets(INetworkChannel channel)
  {
    playerContext.Player.RegisterPacket<SetContainerSlotClientboundPacket>(channel, Operation.Write, SetContainerSlotClientboundPacket.Mappings);
    playerContext.Player.RegisterPacket<SetContainerContentClientboundPacket>(channel, Operation.Write, SetContainerContentClientboundPacket.Mappings);
    playerContext.Player.RegisterPacket<SetContainerPropertyClientboundPacket>(channel, Operation.Write, SetContainerPropertyClientboundPacket.Mappings);

    playerContext.Player.RegisterPacket<OpenContainerClientboundPacket>(channel, Operation.Write, OpenContainerClientboundPacket.Mappings);
    playerContext.Player.RegisterPacket<CloseContainerClientboundPacket>(channel, Operation.Write, CloseContainerClientboundPacket.Mappings);
    playerContext.Player.RegisterPacket<CloseContainerServerboundPacket>(channel, Operation.Read, CloseContainerServerboundPacket.Mappings);

    playerContext.Player.RegisterPacket<ClickContainerServerboundPacket>(channel, Operation.Read, ClickContainerServerboundPacket.Mappings);
    playerContext.Player.RegisterPacket<SetCreativeModeSlotServerboundPacket>(channel, Operation.Read, SetCreativeModeSlotServerboundPacket.Mappings);
    playerContext.Player.RegisterPacket<PlayerActionServerboundPacket>(channel, Operation.Read, PlayerActionServerboundPacket.Mappings);
    playerContext.Player.RegisterPacket<UseItemOnServerboundPacket>(channel, Operation.Read, UseItemOnServerboundPacket.Mappings);

    if (playerContext.Player.ProtocolVersion >= ProtocolVersion.MINECRAFT_1_9)
      playerContext.Player.RegisterPacket<UseItemServerboundPacket>(channel, Operation.Read, UseItemServerboundPacket.Mappings);

    if (playerContext.Player.ProtocolVersion >= ProtocolVersion.MINECRAFT_1_12_1)
      playerContext.Player.RegisterPacket<PlaceRecipeServerboundPacket>(channel, Operation.Read, PlaceRecipeServerboundPacket.Mappings);

    if (playerContext.Player.ProtocolVersion >= ProtocolVersion.MINECRAFT_1_21_4)
    {
      playerContext.Player.RegisterPacket<PickItemFromBlockServerboundPacket>(channel, Operation.Read, PickItemFromBlockServerboundPacket.Mappings);
      playerContext.Player.RegisterPacket<PickItemFromEntityServerboundPacket>(channel, Operation.Read, PickItemFromEntityServerboundPacket.Mappings);
    }
    else if (playerContext.Player.ProtocolVersion >= ProtocolVersion.MINECRAFT_1_13)
    {
      playerContext.Player.RegisterPacket<PickItemServerboundPacket>(channel, Operation.Read, PickItemServerboundPacket.Mappings);
    }

    logger.LogInformation("Registered play packets to client for {Player}", playerContext.Player);
  }
}
