using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Shared._Shiptest.ShipSpawn;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._Shiptest.ShipSpawn;

public sealed class ShipSpawnWindow : DefaultWindow
{
    private readonly IPrototypeManager _proto;
    private readonly Action<ProtoId<PlayerShipFactionPrototype>, ProtoId<PlayerShipBlueprintPrototype>, bool, string?> _onConfirm;
    private readonly IReadOnlyCollection<string> _unavailableBlueprints;
    private readonly OptionButton _factionOption = new();
    private readonly OptionButton _shipOption = new();
    private readonly CheckBox _closedShipCheck = new();
    private readonly LineEdit _passwordInput = new();
    private readonly List<PlayerShipFactionPrototype> _factions = new();
    private readonly List<string> _shipsShownForFaction = new();

    public ShipSpawnWindow(
        IPrototypeManager proto,
        Action<ProtoId<PlayerShipFactionPrototype>, ProtoId<PlayerShipBlueprintPrototype>, bool, string?> onConfirm,
        IReadOnlyCollection<string> unavailableBlueprints)
    {
        _proto = proto;
        _onConfirm = onConfirm;
        _unavailableBlueprints = unavailableBlueprints;

        Title = Loc.GetString("player-ship-spawn-window-title");
        SetSize = new Vector2(460, 340);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(8),
            SeparationOverride = 8
        };

        root.AddChild(new Label { Text = Loc.GetString("player-ship-spawn-faction-label") });
        _factionOption.OnItemSelected += args =>
        {
            _factionOption.SelectId(args.Id);
            RefreshShips();
        };
        root.AddChild(_factionOption);

        root.AddChild(new Label { Text = Loc.GetString("player-ship-spawn-ship-label") });
        _shipOption.OnItemSelected += args => _shipOption.SelectId(args.Id);
        root.AddChild(_shipOption);

        _closedShipCheck.Text = Loc.GetString("player-ship-spawn-closed-check");
        _closedShipCheck.OnToggled += args =>
        {
            _passwordInput.Editable = args.Pressed;
            _passwordInput.ModulateSelfOverride = null;
        };
        root.AddChild(_closedShipCheck);

        root.AddChild(new Label { Text = Loc.GetString("player-ship-spawn-password-label") });
        _passwordInput.PlaceHolder = Loc.GetString("player-ship-spawn-password-placeholder");
        _passwordInput.Editable = false;
        _passwordInput.OnTextChanged += _ =>
        {
            _passwordInput.ModulateSelfOverride = null;
        };
        root.AddChild(_passwordInput);

        var confirm = new Button
        {
            Text = Loc.GetString("player-ship-spawn-confirm"),
            HorizontalAlignment = HAlignment.Center
        };
        confirm.OnPressed += _ =>
        {
            if (_factionOption.SelectedId < 0 || _shipOption.SelectedId < 0)
                return;

            var faction = _factions[_factionOption.SelectedId];
            var shipIdx = _shipOption.GetIdx(_shipOption.SelectedId);
            if (shipIdx < 0 || shipIdx >= _shipsShownForFaction.Count)
                return;

            if (_shipOption.IsItemDisabled(shipIdx))
                return;

            var closedShip = _closedShipCheck.Pressed;
            string? password = null;
            if (closedShip)
            {
                password = _passwordInput.Text.Trim();
                if (password.Length < 3)
                {
                    _passwordInput.ModulateSelfOverride = Color.IndianRed;
                    _passwordInput.GrabKeyboardFocus();
                    return;
                }
            }

            _onConfirm(faction.ID, new ProtoId<PlayerShipBlueprintPrototype>(_shipsShownForFaction[shipIdx]), closedShip, password);
            Close();
        };
        root.AddChild(confirm);

        Contents.AddChild(root);

        PopulateFactions();
        RefreshShips();
    }

    private void PopulateFactions()
    {
        _factionOption.Clear();
        _factions.Clear();
        foreach (var faction in _proto.EnumeratePrototypes<PlayerShipFactionPrototype>().OrderBy(f => f.ID))
        {
            _factions.Add(faction);
            _factionOption.AddItem(Loc.GetString(faction.Name), _factions.Count - 1);
        }

        if (_factions.Count > 0)
            _factionOption.SelectId(0);
    }

    private void RefreshShips()
    {
        _shipOption.Clear();
        _shipsShownForFaction.Clear();
        if (_factionOption.SelectedId < 0 || _factionOption.SelectedId >= _factions.Count)
            return;

        var faction = _factions[_factionOption.SelectedId];
        foreach (var shipId in faction.Ships)
        {
            if (!_proto.TryIndex(new ProtoId<PlayerShipBlueprintPrototype>(shipId), out var ship))
                continue;

            _shipsShownForFaction.Add(shipId);
            var taken = _unavailableBlueprints.Contains(shipId);
            var label = taken
                ? $"{Loc.GetString(ship.Name)} {Loc.GetString("player-ship-spawn-ship-unavailable-suffix")}"
                : Loc.GetString(ship.Name);
            _shipOption.AddItem(label);
            var idx = _shipOption.ItemCount - 1;
            _shipOption.SetItemDisabled(idx, taken);
        }

        for (var i = 0; i < _shipOption.ItemCount; i++)
        {
            if (!_shipOption.IsItemDisabled(i))
            {
                _shipOption.Select(i);
                return;
            }
        }

        if (_shipOption.ItemCount > 0)
            _shipOption.Select(0);
    }
}
