/*
 * All right reserved to CrystallPunk.
 *
 * This file is sublicensed under Custom License Agreement for Stalker14 project (https://github.com/stalker14-project/stalker14) only
 *
 * See LICENSE.TXT file in the project root for full license information.
 * Copyright (c) 2024 TheShuEd (Github)
 */

using System.Numerics;
using Content.Server.Decals;
using Content.Server.GameTicking;
using Content.Server.Parallax;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Stalker.BiomesSpawner;

public sealed class STBiomeSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private int _seed = 27;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnRoundStartAttempt);
        SubscribeLocalEvent<STBiomeSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnRoundStartAttempt(RoundStartAttemptEvent ev)
    {
        _seed = _random.Next(100000);
    }

    private void OnMapInit(Entity<STBiomeSpawnerComponent> spawner, ref MapInitEvent args)
    {
        SpawnBiome(spawner);
        QueueDel(spawner);
    }

    private void SpawnBiome(Entity<STBiomeSpawnerComponent> spawner)
    {
        var biome = _proto.Index(spawner.Comp.Biome);
        var parent = _transform.GetParent(spawner);

        if (parent == null)
            return;

        var gridUid = parent.Owner;

        if (!TryComp<MapGridComponent>(gridUid, out var map))
            return;

        var v2i = _transform.GetGridOrMapTilePosition(spawner);
        if (!_biome.TryGetTile(v2i, biome.Layers, _seed, map, out var tile))
            return;

        // Set new tile
        _maps.SetTile(gridUid, map, v2i, tile.Value);

        // Remove old decals
        var oldDecals = _decals.GetDecalsInRange(gridUid, v2i + new Vector2(0.5f, 0.5f));
        foreach (var (id, _) in oldDecals)
        {
            _decals.RemoveDecal(gridUid, id);
        }

        //Add decals
        if (_biome.TryGetDecals(v2i, biome.Layers, _seed, map, out var decals))
        {
            foreach (var decal in decals)
            {
                _decals.TryAddDecal(decal.ID, new EntityCoordinates(gridUid, decal.Position), out _);
            }
        }

        //TODO maybe need remove anchored entities here

        //Add entities

        if (_biome.TryGetEntity(v2i, biome.Layers, tile.Value, _seed, map, out var entityProto))
        {
            var ent = _entManager.SpawnEntity(entityProto,
                new EntityCoordinates(gridUid, v2i + map.TileSizeHalfVector));
        }
    }
}
