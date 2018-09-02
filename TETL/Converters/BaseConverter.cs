using System;
using System.Reflection;
using TETL.Attributes;

namespace TETL.Converters
{
    public abstract class BaseConverter<TRow, TTarget>
    {
        protected Action<TRow, TTarget> _setter;
        protected Func<TRow, TTarget> _getter;

        protected TextFileMappingAttribute _mappingAttribute;

        public virtual void Setup(PropertyInfo propertyInfo, TextFileMappingAttribute mapper)
        {
            _mappingAttribute = mapper;
            _setter = (Action<TRow, TTarget>)propertyInfo.GetSetMethod().CreateDelegate(typeof(Action<TRow, TTarget>));
            _getter = (Func<TRow, TTarget>)propertyInfo.GetGetMethod().CreateDelegate(typeof(Func<TRow, TTarget>));
        }

        public abstract void SetValue(object target, string value);
        public abstract string GetValue(object target);
    }
}
