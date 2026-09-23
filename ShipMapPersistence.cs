using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace LongshipUpgrades
{
    public partial class LongshipUpgrades
    {
        private static int s_shipMapConfigurationUpdateRequested = 1;

        internal static bool ShipMapsEnabled => mapTableEnabled == null || mapTableEnabled.Value;

        private static void RequestShipMapConfigurationUpdate()
        {
            // Configuration callbacks may originate on a file-watcher thread. All ZDO work
            // stays in the main-thread update/load/save path instead of running in this callback.
            Interlocked.Exchange(ref s_shipMapConfigurationUpdateRequested, 1);
        }

        private static void ApplyShipMapConfiguration()
        {
            if (mapTableEnabled == null || ZNet.instance == null || ZDOMan.instance == null)
                return;
            if (Interlocked.Exchange(ref s_shipMapConfigurationUpdateRequested, 0) == 0 || ShipMapsEnabled)
                return;

            int clearedShips = 0;
            try
            {
                // The server's ZDO index includes ships outside the loaded scene. Collect
                // only ships before raising revisions, which other mods may also observe.
                foreach (ZDO zdo in ZDOMan.instance.m_objectsByID.Values.Where(IsShipZdo).ToList())
                {
                    if (PurgeShipMapData(zdo))
                        clearedShips++;
                }

                // Also discard obsolete cache entries whose ships no longer exist.
                s_shipMapStore.Clear();
            }
            catch
            {
                RequestShipMapConfigurationUpdate();
                throw;
            }
            LogInfo($"Ship map tables are disabled: cleared recorded map data from {clearedShips} ship(s). Changes will be persisted by the next world save started after cleanup.");
        }

        private static bool PurgeShipMapData(ZDO zdo)
        {
            if (!IsShipZdo(zdo) || zdo.SaveClone)
                return false;

            bool changed = s_shipMapStore.Remove(zdo.m_uid);
            changed |= RemoveShipMapDataFromRuntime(zdo.m_uid);
            changed |= RemoveDetachedZdoField(ZDOExtraData.s_ints, zdo.m_uid, LongshipCustomizableParts.s_mapDataCompressed);
            changed |= RemoveDetachedZdoField(ZDOExtraData.s_ints, zdo.m_uid, s_shipMapDataRevision);

            // Raw extra-data deletion does not mark a chunk dirty. Without this revision
            // change, an unloaded ship can retain its old map in the incremental world save.
            // Clients discard their local copy only; they must not advance remote ZDO revisions.
            if (changed && ZNet.instance != null && ZNet.instance.IsServer())
                zdo.IncreaseDataRevision();

            return changed;
        }

        private static BinarySearchDictionary<int, T> CopyZdoFields<T>(BinarySearchDictionary<int, T> source, int additionalCapacity = 0)
        {
            BinarySearchDictionary<int, T> copy = new BinarySearchDictionary<int, T>();
            copy.Reserve((source?.Count ?? 0) + additionalCapacity);
            if (source != null)
            {
                foreach (KeyValuePair<int, T> field in source)
                    copy.Add(field.Key, field.Value);
            }
            return copy;
        }

        private static void DetachZdoFields<T>(Dictionary<ZDOID, BinarySearchDictionary<int, T>> container, ZDOID id)
        {
            if (container.TryGetValue(id, out BinarySearchDictionary<int, T> fields) && fields != null)
                container[id] = CopyZdoFields(fields);
        }

        private static bool RemoveDetachedZdoField<T>(Dictionary<ZDOID, BinarySearchDictionary<int, T>> container, ZDOID id, int hash)
        {
            if (!container.TryGetValue(id, out BinarySearchDictionary<int, T> fields) || fields == null || !fields.ContainsKey(hash))
                return false;

            // BinarySearchDictionary.Clone() shares its key/value arrays with the source.
            // Never shift a live container's arrays: an active save snapshot may use them.
            // The old container must not be returned to the pool for the same reason.
            if (fields.Count == 1)
            {
                container.Remove(id);
                return true;
            }

            BinarySearchDictionary<int, T> remaining = new BinarySearchDictionary<int, T>();
            remaining.Reserve(fields.Count - 1);
            foreach (KeyValuePair<int, T> field in fields)
            {
                if (field.Key != hash)
                    remaining.Add(field.Key, field.Value);
            }
            container[id] = remaining;
            return true;
        }

        private static void SetShipMapDataInRuntime(ZDOID id, byte[] data)
        {
            ZDOExtraData.s_byteArrays.TryGetValue(id, out BinarySearchDictionary<int, byte[]> fields);
            if (fields != null && fields.TryGetValue(ZDOVars.s_data, out byte[] current) && ReferenceEquals(current, data))
                return;

            BinarySearchDictionary<int, byte[]> copy = CopyZdoFields(fields, 1);
            // Stored payloads are immutable here. Copy the container arrays, not the map bytes.
            copy[ZDOVars.s_data] = data;
            ZDOExtraData.s_byteArrays[id] = copy;
        }

        private static void SetShipMapInt(ZDO zdo, int hash, int value)
        {
            if (zdo.GetInt(hash) == value)
                return;

            // Map metadata can change while a previously captured world save is being written.
            DetachZdoFields(ZDOExtraData.s_ints, zdo.m_uid);
            zdo.Set(hash, value);
        }

        public sealed class ShipMapSaveState
        {
            internal readonly List<KeyValuePair<ZDOID, byte[]>> InjectedMaps = new List<KeyValuePair<ZDOID, byte[]>>();
            internal readonly Stopwatch Timer = Stopwatch.StartNew();
            internal bool CleanupCompleted;
        }

        private static void InjectShipMapDataForSave(ShipMapSaveState state)
        {
            LogInfo($"Ship map save preparation started: {s_shipMapStore.Count} cached map(s).");
            long injectedBytes = 0;
            foreach (ZDOID id in s_shipMapStore.Keys.ToList())
            {
                ZDO zdo = ZDOMan.instance?.GetZDO(id);
                if (!IsShipZdo(zdo) || !s_shipMapStore.TryGetValue(id, out ShipMapDataEntry entry) || entry?.Data == null)
                    continue;

                // A nested map operation may already expose newer data. Leave it in place,
                // and do not remove data owned by that operation when this save returns.
                if (zdo.GetByteArray(ZDOVars.s_data) != null)
                    continue;

                // Record ownership before insertion so the finalizer also handles a partial failure.
                state.InjectedMaps.Add(new KeyValuePair<ZDOID, byte[]>(id, entry.Data));
                SetShipMapDataInRuntime(id, entry.Data);
                injectedBytes += entry.Data.LongLength;
            }

            LogInfo($"Ship map save injection ready: {state.InjectedMaps.Count} map(s), {injectedBytes} payload bytes, {state.Timer.Elapsed.TotalMilliseconds:F2} ms. Entering world snapshot preparation.");
        }

        private static void RemoveShipMapSaveInjection(ShipMapSaveState state)
        {
            if (state == null || state.CleanupCompleted)
                return;
            state.CleanupCompleted = true;

            foreach (KeyValuePair<ZDOID, byte[]> entry in state.InjectedMaps)
            {
                if (ZDOExtraData.s_byteArrays.TryGetValue(entry.Key, out BinarySearchDictionary<int, byte[]> fields)
                    && fields != null && fields.TryGetValue(ZDOVars.s_data, out byte[] current) && ReferenceEquals(current, entry.Value))
                {
                    RemoveShipMapDataFromRuntime(entry.Key);
                }

                // Separate map metadata from the captured snapshot before ordinary ship
                // updates can insert, remove or update other integer fields on the next frame.
                DetachZdoFields(ZDOExtraData.s_ints, entry.Key);
            }

            state.Timer.Stop();
            LogInfo($"Ship map save injection cleared: {state.InjectedMaps.Count} map(s); total PrepareSave scope {state.Timer.Elapsed.TotalMilliseconds:F2} ms (including game and other patches).");
        }
    }
}
