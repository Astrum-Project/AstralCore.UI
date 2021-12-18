using System;
using System.Reflection;

namespace Astrum.AstralCore.UI.Attributes
{
    public class UIBaseAttribute : Attribute
    {
        public string Module;
        public string Name;

        public UIBaseAttribute(string module, string name) => (Module, Name) = (module, name);
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class UIRawAttribute : UIBaseAttribute
    {
        public UIRawAttribute(string module, string name) : base(module, name) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class UIButton : UIBaseAttribute
    {
        public void Click() => OnClick();

        public UIButton(string module, string name) : base(module, name) { }

        internal Action OnClick;
        internal void Setup(MethodInfo info)
        {
            Logger.Trace($"[Core.UI] Button {Module}:{Name}");
            OnClick = (Action)Delegate.CreateDelegate(typeof(Action), info);
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class UIFieldProp<T> : UIBaseAttribute
    {
        public abstract T Value { get; set; }

        public Func<T, bool> Validator;

        public virtual T FromString(string val) => Value = MelonLoader.TinyJSON.Decoder.Decode(val).Make<T>();

        public UIFieldProp(string module, string name) : base(module, name) { }
        public UIFieldProp(string module, string name, Func<T, bool> validator) : base(module, name) => Validator = validator;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class UIField<T> : UIFieldProp<T>
    {
        public override T Value
        {
            get => _value;
            set
            {
                if (!(Validator?.Invoke(value) ?? true))
                    return;

                _value = value;
                Info?.SetValue(null, value);
            }
        }

        public new Func<T, bool> Validator;

        public UIField(string module, string name) : base(module, name) { }
        public UIField(string module, string name, Func<T, bool> validator) : base(module, name) => Validator = validator;

        public T Refresh() => _value = (T)Info.GetValue(null);

        internal T _value;
        internal FieldInfo Info;
        internal void Setup(FieldInfo info)
        {
            Logger.Trace($"[Core.UI] Field {Module}:{Name}");
            _value = (T)info.GetValue(null);
            Info = info;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class UIProperty<T> : UIFieldProp<T>
    {
        public override T Value
        {
            get => _get();
            set => _set(value);
        }

        public new Func<T, bool> Validator;

        public UIProperty(string module, string name) : base(module, name) { }
        public UIProperty(string module, string name, Func<T, bool> validator) : base(module, name) => Validator = validator;

        internal Func<T> _get;
        internal Action<T> _set;
        internal void Setup(PropertyInfo prop)
        {
            Logger.Trace($"[Core.UI] Property {Module}:{Name}");
            _get = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), prop.GetGetMethod());
            _set = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), prop.GetSetMethod());
        }
    }
}
