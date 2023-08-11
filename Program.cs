using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;

internal class Program
{
    private static void Main(string[] args)
    {
        var listener = new ServerListener<MyPlayer, VipHammerChallengeServer>();
        listener.Start(29294);

        Thread.Sleep(-1);
    }
}

internal class MyPlayer : Player<MyPlayer>
{
    // ToDo not used..
    public bool IsVIP;
}

internal class VipHammerChallengeServer : GameServer<MyPlayer>
{
    private const int NeededPlayers = 6;
    private MyPlayer _vip;


    public override async Task OnRoundStarted()
    {
        StartChallenge();
    }

    public override async Task OnRoundEnded()
    {
        if (_vip != null) AnnounceLong($"{_vip.Name} and their team have one the challenge!");
        _vip = null;
    }
    
    public override async Task OnPlayerConnected(MyPlayer player)
    {
        if (CurrentPlayers < NeededPlayers)
            SayToChat($"{player.Name} has joined the server. {CurrentPlayers}/{NeededPlayers} of required players.");
    }

    public override async Task OnPlayerDisconnected(MyPlayer player)
    {
        if (RoundSettings.State == GameState.Playing && _vip == player)
        {
            AnnounceLong($"VIP {player.Name} has left. Choosing a new one.");
            StartChallenge();
        }
    }

    private void StartChallenge()
    {
        var random = new Random();
        var players = AllPlayers.ToList();
        // We already have a vip... try to redraw in the same team.
        if (_vip != null)
        {
            var tries = 0;
            while (true)
            {
                var j = random.Next(players.Count);
                if (players[j].Team != _vip.Team)
                {
                    tries++;
                    continue;
                }

                if (tries >= 10)
                {
                    AnnounceShort("Restarting round. Unable to determine a new VIP");
                    RoundSettings.SecondsLeft = 5;
                    return;
                }

                _vip = players[j];
            }
        }

        _vip = players[random.Next(players.Count)];
        // First vip of the round. kill everyone to reconfigure their loadouts.
        foreach (var player in AllPlayers)
        {
            player.Kill();
            // Todo also respawn them automatically
        }

        AnnounceLong($"{_vip.Name} is the VIP!");
    }

    public override async Task OnAPlayerKilledAnotherPlayer(OnPlayerKillArguments<MyPlayer> args)
    {
        if (args.Victim == _vip)
        {
            AnnounceLong($"{args.Killer.Name} has killed the VIP ${_vip.Name} and wins the challenge!");
            RoundSettings.SecondsLeft = 10;
        }
    }

    public override async Task<OnPlayerSpawnArguments> OnPlayerSpawning(MyPlayer player, OnPlayerSpawnArguments request)
    {
        if (_vip == null)
            // no VIP do not do anything yet. (The challenge kills everyone anyway when starting :) )
            return request;

        if (player.Team != _vip.Team)
        {
            // Only give them a hammer
            request.Loadout.PrimaryWeapon = default;
            request.Loadout.SecondaryWeapon = default;
            request.Loadout.LightGadget = null;
            request.Loadout.HeavyGadget = Gadgets.SledgeHammer;
            request.Loadout.Throwable = null;
        }

        return request;
    }

    public override async Task OnPlayerSpawned(MyPlayer player)
    {
        if (player == _vip)
        {
            player.SetRunningSpeedMultiplier(2f);
            player.SetReceiveDamageMultiplier(0.5f);
            player.SetGiveDamageMultiplier(4f);
        }
        else if (_vip.Team == player.Team)
        {
            player.SetRunningSpeedMultiplier(0.6f);
            player.SetReceiveDamageMultiplier(2f);
            player.SetGiveDamageMultiplier(0.25f);
            player.Message($"The VIP {player.Name} is in your Team. Protect them! You may use all weapons.");
        }
        else
        {
            player.Message($"{player.Name} in the enemy team is the VIP. Kill them!");
        }
    }

    public override async Task OnConnected()
    {
        await Console.Out.WriteLineAsync("Current state: " + RoundSettings.State);
        RoundSettings.PlayersToStart = NeededPlayers;
    }

    public override async Task OnGameStateChanged(GameState oldState, GameState newState)
    {
        await Console.Out.WriteLineAsync("State changed to -> " + newState);
    }
}