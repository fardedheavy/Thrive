﻿using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Spawns AI cells and other environmental things as the player moves around
/// </summary>
public class SpawnSystem
{
    [JsonProperty]
    private float elapsed;

    [JsonProperty]
    private float spawnWandererTimer = 0;

    [JsonIgnore]
    private CompoundCloudSpawner cloudSpawner;

    [JsonIgnore]
    private ChunkSpawner chunkSpawner;

    [JsonIgnore]
    private MicrobeSpawner microbeSpawner;

    /// <summary>
    ///   Root node to parent all spawned things to
    /// </summary>
    private Node worldRoot;

    // List of SpawnItems, used to fill the spawnItemBag when it is empty.
    [JsonIgnore]
    private List<SpawnItem> spawnItems = new List<SpawnItem>();

    // Used for a Tetris style random bag. Fill and shuffle the bag,
    // then simply pop one out until empty. Rinse and repeat.
    [JsonIgnore]
    private List<SpawnItem> spawnItemBag = new List<SpawnItem>();

    // List of MicrobeItems, used to fill the wanderMicrobeBag.
    [JsonIgnore]
    private List<SpawnItem> microbeItems = new List<SpawnItem>();

    // Used to spawn wandering random microbes when sitting still.
    [JsonIgnore]
    private List<SpawnItem> wanderMicrobeBag = new List<SpawnItem>();

    // Queue of spawn items to spawn. a few from this list get spawned every frame.
    [JsonIgnore]
    private Queue<SpawnItem> itemsToSpawn = new Queue<SpawnItem>();

    [JsonIgnore]
    private int microbeBagSize;

    [JsonProperty]
    private Random random = new Random();

    /// <summary>
    ///   Delete a max of this many entities per step to reduce lag
    ///   from deleting tons of entities at once.
    /// </summary>
    [JsonProperty]
    private int maxEntitiesToDeletePerStep = Constants.MAX_DESPAWNS_PER_FRAME;

    /// <summary>
    ///   This limits the total number of things that can be spawned.
    /// </summary>
    [JsonProperty]
    private int maxAliveEntities = 1000;

    private Queue<Vector3> spawnedGrid = new Queue<Vector3>();

    public SpawnSystem(Node root)
    {
        worldRoot = root;
    }

    public static void AddEntityToTrack(ISpawned entity)
    {
        entity.DespawnRadius = Constants.DESPAWN_ITEM_RADIUS;
        entity.SpawnedNode.AddToGroup(Constants.SPAWNED_GROUP);
    }

    public void Init(CompoundCloudSystem cloudSystem)
    {
        cloudSpawner = new CompoundCloudSpawner(cloudSystem);
        chunkSpawner = new ChunkSpawner(cloudSystem);
        microbeSpawner = new MicrobeSpawner(cloudSystem);
    }

    public void SetCurrentGame(GameProperties currentGame)
    {
        microbeSpawner.SetCurrentGame(currentGame);
    }

    public void AddSpawnItem(SpawnItem spawnItem)
    {
        spawnItems.Add(spawnItem);
    }

    public void AddMicrobeItem(MicrobeItem microbeItem)
    {
        microbeItems.Add(microbeItem);
    }

    public void SetMicrobeBagSize()
    {
        microbeBagSize = microbeItems.Count;
        spawnWandererTimer = Constants.WANDERER_SPAWN_RATE / microbeBagSize;
    }

    // Adds this spot to SpawnedGrid, so that respawning is less likely to give spawn event.
    public void RespawningPlayer(Vector3 newPlayerPos)
    {
        int spawnGridX = (int)newPlayerPos.x / Constants.SPAWN_GRID_SIZE;
        int spawnGridZ = (int)newPlayerPos.z / Constants.SPAWN_GRID_SIZE;

        float respawnGridPosX = spawnGridX * Constants.SPAWN_GRID_SIZE;
        float respawnGridPosZ = spawnGridZ * Constants.SPAWN_GRID_SIZE;

        Vector3 respawnGridPos = new Vector3(respawnGridPosX, 0, respawnGridPosZ);
        GD.Print("RESPAWN GRID: " + respawnGridPos);
        spawnedGrid.Enqueue(respawnGridPos);
    }

    public void ClearBags()
    {
        spawnItems.Clear();
        spawnItemBag.Clear();
        microbeItems.Clear();
        wanderMicrobeBag.Clear();
    }

    /// <summary>
    ///   Despawns all spawned entities
    /// </summary>
    public void DespawnAll()
    {
        var spawnedEntities = worldRoot.GetTree().GetNodesInGroup(Constants.SPAWNED_GROUP);

        foreach (Node entity in spawnedEntities)
        {
            if (!entity.IsQueuedForDeletion())
                entity.DetachAndQueueFree();
        }
    }

    /// <summary>
    ///   Processes spawning and despawning things
    /// </summary>
    public void Process(float delta, Vector3 playerPosition)
    {
        elapsed += delta;

        // Remove the y-position from player position
        playerPosition.y = 0;

        SpawnEventGrid(playerPosition);
        SpawnWanderingMicrobes(playerPosition);
        SpawnItemsInSpawnList();
    }

