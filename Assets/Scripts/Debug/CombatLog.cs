#if UNITY_2019_4_OR_NEWER || UNITY_6000_0_OR_NEWER
#define UNITY_PRESENT
#endif

using System;
using System.Text;
using UnityEngine;

namespace DebugTools
{
    public enum LogLevel { Off = 0, Basic = 1, Verbose = 2 }

    [CreateAssetMenu(fileName = "CombatLogConfig", menuName = "Debug/Combat Log Config")]
    public class CombatLogConfig : ScriptableObject
    {
        [Header("Enable/Disable entire system")]
        public bool enabled = true;
        public LogLevel level = LogLevel.Verbose;

        [Header("Categories")]
        public bool cast = true;
        public bool resolve = true;
        public bool status = true;
        public bool zones = true;
        public bool summons = true;
        public bool timer = true;
        public bool ui = true;
        public bool targeting = true;
        public bool movement = false;

        [Header("Formatting")]
        public bool richText = true;
        public bool includeTimestamps = true;
        public bool includeActorAndViewIds = true;
    }

    public static class CombatLog
    {
        private static CombatLogConfig _config;
        public static CombatLogConfig Config
        {
            get
            {
                if (_config == null)
                {
#if UNITY_PRESENT
                    _config = Resources.Load<CombatLogConfig>("CombatLogConfig");
#endif
                    if (_config == null)
                    {
                        _config = ScriptableObject.CreateInstance<CombatLogConfig>();
                    }
                }
                return _config;
            }
            set { _config = value; }
        }

        public static string NewTraceId() => Guid.NewGuid().ToString("N").Substring(0, 8);

        public static string KV(params (string k, object v)[] kvs)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < kvs.Length; i++)
            {
                if (i > 0) sb.Append("  ");
                sb.Append(kvs[i].k).Append('=').Append(kvs[i].v);
            }
            return sb.ToString();
        }

        private static string Prefix(string category, string traceId)
        {
            var t = Config.includeTimestamps ? DateTime.Now.ToString("HH:mm:ss.fff") + " " : "";
            if (Config.richText)
                return $"{t}<b>[{category}]</b>({traceId}) ";
            return $"{t}[{category}]({traceId}) ";
        }

        // Core emitters
        private static void Emit(LogType type, string category, string traceId, string msg, UnityEngine.Object ctx = null)
        {
            if (!Config.enabled || Config.level == LogLevel.Off) return;
            var final = Prefix(category, traceId) + msg;
#if UNITY_PRESENT
            Debug.unityLogger.Log(type, final);
#else
            Console.WriteLine($"{type}: {final}");
#endif
        }

        // Public helpers per category
        public static void Cast(string traceId, string msg, UnityEngine.Object ctx = null)
        { if (Config.cast) Emit(LogType.Log, "Cast", traceId, msg, ctx); }

        public static void Resolve(string traceId, string msg, UnityEngine.Object ctx = null)
        { if (Config.resolve) Emit(LogType.Log, "Resolve", traceId, msg, ctx); }

        public static void Status(string traceId, string msg, UnityEngine.Object ctx = null)
        { if (Config.status) Emit(LogType.Log, "Status", traceId, msg, ctx); }

        public static void Zones(string traceId, string msg, UnityEngine.Object ctx = null)
        { if (Config.zones) Emit(LogType.Log, "Zones", traceId, msg, ctx); }

        public static void Summons(string traceId, string msg, UnityEngine.Object ctx = null)
        { if (Config.summons) Emit(LogType.Log, "Summons", traceId, msg, ctx); }

        public static void Timer(string traceId, string msg, UnityEngine.Object ctx = null)
        { if (Config.timer) Emit(LogType.Log, "Timer", traceId, msg, ctx); }

        public static void UI(string traceId, string msg, UnityEngine.Object ctx = null)
        { if (Config.ui) Emit(LogType.Log, "UI", traceId, msg, ctx); }

        public static void Targeting(string traceId, string msg, UnityEngine.Object ctx = null)
        { if (Config.targeting) Emit(LogType.Log, "Targeting", traceId, msg, ctx); }

        public static void Movement(string traceId, string msg, UnityEngine.Object ctx = null)
        { if (Config.movement) Emit(LogType.Log, "Movement", traceId, msg, ctx); }

        public static string Short(UnityEngine.GameObject go)
        {
            if (go == null) return "null";
            var viewId = TryGetPhotonViewId(go);
            return viewId >= 0 ? $"{go.name}#{viewId}" : go.name;
        }

        public static int TryGetPhotonViewId(UnityEngine.GameObject go)
        {
#if PHOTON_UNITY_NETWORKING
            var v = go.GetComponent<Photon.Pun.PhotonView>();
            return v ? v.ViewID : -1;
#else
            return -1;
#endif
        }
    }
}
