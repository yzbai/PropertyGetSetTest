/*
 * Author：Yuzhao Bai
 * Email: yuzhaobai@outlook.com
 * The code of this file and others in HB.FullStack.* are licensed under MIT LICENSE.
 */

namespace System
{
    public class TypeDef
    {
        public Type Type { get; set; } = null!;

        public IDictionary<string, PropertyDef> PropertyDefs { get; set; } = new Dictionary<string, PropertyDef>();
        public string FullName { get; set; } = null!;

        public PropertyDef? GetPropertyDef(string propertyName)
        {
            if(PropertyDefs.TryGetValue(propertyName, out PropertyDef? propertyDef))
            {
                return propertyDef;
            }

            return null;
        }
    }
}