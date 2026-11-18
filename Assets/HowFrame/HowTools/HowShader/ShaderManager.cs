using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HowEnum;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HowFrame
{
    /// <summary>
    /// Shader / 材质管理器：
    /// - Shader 字典：按名字取 Shader（字典 → AssetAssistant 缓存 → Shader.Find），取到后缓存；
    /// - 材质字典：用 key 管理材质，可从 Shader 新建、从已有材质克隆、登记外部材质；
    /// - 按字符串改参数：SetFloat("Water", "_WaveSpeed", 2) —— key 是材质 key 时改这一个材质，
    ///   是 Shader 名时改字典里所有用这个 Shader 的材质（材质 key 优先）；
    /// - 全局参数 / 关键字：SetGlobalXxx、SetGlobalKeyword；
    /// - 属性名可以传字符串，也可以传 Shader Keys Generator 生成的常量（ShaderProperty）。
    /// 编辑器下修改材质「资源」时会先做快照，退出 Play 自动还原，避免运行时调参把资源改掉。
    /// </summary>
    public static class ShaderManager
    {
        private class MaterialEntry
        {
            public Material Material;
            /// <summary>由管理器创建（new / 克隆），Release 时销毁</summary>
            public bool Owned;
        }

        private static readonly Dictionary<string, Shader> _shaders = new Dictionary<string, Shader>();
        private static readonly Dictionary<string, MaterialEntry> _materials = new Dictionary<string, MaterialEntry>();
        private static readonly HashSet<string> _warned = new HashSet<string>();
        private static bool _initialized;

        /// <summary>已登记的 Shader（名字 → Shader）</summary>
        public static IReadOnlyDictionary<string, Shader> Shaders => _shaders;

        /// <summary>已登记的材质 key</summary>
        public static IEnumerable<string> MaterialKeys => _materials.Keys;

        #region Shader 字典

        /// <summary>登记 Shader；key 为空时用 shader.name（如 "Universal Render Pipeline/Lit"）</summary>
        public static void RegisterShader(Shader shader, string key = null)
        {
            if (shader == null) return;
            _shaders[string.IsNullOrEmpty(key) ? shader.name : key] = shader;
        }

        /// <summary>按名字取 Shader：字典 → AssetAssistant 缓存 → Shader.Find（需要被引用或在 Always Included 中才会进包）</summary>
        public static bool TryGetShader(string name, out Shader shader)
        {
            shader = null;
            if (string.IsNullOrEmpty(name)) return false;
            if (_shaders.TryGetValue(name, out shader) && shader != null) return true;

            if (!AssetAssistant.TryAddressableGet(name, out shader)) shader = Shader.Find(name);
            if (shader == null) return false;

            _shaders[name] = shader;
            _shaders[shader.name] = shader;
            return true;
        }

        public static Shader GetShader(string name)
        {
            if (TryGetShader(name, out var shader)) return shader;
            WarnOnce("shader:" + name, $"[ShaderManager] 没有找到 Shader: {name}（未加载到 Addressables 缓存、也未被打进包？）");
            return null;
        }

        public static bool HasShader(string name) => TryGetShader(name, out _);

        /// <summary>把 AssetAssistant 缓存里的 Shader 全部登记进字典（加载新 Label 后可调用）</summary>
        public static int RefreshFromCache()
        {
            int count = 0;
            foreach (var (_, asset, _) in AssetAssistant.CacheEntries().ToList())
            {
                if (asset is Shader shader)
                {
                    RegisterShader(shader);
                    count++;
                }
            }
            return count;
        }

        #endregion

        #region 材质字典

        /// <summary>用 Shader 新建材质并登记（已有同名 key 时先释放旧的）</summary>
        public static Material CreateMaterial(string key, string shaderName)
        {
            var shader = GetShader(shaderName);
            return shader == null ? null : CreateMaterial(key, shader);
        }

        public static Material CreateMaterial(string key, Shader shader)
        {
            if (string.IsNullOrEmpty(key) || shader == null) return null;
            var material = new Material(shader) { name = key };
            SetEntry(key, material, true);
            return material;
        }

        /// <summary>克隆一个材质（来源可以是材质 key、缓存里的材质资源名，或 Shader 名）并以 newKey 登记</summary>
        public static Material CloneMaterial(string newKey, string sourceKey)
        {
            if (string.IsNullOrEmpty(newKey)) return null;
            if (TryGetMaterial(sourceKey, out var source)) return CloneMaterial(newKey, source);
            if (TryGetShader(sourceKey, out var shader)) return CreateMaterial(newKey, shader);
            WarnOnce("clone:" + sourceKey, $"[ShaderManager] 克隆失败：没有名为 {sourceKey} 的材质或 Shader");
            return null;
        }

        public static Material CloneMaterial(string newKey, Material source)
        {
            if (string.IsNullOrEmpty(newKey) || source == null) return null;
            var material = new Material(source) { name = newKey };
            SetEntry(newKey, material, true);
            return material;
        }

        /// <summary>登记一个外部材质（例如场景里某个物体正在用的材质）；管理器不会销毁它</summary>
        public static void RegisterMaterial(string key, Material material)
        {
            if (string.IsNullOrEmpty(key) || material == null) return;
            SetEntry(key, material, false);
        }

        /// <summary>按 key 取材质：字典 → AssetAssistant 缓存中的同名材质资源（取到后登记）</summary>
        public static bool TryGetMaterial(string key, out Material material)
        {
            material = null;
            if (string.IsNullOrEmpty(key)) return false;
            if (_materials.TryGetValue(key, out var entry))
            {
                if (entry.Material != null)
                {
                    material = entry.Material;
                    return true;
                }
                _materials.Remove(key); // 被外部销毁了
            }

            if (!AssetAssistant.TryAddressableGet(key, out material)) return false;
            SetEntry(key, material, false);
            return true;
        }

        public static Material GetMaterial(string key)
        {
            if (TryGetMaterial(key, out var material)) return material;
            WarnOnce("material:" + key, $"[ShaderManager] 没有找到材质: {key}");
            return null;
        }

        public static bool HasMaterial(string key) => TryGetMaterial(key, out _);

        /// <summary>把字典里的材质赋给 Renderer（共享材质：之后改这个 key 的参数，所有用它的物体一起变）</summary>
        public static bool Apply(Renderer renderer, string materialKey, int slot = 0)
        {
            if (renderer == null || !TryGetMaterial(materialKey, out var material)) return false;
            var shared = renderer.sharedMaterials;
            if (slot < 0 || slot >= shared.Length)
            {
                if (shared.Length == 0 && slot == 0) shared = new Material[1];
                else return false;
            }
            shared[slot] = material;
            renderer.sharedMaterials = shared;
            return true;
        }

        /// <summary>移除材质；由管理器创建的会被销毁</summary>
        public static void ReleaseMaterial(string key)
        {
            if (key == null || !_materials.TryGetValue(key, out var entry)) return;
            _materials.Remove(key);
            if (entry.Owned && entry.Material != null) DestroyMaterial(entry.Material);
        }

        /// <summary>清空材质字典（销毁管理器创建的材质）；Shader 字典保留</summary>
        public static void ReleaseAllMaterials()
        {
            foreach (var key in _materials.Keys.ToList()) ReleaseMaterial(key);
        }

        private static void SetEntry(string key, Material material, bool owned)
        {
            if (_materials.TryGetValue(key, out var old) && old.Material != material) ReleaseMaterial(key);
            _materials[key] = new MaterialEntry { Material = material, Owned = owned };
        }

        private static void DestroyMaterial(Material material)
        {
            if (Application.isPlaying) Object.Destroy(material);
            else Object.DestroyImmediate(material);
        }

        #endregion

        #region 按字符串改参数

        /// <summary>
        /// key 对应的材质：是材质 key 时就是它；否则当作 Shader 名，返回字典里所有用这个 Shader 的材质。
        /// </summary>
        public static List<Material> Resolve(string key)
        {
            var result = new List<Material>();
            if (TryGetMaterial(key, out var material))
            {
                result.Add(material);
                return result;
            }
            if (!TryGetShader(key, out var shader)) return result;
            foreach (var entry in _materials.Values)
                if (entry.Material != null && entry.Material.shader == shader) result.Add(entry.Material);
            return result;
        }

        public static int SetFloat(string key, ShaderProperty property, float value) => Modify(key, property, m => m.SetFloat(property.Id, value));
        public static int SetInt(string key, ShaderProperty property, int value) => Modify(key, property, m => m.SetInteger(property.Id, value));
        public static int SetColor(string key, ShaderProperty property, Color value) => Modify(key, property, m => m.SetColor(property.Id, value));
        public static int SetVector(string key, ShaderProperty property, Vector4 value) => Modify(key, property, m => m.SetVector(property.Id, value));
        public static int SetTexture(string key, ShaderProperty property, Texture value) => Modify(key, property, m => m.SetTexture(property.Id, value));

        /// <summary>开关材质（局部）关键字</summary>
        public static int SetKeyword(string key, string keyword, bool enabled)
        {
            var targets = Resolve(key);
            if (targets.Count == 0) WarnMissingTarget(key);
            foreach (var m in targets)
            {
                Snapshot(m);
                if (enabled) m.EnableKeyword(keyword);
                else m.DisableKeyword(keyword);
            }
            return targets.Count;
        }

        public static float GetFloat(string key, ShaderProperty property, float fallback = 0f)
        {
            var m = Resolve(key).FirstOrDefault(x => x.HasProperty(property.Id));
            return m != null ? m.GetFloat(property.Id) : fallback;
        }

        public static Color GetColor(string key, ShaderProperty property, Color fallback = default)
        {
            var m = Resolve(key).FirstOrDefault(x => x.HasProperty(property.Id));
            return m != null ? m.GetColor(property.Id) : fallback;
        }

        public static Vector4 GetVector(string key, ShaderProperty property, Vector4 fallback = default)
        {
            var m = Resolve(key).FirstOrDefault(x => x.HasProperty(property.Id));
            return m != null ? m.GetVector(property.Id) : fallback;
        }

        /// <summary>
        /// 用字符串值设置参数，按 Shader 中声明的属性类型自动解析（控制台 / 配置表用）：
        /// Float/Range: 1.5　Int: 3　Color: #FF8800 / #FF880080 / 1,0.5,0,1　Vector: 1,2,3,4　Texture: 缓存中的贴图名
        /// </summary>
        public static bool TrySetFromString(string key, ShaderProperty property, string value, out string error)
        {
            error = null;
            var targets = Resolve(key);
            if (targets.Count == 0)
            {
                error = $"没有名为 {key} 的材质或 Shader";
                return false;
            }

            var shader = targets[0].shader;
            int index = shader.FindPropertyIndex(property.Name);
            if (index < 0)
            {
                error = $"{shader.name} 没有属性 {property.Name}";
                return false;
            }

            switch (shader.GetPropertyType(index))
            {
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) break;
                    SetFloat(key, property, f);
                    return true;
                case ShaderPropertyType.Int:
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) break;
                    SetInt(key, property, i);
                    return true;
                case ShaderPropertyType.Color:
                    if (!TryParseColor(value, out var c)) break;
                    SetColor(key, property, c);
                    return true;
                case ShaderPropertyType.Vector:
                    if (!TryParseVector(value, out var v)) break;
                    SetVector(key, property, v);
                    return true;
                case ShaderPropertyType.Texture:
                    Texture tex = null;
                    if (!string.IsNullOrEmpty(value) && value != "null" && !AssetAssistant.TryAddressableGet(value, out tex)) break;
                    SetTexture(key, property, tex);
                    return true;
            }

            error = $"无法把 \"{value}\" 解析为 {shader.GetPropertyType(index)}（属性 {property.Name}）";
            return false;
        }

        public static bool TryParseColor(string value, out Color color)
        {
            color = default;
            if (string.IsNullOrEmpty(value)) return false;
            if (value.StartsWith("#")) return ColorUtility.TryParseHtmlString(value, out color);
            if (!TryParseVector(value, out var v, 3)) return false;
            color = new Color(v.x, v.y, v.z, value.Split(',').Length >= 4 ? v.w : 1f);
            return true;
        }

        public static bool TryParseVector(string value, out Vector4 vector, int minComponents = 1)
        {
            vector = default;
            if (string.IsNullOrEmpty(value)) return false;
            var parts = value.Split(',');
            if (parts.Length < minComponents || parts.Length > 4) return false;
            for (int i = 0; i < parts.Length; i++)
            {
                if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) return false;
                vector[i] = f;
            }
            return true;
        }

        private static int Modify(string key, ShaderProperty property, Action<Material> set)
        {
            var targets = Resolve(key);
            if (targets.Count == 0)
            {
                WarnMissingTarget(key);
                return 0;
            }

            int changed = 0;
            foreach (var m in targets)
            {
                if (!m.HasProperty(property.Id))
                {
                    WarnOnce($"prop:{m.shader.name}/{property.Name}", $"[ShaderManager] {m.shader.name} 没有属性 {property.Name}");
                    continue;
                }
                Snapshot(m);
                set(m);
                changed++;
            }
            return changed;
        }

        private static void WarnMissingTarget(string key) =>
            WarnOnce("target:" + key, $"[ShaderManager] 没有名为 {key} 的材质或 Shader");

        #endregion

        #region 全局参数

        public static void SetGlobalFloat(ShaderProperty property, float value) => Shader.SetGlobalFloat(property.Id, value);
        public static void SetGlobalInt(ShaderProperty property, int value) => Shader.SetGlobalInteger(property.Id, value);
        public static void SetGlobalColor(ShaderProperty property, Color value) => Shader.SetGlobalColor(property.Id, value);
        public static void SetGlobalVector(ShaderProperty property, Vector4 value) => Shader.SetGlobalVector(property.Id, value);
        public static void SetGlobalTexture(ShaderProperty property, Texture value) => Shader.SetGlobalTexture(property.Id, value);

        public static float GetGlobalFloat(ShaderProperty property) => Shader.GetGlobalFloat(property.Id);
        public static Color GetGlobalColor(ShaderProperty property) => Shader.GetGlobalColor(property.Id);
        public static Vector4 GetGlobalVector(ShaderProperty property) => Shader.GetGlobalVector(property.Id);

        public static void SetGlobalKeyword(string keyword, bool enabled)
        {
            if (enabled) Shader.EnableKeyword(keyword);
            else Shader.DisableKeyword(keyword);
        }

        public static bool IsGlobalKeywordEnabled(string keyword) => Shader.IsKeywordEnabled(keyword);

        #endregion

        #region 编辑器下保护材质资源

        // 编辑器里改材质资源会直接写进 .mat 文件；第一次修改前留快照，退出 Play 时还原
        private static readonly Dictionary<Material, Material> _snapshots = new Dictionary<Material, Material>();

        private static void Snapshot(Material material)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying || _snapshots.ContainsKey(material)) return;
            if (!UnityEditor.AssetDatabase.Contains(material)) return;
            _snapshots[material] = new Material(material) { hideFlags = HideFlags.HideAndDontSave };
#endif
        }

        /// <summary>把被修改过的材质资源还原（编辑器下退出 Play 时自动调用）</summary>
        public static void RestoreMaterialAssets()
        {
            foreach (var pair in _snapshots)
            {
                if (pair.Key != null && pair.Value != null)
                {
                    pair.Key.CopyPropertiesFromMaterial(pair.Value);
                    pair.Key.shaderKeywords = pair.Value.shaderKeywords;
                }
                if (pair.Value != null) Object.DestroyImmediate(pair.Value);
            }
            _snapshots.Clear();
        }

        #endregion

        private static void WarnOnce(string id, string message)
        {
            if (_warned.Add(id)) Debug.LogWarning(message);
        }

        /// <summary>初始化：登记已缓存的 Shader；编辑器下退出 Play 时还原被改动的材质资源</summary>
        public static void Wake()
        {
            if (_initialized) return;
            _initialized = true;
            RefreshFromCache();
            Application.quitting += () =>
            {
                RestoreMaterialAssets();
                ReleaseAllMaterials();
            };
        }
    }
}
