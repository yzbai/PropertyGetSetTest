/*
 * Author：Yuzhao Bai
 * Email: yuzhaobai@outlook.com
 * The code of this file and others in HB.FullStack.* are licensed under MIT LICENSE.
 */


using System.Reflection;

namespace System
{
    public class PropertyDef
    {
        public string PropertyName { get; set; } = null!;

        public Type PropertyType { get; set; } = null!;

        public MethodInfo? SetMethod { get; set; }

        public MethodInfo? GetMethod { get; set; }
    }
}