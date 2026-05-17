using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Shiptest.GameTicking;

[AdminCommand(AdminFlags.Round)]
public sealed class RoundPreparationSkipCommand : IConsoleCommand
{
    public string Command => "roundprep_skip";
    public string Description => "Immediately ends the round preparation phase.";
    public string Help => "No arguments required.";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError("This command does not take arguments.");
            return;
        }

        var preparation = EntitySystem.Get<RoundPreparationSystem>();
        if (!preparation.TrySkipPreparation())
        {
            shell.WriteLine("Round preparation is not active.");
            return;
        }

        shell.WriteLine("Round preparation skipped.");
    }
}
