using System;
using UnityEngine;

namespace NANINuko.Reflection
{
    internal static class NANINukoCharaReflectionHelper
    {
        internal static void ReviveById(string id)
        {
            try
            {
                id = id?.Trim();
                if (string.IsNullOrEmpty(id))
                    return;

                var game = EClass.game;
                var pc = EClass.pc;
                var globalCharas = game?.cards?.globalCharas;

                if (globalCharas == null || pc == null)
                    return;

                var c = globalCharas.Find(id);
                if (c == null)
                    return;

                if (!c.isDead)
                    return;

                c.Revive(pc.pos, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NANINukoCharaReflectionHelper] ReviveById('{id}') failed: {ex}");
            }
        }

        internal static void MakeAllyOnMap(string id)
        {
            try
            {
                id = id?.Trim();
                if (string.IsNullOrEmpty(id))
                    return;

                var map = EClass._map;
                if (map == null)
                    return;

                var c = map.FindChara(id);
                if (c == null)
                    return;

                c.MakeAlly(true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NANINukoCharaReflectionHelper] MakeAllyOnMap('{id}') failed: {ex}");
            }
        }

        internal static void RemoveAllyOnMap(string id)
        {
            try
            {
                id = id?.Trim();
                if (string.IsNullOrEmpty(id))
                    return;

                var map = EClass._map;
                var pc = EClass.pc;
                var party = pc?.party;

                if (map == null || pc == null || party == null)
                    return;

                var c = map.FindChara(id);
                if (c == null)
                    return;

                party.RemoveMember(c);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NANINukoCharaReflectionHelper] RemoveAllyOnMap('{id}') failed: {ex}");
            }
        }

        internal static void MoveCharaOnMap(string id, int x, int z)
        {
            try
            {
                id = id?.Trim();
                if (string.IsNullOrEmpty(id))
                    return;

                var map = EClass._map;
                if (map == null)
                    return;

                var c = map.FindChara(id);
                if (c == null)
                    return;

                map.MoveCard(new Point(x, z), c);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NANINukoCharaReflectionHelper] MoveCharaOnMap('{id}', {x}, {z}) failed: {ex}");
            }
        }
    }
}

