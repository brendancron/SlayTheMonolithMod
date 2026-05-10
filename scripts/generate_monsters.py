"""One-off generator for batch-creating monster + encounter + scene files.

Reads the `monsters` table below as the source of truth and emits, per row:
  SlayTheMonolithModCode/Monsters/{Cls}.cs
  SlayTheMonolithModCode/Encounters/{Cls}Normal.cs
  SlayTheMonolithMod/scenes/creature_visuals/{lower}.tscn

The .tscn template assumes a 200x200 source sprite and lays out bounds + markers accordingly.
"""
import os
import re

monsters = [
    # (ClassName, DisplayName, MoveId, MoveLabel, MinHp, MaxHp, Dmg, DamageSfxType)
    ('Aberration',  'Aberration',  'WARP_MOVE',     'Warp',          26, 32, 7,  'Magic'),
    ('Ballet',      'Ballet',      'PIROUETTE_MOVE','Pirouette',     22, 28, 6,  'Magic'),
    ('Benisseur',   'Benisseur',   'BLESS_MOVE',    'Bless',         24, 30, 6,  'Magic'),
    ('Bouchelier',  'Bouchelier',  'BASH_MOVE',     'Bash',          34, 40, 8,  'Armor'),
    ('Braseleur',   'Braseleur',   'BRAZIER_MOVE',  'Brazier',       28, 34, 8,  'Magic'),
    ('Bruler',      'Bruler',      'IGNITE_MOVE',   'Ignite',        24, 30, 7,  'Magic'),
    ('Chevaliere',  'Chevaliere',  'CHARGE_MOVE',   'Charge',        32, 38, 9,  'Armor'),
    ('Clair',       'Clair',       'GLEAM_MOVE',    'Gleam',         26, 32, 7,  'Magic'),
    ('Danseuses',   'Danseuses',   'WALTZ_MOVE',    'Waltz',         24, 30, 6,  'Magic'),
    ('Demineur',    'Demineur',    'MINE_MOVE',     'Mine',          28, 34, 8,  'Armor'),
    ('GrosseTete',  'Grosse Tete', 'HEADBUTT_MOVE', 'Headbutt',      36, 44, 10, 'Stone'),
    ('Hexga',       'Hexga',       'HEX_MOVE',      'Hex',           26, 32, 7,  'Magic'),
    ('Mime',        'Mime',        'MIRROR_MOVE',   'Mirror',        20, 26, 6,  'Magic'),
    ('Noir',        'Noir',        'SHADOW_MOVE',   'Shadow',        28, 34, 8,  'Magic'),
    ('Obscur',      'Obscur',      'OBSCURE_MOVE',  'Obscure',       28, 34, 8,  'Magic'),
    ('Pelerin',     'Pelerin',     'PILGRIM_STRIKE','Pilgrim Strike',26, 32, 7,  'Armor'),
    ('Petank',      'Petank',      'TOSS_MOVE',     'Toss',          30, 36, 8,  'Stone'),
    ('Ramasseur',   'Ramasseur',   'GATHER_MOVE',   'Gather',        24, 30, 7,  'Armor'),
    ('Rocher',      'Rocher',      'ROCK_MOVE',     'Rock Throw',    34, 40, 9,  'Stone'),
    ('Stalact',     'Stalact',     'DROP_MOVE',     'Drop',          30, 36, 9,  'Stone'),
    ('Troubadour',  'Troubadour',  'BALLAD_MOVE',   'Ballad',        22, 28, 6,  'Magic'),
    ('Veilleur',    'Veilleur',    'VIGIL_MOVE',    'Vigil',         28, 34, 7,  'Armor'),
    ('Volester',    'Volester',    'SHARD_THROW',   'Shard Throw',   30, 36, 8,  'Stone'),
]

repo = r'C:\Users\Brendan\source\repos\SlayTheMonolithMod'
mons_dir = os.path.join(repo, 'SlayTheMonolithModCode', 'Monsters')
enc_dir = os.path.join(repo, 'SlayTheMonolithModCode', 'Encounters')
scn_dir = os.path.join(repo, 'SlayTheMonolithMod', 'scenes', 'creature_visuals')

MON_TPL = """using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

public sealed class {Cls} : CustomMonsterModel, ILocalizationProvider
{{
    private const string MoveIdConst = "{MoveId}";

    public override int MinInitialHp => {MinHp};
    public override int MaxInitialHp => {MaxHp};

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/{lower}.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is {{ }} path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.{Sfx};

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "{Display}",
        MoveTitles: new[] {{ (MoveIdConst, "{MoveLabel}") }});

    private int MoveDamage => {Dmg};

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {{
        var move = new MoveState(MoveIdConst, DoMove, new SingleAttackIntent(MoveDamage));
        move.FollowUpState = move;
        return new MonsterMoveStateMachine(new List<MonsterState> {{ move }}, move);
    }}

    private async Task DoMove(IReadOnlyList<Creature> targets)
    {{
        await DamageCmd.Attack(MoveDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }}
}}
"""

ENC_TPL = """using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

public sealed class {Cls}Normal : CustomEncounterModel
{{
    public {Cls}Normal() : base(RoomType.Monster) {{ }}

    public override bool IsValidForAct(ActModel act) => act is TheContinent;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {{
        ModelDb.Monster<{Cls}>(),
    }};

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {{
            (ModelDb.Monster<{Cls}>().ToMutable(), null),
        }};
}}
"""

# 200x200 sprite: offset y=-100 puts feet at floor (y=0), top at -200.
SCN_TPL = """[gd_scene load_steps=2 format=3 uid="uid://{uid}"]

[ext_resource type="Texture2D" path="res://SlayTheMonolithMod/images/{lower}.png" id="1_{short}"]

[node name="{Cls}" type="Node2D"]

[node name="Visuals" type="Sprite2D" parent="."]
unique_name_in_owner = true
texture = ExtResource("1_{short}")
offset = Vector2(0, -100)

[node name="Bounds" type="Control" parent="."]
unique_name_in_owner = true
custom_minimum_size = Vector2(100, 200)
offset_left = -50.0
offset_top = -200.0
offset_right = 50.0
offset_bottom = 0.0

[node name="IntentPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = Vector2(0, -240)

[node name="CenterPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = Vector2(0, -100)
"""

written = 0
for (cls, disp, move_id, move_lbl, min_hp, max_hp, dmg, sfx) in monsters:
    lower = cls.lower()
    short = re.sub(r'[^a-z0-9]', '', lower)[:5].ljust(5, 'x')
    uid = ('b1' + lower + '0' * 16)[:18]
    mon_src = MON_TPL.format(
        Cls=cls, lower=lower, MinHp=min_hp, MaxHp=max_hp, Dmg=dmg, Sfx=sfx,
        Display=disp, MoveId=move_id, MoveLabel=move_lbl,
    )
    enc_src = ENC_TPL.format(Cls=cls)
    scn_src = SCN_TPL.format(Cls=cls, lower=lower, short=short, uid=uid)
    with open(os.path.join(mons_dir, f'{cls}.cs'), 'w', encoding='utf-8') as f:
        f.write(mon_src)
    with open(os.path.join(enc_dir, f'{cls}Normal.cs'), 'w', encoding='utf-8') as f:
        f.write(enc_src)
    with open(os.path.join(scn_dir, f'{lower}.tscn'), 'w', encoding='utf-8') as f:
        f.write(scn_src)
    written += 1
print(f'Generated {written} (.cs monster + .cs encounter + .tscn) triples')