    // Takes SpawnItems list and shuffles them in bag.
    private void FillBag(List<SpawnItem> bag, List<SpawnItem> fullBag)
    {
        bag.Clear();

        foreach (SpawnItem spawnItem in fullBag)
        {
            bag.Add(spawnItem);
        }

        ShuffleBag(bag);
    }

    // Fisher–Yates Shuffle Alg. Perfectly Random shuffle, O(n)
    private void ShuffleBag(List<SpawnItem> bag)
    {
        for (int i = 0; i < bag.Count - 2; i++)
        {
            int j = random.Next(i, bag.Count);

            // swap i, j
            SpawnItem swap = bag[i];

            bag[i] = bag[j];
            bag[j] = swap;
        }
    }

    private SpawnItem BagPop(List<SpawnItem> bag, List<SpawnItem> fullBag)
    {
        if (bag.Count == 0)
        {
            FillBag(bag, fullBag);
        }

        SpawnItem pop = bag[0];
        bag.RemoveAt(0);

        if (pop is CloudItem cloudPop)
        {
            cloudPop.SetCloudSpawner(cloudSpawner);
        }

        if (pop is ChunkItem chunkPop)
        {
            chunkPop.SetChunkSpawner(chunkSpawner, worldRoot);
        }

        if (pop is MicrobeItem microbePop)
        {
            microbePop.SetMicrobeSpawner(microbeSpawner, worldRoot);
        }

        return pop;
    }

    private void SpawnEventGrid(Vector3 playerPosition)
    {
        int playerGridX = (int)playerPosition.x / Constants.SPAWN_GRID_SIZE;
        int playerGridZ = (int)playerPosition.z / Constants.SPAWN_GRID_SIZE;

        for (int i = -Constants.SPAWN_GRID_WIDTH; i <= Constants.SPAWN_GRID_WIDTH; i++)
        {
            for (int j = -Constants.SPAWN_GRID_WIDTH; j <= Constants.SPAWN_GRID_WIDTH; j++)
            {
                int spawnGridX = playerGridX + i;
                int spawnGridZ = playerGridZ + j;

                float spawnEventPosX = spawnGridX * Constants.SPAWN_GRID_SIZE;
                float spawnEventPosZ = spawnGridZ * Constants.SPAWN_GRID_SIZE;

                Vector3 spawnEvent = new Vector3(spawnEventPosX, 0, spawnEventPosZ);

                if (!spawnedGrid.Contains(spawnEvent))
                {
                    GD.Print("SPAWN EVENT: " + spawnEvent + "  player pos: "+ playerPosition);

                    SpawnEvent(spawnEvent, playerPosition);

                    spawnedGrid.Enqueue(spawnEvent);

                    DespawnEntities(playerPosition);

                    // In case player wanders forever without evolving, toss old spawnEvents.
                    if (spawnedGrid.Count > 50)
                    {
                        spawnedGrid.Dequeue();
                    }
                }
            }
        }
    }

    private void SpawnWanderingMicrobes(Vector3 playerPosition)
    {
        if (elapsed > spawnWandererTimer)
        {
            elapsed = 0;
            float spawnDistance = Constants.SPAWN_WANDERER_RADIUS;
            float spawnAngle = random.NextFloat() * 2 * Mathf.Pi;

            Vector3 spawnWanderer = new Vector3(spawnDistance * Mathf.Sin(spawnAngle),
                0, spawnDistance * Mathf.Cos(spawnAngle));

            GD.Print("SPAWNING WANDERER: " + spawnWandererTimer);
            AddSpawnItemInToSpawnList(spawnWanderer + playerPosition, wanderMicrobeBag, microbeItems);

            DespawnEntities(playerPosition);
        }
    }

    private void SpawnEvent(Vector3 spawnGridPos, Vector3 playerPosition)
    {
        Vector3 spawnEventCenter = GetRandomEventPosition(spawnGridPos, playerPosition);

        for (int i = 0; i < random.Next(Constants.SPAWN_EVENT_MIN, Constants.SPAWN_EVENT_MAX); i++)
        {
            float spawnRadius = (random.NextFloat() + 0.1f) * Constants.SPAWN_EVENT_RADIUS;
            float spawnAngle = random.NextFloat() * 2 * Mathf.Pi;

            Vector3 spawnModPosition = new Vector3(spawnRadius * Mathf.Sin(spawnAngle),
                0, spawnRadius * Mathf.Cos(spawnAngle));

            AddSpawnItemInToSpawnList(spawnEventCenter + spawnModPosition, spawnItemBag, spawnItems);
        }
    }

