using System;
using UnityEngine;

namespace HowEnum
{
    /// <summary>
    /// Shader 属性键：名字 + 缓存的 PropertyToID。
    /// 可由字符串隐式转换（ShaderManager 的 API 既能传 "_BaseColor" 也能传生成的常量），
    /// 生成的常量见 Shader Keys Generator（Tools/HowFrame/Shader Keys Generator）。
    /// </summary>
    public readonly struct ShaderProperty : IEquatable<ShaderProperty>
    {
        public readonly string Name;

        public ShaderProperty(string name)
        {
            Name = name;
        }

        /// <summary>Shader.PropertyToID（Unity 内部有缓存，开销很小）</summary>
        public int Id => Shader.PropertyToID(Name ?? "");

        public bool IsValid => !string.IsNullOrEmpty(Name);

        public static implicit operator ShaderProperty(string name) => new ShaderProperty(name);
        public static implicit operator string(ShaderProperty property) => property.Name;

        public bool Equals(ShaderProperty other) => string.Equals(Name, other.Name, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ShaderProperty other && Equals(other);
        public override int GetHashCode() => Name?.GetHashCode() ?? 0;
        public override string ToString() => Name;
    }
}
