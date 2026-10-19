// ============================================================
// 决斗聚焦（Duel Spotlight，v0.21.39）
// 解决"全挤在一起分不清谁是谁"：把正在交战的单位从混战里"提出来"。
//   - 暗场：复用 CombatFxCinema.Dim 压暗全场（焦点窗口内）
//   - 焦点单位：脚下发光环 + 头顶菱形标记（SpriteRenderer 每帧跟随）
// 单一职责：只维护"焦点集合 + 聚焦窗口 + 跟随标记"，
// 不碰伤害/AI/招式逻辑；挂载点见 CombatFxDirector.Tick 与
// CombatMoveDirector.Execute（重招触发）。
// 纹理 static 生成一次并缓存（不每帧建，遵守 v0.18 性能红线）。
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace Code.Combat.Fx
{
    public static class CombatSpotlight
    {
        private const float VeilAlpha = 0.42f;     // 暗场强度（焦点单位仍清晰发光）
        private const int SortRing = 29100;        // 高于暗幕(29000)，低于 Cine 粒子(30000+)
        private const int SortMark = 29101;

        private class FocusUnit
        {
            public Actor Actor;
            public SpriteRenderer Ring;
            public SpriteRenderer Mark;
        }

        private static readonly List<FocusUnit> _units = new List<FocusUnit>(4);
        private static float _left;
        private static Sprite _ringSprite, _markSprite;

        /// <summary>是否正处在聚焦窗口内。</summary>
        public static bool Active => _left > 0f;

        /// <summary>聚焦一对交战单位 'seconds' 秒（真实时间）；重招/终极招使用。</summary>
        public static void Focus(Actor a, Actor b, float seconds)
        {
            if (a == null && b == null) return;
            SetWindow(seconds);
            Add(a);
            Add(b);
        }

        /// <summary>技能演出聚焦：以攻击者为中心短暂聚焦（无明确对手时）。</summary>
        public static void Pulse(Actor actor, float seconds)
        {
            if (actor == null) return;
            SetWindow(seconds);
            Add(actor);
        }

        // ------------------------------------------------------ 内部 --

        private static void SetWindow(float seconds)
        {
            _left = Mathf.Max(_left, Mathf.Max(seconds, 0.6f));
            // 金色暗场，聚焦单位在暗幕上发光更醒目
            CombatFxCinema.Dim(VeilAlpha, new Color(0.03f, 0.03f, 0.08f), 0.22f);
        }

        private static void Add(Actor actor)
        {
            if (actor == null) return;
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].Actor == actor) return;
            }
            EnsureSprites();
            FocusUnit u = new FocusUnit { Actor = actor };
            u.Ring = MakeRenderer(_ringSprite, SortRing);
            u.Mark = MakeRenderer(_markSprite, SortMark);
            _units.Add(u);
        }

        private static void Release(int i)
        {
            FocusUnit u = _units[i];
            _units.RemoveAt(i);
            if (u.Ring != null) UnityEngine.Object.Destroy(u.Ring.gameObject);
            if (u.Mark != null) UnityEngine.Object.Destroy(u.Mark.gameObject);
        }

        private static void ReleaseAll()
        {
            for (int i = _units.Count - 1; i >= 0; i--) Release(i);
        }

        /// <summary>每帧驱动：计时 + 焦点标记跟随单位 + 窗口结束恢复（由 CombatFxDirector.Tick 调用）。</summary>
        public static void Tick()
        {
            if (_left <= 0f)
            {
                if (_units.Count > 0)
                {
                    ReleaseAll();
                    CombatFxCinema.Undim(0.5f);
                }
                return;
            }
            _left -= Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (World.world == null || World.world.isPaused()) return;
            for (int i = _units.Count - 1; i >= 0; i--)
            {
                FocusUnit u = _units[i];
                if (u.Actor == null || !u.Actor.isAlive())
                {
                    Release(i);
                    continue;
                }
                Vector2 p = u.Actor.current_position;
                float r = BodyRadius(u.Actor);
                float k = r / 0.35f;                     // 体型归一化（人类单位基准）
                if (u.Ring != null)
                {
                    u.Ring.transform.position = new Vector3(p.x, p.y, 0f);
                    u.Ring.transform.localScale = new Vector3(0.40f * k, 0.40f * k, 1f);
                }
                if (u.Mark != null)
                {
                    u.Mark.transform.position = new Vector3(p.x, p.y + r + 0.22f, 0f);
                    u.Mark.transform.localScale = new Vector3(0.14f * k, 0.14f * k, 1f);
                }
            }
            if (_left <= 0f && _units.Count > 0)
            {
                ReleaseAll();
                CombatFxCinema.Undim(0.5f);
            }
        }

        /// <summary>世界销毁/存档切换：清空聚焦（由 CombatFxDirector.ClearAll 调用）。</summary>
        public static void Clear()
        {
            _left = 0f;
            ReleaseAll();
            CombatFxCinema.Undim(0.5f);
        }

        private static float BodyRadius(Actor actor)
        {
            // 原版 ActorData 不暴露体型字段；固定用人类单位基准（0.35 世界宽）——
            // 聚焦标记只做"区分"，固定尺寸足够且零风险（不随体型/种族波动）。
            return 0.35f;
        }

        // ------------------------------------------------------ 资源 --

        private static void EnsureSprites()
        {
            if (_ringSprite == null) _ringSprite = MakeRing();
            if (_markSprite == null) _markSprite = MakeMark();
        }

        private static SpriteRenderer MakeRenderer(Sprite s, int order)
        {
            GameObject go = new GameObject("GeneSpotlightFx");
            UnityEngine.Object.DontDestroyOnLoad(go);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            if (s != null) sr.sprite = s;
            sr.sortingLayerID = 0;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>脚下光环：32px 圆环纹理（static 生成一次并缓存）。</summary>
        private static Sprite MakeRing()
        {
            const int s = 32;
            float c = (s - 1) * 0.5f;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(1.1f - Mathf.Abs(d - 10f) / 2.2f);
                    tex.SetPixel(x, y, new Color(1f, 0.94f, 0.78f, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>头顶菱形标记：16px（static 生成一次并缓存）。</summary>
        private static Sprite MakeMark()
        {
            const int s = 16;
            float c = (s - 1) * 0.5f;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float nx = Mathf.Abs(x - c) / c;
                    float ny = Mathf.Abs(y - c) / c;
                    float a = Mathf.Clamp01(1f - (nx + ny));
                    tex.SetPixel(x, y, new Color(1f, 0.85f, 0.4f, a * 0.95f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
