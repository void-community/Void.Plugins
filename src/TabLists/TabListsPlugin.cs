using Microsoft.Extensions.DependencyInjection;
using TabLists.Minecraft.PlayerInfo.Actions;
using TabLists.Protocol.Packets.Clientbound;
using TabLists.Services;
using Void.Data.Api.Minecraft;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Binary;
using Void.Minecraft.Players.Extensions;
using Void.Minecraft.Profiles;
using Void.Proxy.Api.Events;
using Void.Proxy.Api.Events.Authentication;
using Void.Proxy.Api.Events.Network;
using Void.Proxy.Api.Events.Proxy;
using Void.Proxy.Api.Network;
using Void.Proxy.Api.Plugins;
using Void.Proxy.Api.Plugins.Dependencies;

namespace TabLists;

public class TabListsPlugin(IDependencyService dependencyService) : IApiPlugin
{
  public string Name => nameof(TabListsPlugin);

  public Dictionary<Uuid, GameProfile> Profiles { get; } = new()
  {
    [Uuid.Parse("2230efb612124348813c101f2ef5771a")] = new GameProfile( "Ircmaan", Uuid.Parse("2230efb612124348813c101f2ef5771a"), [new Property("textures", "ewogICJ0aW1lc3RhbXAiIDogMTc0NjU1MjYxMjE5OCwKICAicHJvZmlsZUlkIiA6ICIyMjMwZWZiNjEyMTI0MzQ4ODEzYzEwMWYyZWY1NzcxYSIsCiAgInByb2ZpbGVOYW1lIiA6ICJJcmNtYWFuIiwKICAic2lnbmF0dXJlUmVxdWlyZWQiIDogdHJ1ZSwKICAidGV4dHVyZXMiIDogewogICAgIlNLSU4iIDogewogICAgICAidXJsIiA6ICJodHRwOi8vdGV4dHVyZXMubWluZWNyYWZ0Lm5ldC90ZXh0dXJlL2QxZDUxMDUwNTZlOGE0MmFkODZkOTgwOWM5Mjg2NTE3NzcwYjdkMWZiNjM1ZDJjZmVmODVmOTVkYTA4NTVmMyIKICAgIH0KICB9Cn0=", true, "nBFQ0mtATw7TxvDHIkhAX5h2p8f/Wwg6aiXmS2lqm2un4Zd2e9s4LHUE2ndnbrZOEinX0QZbSdY2Fkpm9vFNz4b+lp8bHUZECHyoQ/RbVBMMy6Jp8DqEC7wWxkRdedXdndhhwjD9w2ZdyDRPVIJkT+xGugx4MjpSXrYL8fp/N3KHlwpHvXwWpVEpPNb8s7nuk4UnWY1ThHFbT+/CFUcRnUvmd8Q0q/nPJTqRhi5B5eqGLdB1e/QVj5XDPT17rFfO13KxAde2hhOgjekMHP4KaclW2HKJ+0ExxJ0Ef5AYolsVUiVqrkrUa6crqvaq82bnK3WUi0uASdqW6SIX71l/bnfymwfieg/SEvcJMWojiSupYVfTt4/kXJYGrhCQFTNYCHtf/a1i2ynk48vsXeWTSSnh0d4c2mW9QdhPxS7AJCwSdHXOClG080q7NWBgyFCQ5EPAutJZMOAeq8bp2knFay+MhQkRAVhMdKOrCs7uD7qVO1vIc24QEkaASUw80qRbW1n9JsSpV8sSGmKVaAYcmwanSZuJ5a8gtuxMd1jPmNFiNt88o8vCl/jkPNAjzP/G1E9EgxOI18gTVV1VSp/iSwI6yJvPGQhRa4eiLTCeqRjIEf9buQvAxLzQLkVSF2KAjpyp92RFGf5TuL9F1ONC6N2rt7ipKSnFYQ91agX7IOQ=")]),
    [Uuid.Parse("f680df9bac5c4d3f9bac75bc0e316afa")] = new GameProfile( "Admin", Uuid.Parse("f680df9bac5c4d3f9bac75bc0e316afa"), [new Property("textures", "ewogICJ0aW1lc3RhbXAiIDogMTc0NjU1MzIxMDM3OCwKICAicHJvZmlsZUlkIiA6ICJmNjgwZGY5YmFjNWM0ZDNmOWJhYzc1YmMwZTMxNmFmYSIsCiAgInByb2ZpbGVOYW1lIiA6ICJBZG1pbiIsCiAgInNpZ25hdHVyZVJlcXVpcmVkIiA6IHRydWUsCiAgInRleHR1cmVzIiA6IHsKICAgICJTS0lOIiA6IHsKICAgICAgInVybCIgOiAiaHR0cDovL3RleHR1cmVzLm1pbmVjcmFmdC5uZXQvdGV4dHVyZS9lZWU1MjI2MTEwMDVhY2YyNTZkYmQxNTJlOTkyYzYwYzBiYjc5NzhjYjBmMzEyNzgwNzcwMGU0NzhhZDk3NjY0IiwKICAgICAgIm1ldGFkYXRhIiA6IHsKICAgICAgICAibW9kZWwiIDogInNsaW0iCiAgICAgIH0KICAgIH0KICB9Cn0=", true, "vbROxJlbAZK1VhUJtO2sYLkOlFZnlzFgYyqeKQynRF+FGyD3nrvurZGGSnIBlq7c8niIH0nROSe6fi0S6HHVGYaiwjgHcXxOmrmoEEBkIPy2XrIo5S0m8AYgr9X8H2iDVhys6U3RfuLtoih/DFVNNYwDTTgtFbXwnNWpGabeNliKkOJv1UvLaSIP6376+iB0U/Ji4ZFakpL4KPYB1yLWG7hOWZsS95thUI4VM3oMZp+rCirFxPCsQfAUx9OL3/KWzcEkgTh6O7wHe8W43RL98L7DF+C14pNFFrt+Dqs381/1Bv3y7B8WTJXZY5MlBuaaxVhAnvKbFAC0YMivwTFhk8A/4FDbCcX2fGiHN5eEAzKcs1pOkHl7xp6m3alZxgIzX7jeZPBnb3ICldSGvp0NdX3xon2UGBV7l4BaLEBGnuuaMcTF+Ldifsakp+6SB/IlC0jvOaBjexecLrcggebdISgyU0fDvmB1VGNpJrQy2M/GyGjuCSbEIoAVQ2RIeG+9XwD+8ZIP3TJCmJC6u2ERyo2LIenyyeE19jUYdbrTHPpIpyaSgQB/e7ArZMB1GaaWr41LkwQCWXNreOVHrTQ0F5TpAkpuR+bwrGo3GrPtlAmUHhrCPjC3uALy4yiPmL41+hShSqvF8xEgSAS14rD2WghCCAOFJi/4H9VYIHZXVs0=")]),
    [Uuid.Parse("069a79f444e94726a5befca90e38aaf5")] = new GameProfile( "Notch", Uuid.Parse("069a79f444e94726a5befca90e38aaf5"), [new Property("textures", "ewogICJ0aW1lc3RhbXAiIDogMTc0NjU1MzM1MDg0MywKICAicHJvZmlsZUlkIiA6ICIwNjlhNzlmNDQ0ZTk0NzI2YTViZWZjYTkwZTM4YWFmNSIsCiAgInByb2ZpbGVOYW1lIiA6ICJOb3RjaCIsCiAgInNpZ25hdHVyZVJlcXVpcmVkIiA6IHRydWUsCiAgInRleHR1cmVzIiA6IHsKICAgICJTS0lOIiA6IHsKICAgICAgInVybCIgOiAiaHR0cDovL3RleHR1cmVzLm1pbmVjcmFmdC5uZXQvdGV4dHVyZS8yOTIwMDlhNDkyNWI1OGYwMmM3N2RhZGMzZWNlZjA3ZWE0Yzc0NzJmNjRlMGZkYzMyY2U1NTIyNDg5MzYyNjgwIgogICAgfQogIH0KfQ==", true, "H0PSZi5e009sVGMHibwMxR2/TaSvuji5ZISg2fX0csncA+clBa1UlWfVWLd6zr4LXIqTj1yWGcZRedr42JHuxP4iOR/l7kOhrNr1d/wQtpQf110bgh5bDs67Q2QGDIYiYijZrveFdL43OYc7YhZAHtunU8XPenZvygWmM5MeeK4M/Vz9ZWva2bn1bpMI/LCR2D36V8Ah8ErGsttqSNFKseOPX25IMcZA/F/xP2qm4sBbAZsrGtB01Q6aBkDVp0f0GZdDa927jUyxn9AiqiAHdIKpMBvqOaSMnzJnGhmaG9UgYAfQoXNW4zXLQLwQW3XAvZbr2XscR9pNOG+vG2tajKzACH1pFc5IWwX4JFk/Ltuxy64uTGJldFQStdLsDGWlEKmjgieMHQpZ9pEbwWmulOsob+VF9kjDdUu5w0cNp62FgtOVuoTAHrk7Lo83rSrf4oEAKzicybs/7Tge9vufm3VqUVMLJZsXIZcjVJfcar/z2MDnwUwHuvlOX3vpR3G+WMsUBCzJW6ZEnVhHfVwScnl/lXIgEvZgaMLz1v17lK0qiBreU77hqjpFtfTsp+UWtuR5zbzCFqntMuIB43Uyw2C14L/yptc9zSh3Y/AEcd5W9N7HyQu2mMhbuATbysikFDRz8UpIcGEBxpzuPu05My6xxayIZqe+RPfgNTJnriI=")]),
  };