    private Vector3 GetRandomEventPosition(Vector3 spawnGridPos, Vector3 playerPos)
    {
        // Choose random place to spawn
        float eventCenterX = random.NextFloat() * (Constants.SPAWN_GRID_SIZE - Constants.DESPAWN_ITEM_RADIUS * 2)
            + Constants.DESPAWN_ITEM_RADIUS;

        float eventCenterZ = random.NextFloat() * (Constants.SPAWN_GRID_SIZE - Constants.DESPAWN_ITEM_RADIUS * 2)
            + Constants.DESPAWN_ITEM_RADIUS;

        // Flip X or Z if they are too close to the player
        if (Math.Abs(playerPos.x - eventCenterX) - Constants.SPAWN_EVENT_RADIUS < Constants.DESPAWN_ITEM_RADIUS)
        {
            float subGridX = eventCenterX % Constants.SPAWN_GRID_SIZE;
            subGridX = -subGridX + Constants.SPAWN_GRID_SIZE;
            eventCenterX = (eventCenterX / Constants.SPAWN_GRID_SIZE) + subGridX;
        }

        if (Math.Abs(playerPos.z - eventCenterZ) - Constants.SPAWN_EVENT_RADIUS < Constants.DESPAWN_ITEM_RADIUS)
        {
            float subGridZ = eventCenterZ % Constants.SPAWN_GRID_SIZE;
            subGridZ = -subGridZ + Constants.SPAWN_GRID_SIZE;
            eventCenterZ = (eventCenterZ / Constants.SPAWN_GRID_SIZE) + subGridZ;
        }

        return spawnGridPos + new Vector3(eventCenterX, 0, eventCenterZ);
    }

    private void AddSpawnItemInToSpawnList(Vector3 spawnPos,
        List<SpawnItem> bag, List<SpawnItem> fullBag)
    {
        SpawnItem spawn = BagPop(bag, fullBag);

        // If there are too many entities, do not add more to spawn list.
        var spawnedEntities = worldRoot.GetTree().GetNodesInGroup(Constants.SPAWNED_GROUP);
        if (spawnedEntities.Count >= maxAliveEntities)
            return;

        spawn?.SetSpawnPosition(spawnPos);

        itemsToSpawn.Enqueue(spawn);
    }

    private void SpawnItemsInSpawnList()
    {
        for (int i = 0; i < Constants.MAX_SPAWNS_PER_FRAME; i++)
        {
            if (itemsToSpawn.Count > 0)
            {
                SpawnItem spawn = itemsToSpawn.Dequeue();

                if (spawn == null)
                {
                    break;
                }

                List<ISpawned> spawnedList = spawn.Spawn();
                if (spawnedList != null)
                {
                    foreach (ISpawned spawned in spawnedList)
                    {
                        ProcessSpawnedEntity(spawned);
                    }
                }
            }
        }
    }

    /// <summary>
    ///   Despawns entities that are far away from the player
    /// </summary>
    private void DespawnEntities(Vector3 playerPosition)
    {
        int entitiesDeleted = 0;

        // Despawn entities
        var spawnedEntities = worldRoot.GetTree().GetNodesInGroup(Constants.SPAWNED_GROUP);

        foreach (Node entity in spawnedEntities)
        {
            var spawned = entity as ISpawned;

            if (spawned == null)
            {
                GD.PrintErr("A node has been put in the spawned group " +
                    "but it isn't derived from ISpawned");
                continue;
            }

            var entityPosition = ((Spatial)entity).Translation;

            int playerGridX = (int)playerPosition.x / Constants.SPAWN_GRID_SIZE;
            int playerGridZ = (int)playerPosition.z / Constants.SPAWN_GRID_SIZE;

            float minSpawnX = (playerGridX - Constants.SPAWN_GRID_WIDTH) * Constants.SPAWN_GRID_SIZE;
            float maxSpawnX = (playerGridX + Constants.SPAWN_GRID_WIDTH + 1) * Constants.SPAWN_GRID_SIZE;
            float minSpawnZ = (playerGridZ - Constants.SPAWN_GRID_WIDTH) * Constants.SPAWN_GRID_SIZE;
            float maxSpawnZ = (playerGridZ + Constants.SPAWN_GRID_WIDTH + 1) * Constants.SPAWN_GRID_SIZE;

            // If the entity is too far away from the player, despawn it.
            if (entityPosition.x < minSpawnX || entityPosition.x > maxSpawnX ||
                entityPosition.z < minSpawnZ || entityPosition.z > maxSpawnZ)
            {
                entitiesDeleted++;
                entity.DetachAndQueueFree();

                if (entitiesDeleted >= maxEntitiesToDeletePerStep)
                    break;
            }
        }
    }

    /// <summary>IT
    ///   Add the entity to the spawned group and add the despawn radius
    /// </summary>
    private void ProcessSpawnedEntity(ISpawned entity)
    {
        // I don't understand why the same
        // value is used for spawning and
        // despawning, but apparently it works
        // just fine
        entity.DespawnRadius = Constants.DESPAWN_ITEM_RADIUS;

        entity.SpawnedNode.AddToGroup(Constants.SPAWNED_GROUP);
    }
}