  [Subscribe]
  private void OnProxyStarting(ProxyStartingEvent @event)
  {
    dependencyService.Register(services => { services.AddScoped<PacketService>(); });
  }

  [Subscribe]
  private void OnAuthenticationFinished(AuthenticationFinishedEvent @event)
  {
    var profile = @event.Player.Profile;
    if (profile is null)
      return;

    Profiles[profile.Id] = profile;
  }

  [Subscribe]
  private async ValueTask OnPlayerInfoPacket(MessageReceivedEvent @event, CancellationToken cancellationToken)
  {
    if (@event.Player.ProtocolVersion >= ProtocolVersion.MINECRAFT_1_20_5)
    {
      var playPacketId = @event.Player.ProtocolVersion > ProtocolVersion.MINECRAFT_1_21
        ? MinecraftPacketRegistry.GetId(@event.Player.ProtocolVersion, Phase.Play, Direction.Clientbound, "minecraft:login")
        : 0x2B;

      if (@event.From is Side.Server && @event.Message is IMinecraftBinaryMessage binaryPacket && binaryPacket.Id == playPacketId)
      {
        @event.Cancel();

        var position = binaryPacket.Stream.Position;
        binaryPacket.Stream.Position = binaryPacket.Stream.Length - 1; // Enforces secure chat
        binaryPacket.Stream.WriteByte(1);
        binaryPacket.Stream.Position = position;

        await @event.Player.SendPacketAsync(binaryPacket, cancellationToken);
      }
    }

    if (@event.Message is not PlayerInfoUpdateClientboundPacket playerInfoUpdateClientboundPacket)
      return;

    // @event.Cancel();

    var packet = new PlayerInfoUpdateClientboundPacket
    {
      PlayerInfoActions = Profiles.ToDictionary(i => i.Key, i => new List<IPlayerInfoAction> {
        new AddPlayerAction(i.Value),
        new UpdateGameModeAction(0),
        new UpdateListedAction(true)
      })
    };

    await @event.Player.SendPacketAsync(packet, cancellationToken);
  }
}
