using System;
using System.Reflection;

namespace Astrum.AstralCore.UI.Attributes
{
    public class UIBase : Attribute
    {
        public string Module;
        public string Name;

        public UIBase(string module, string name) => (Module, Name) = (module, name);
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class UIRaw : UIBase
    {
        public UIRaw(string module, string name) : base(module, name) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class UIButton : UIBase
    {
        internal Action OnClick;

        public UIButton(string module, string name) : base(module, name) { }

        public UIButton Setup(MethodInfo info)
        {
            Logger.Trace($"[Core.UI] Button {Module}:{Name}");
            OnClick = (Action)Delegate.CreateDelegate(typeof(Action), info);
            return this;
        }

        public void Click() => OnClick();
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class UIFieldProp<T> : UIBase
    {
        public abstract T Value { get; set; }

        public Func<T, bool> Validator;

        public UIFieldProp(string module, string name) : base(module, name) { }
        public UIFieldProp(string module, string name, Func<T, bool> validator) : base(module, name) => Validator = validator;
        
        public virtual T FromString(string val) => Value = MelonLoader.TinyJSON.Decoder.Decode(val).Make<T>();
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class UIField<T> : UIFieldProp<T>
    {
        public override T Value
        {
            get => _value;
            set
            {
                if (_value.Equals(value)) return;
                if (!(Validator?.Invoke(value) ?? true))
                    return;

                _value = value;
                Info?.SetValue(null, value);
            }
        }

        public new Func<T, bool> Validator;

        internal T _value;
        internal FieldInfo Info;

        public UIField(string module, string name) : base(module, name) { }
        public UIField(string module, string name, Func<T, bool> validator) : base(module, name) => Validator = validator;

        public UIField<T> Setup(FieldInfo info)
        {
            Logger.Trace($"[Core.UI] Field {Module}:{Name}");
            _value = (T)info.GetValue(null);
            Info = info;
            return this;
        }

        public T Refresh() => _value = (T)Info.GetValue(null);
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

        internal Func<T> _get;
        internal Action<T> _set;

        public UIProperty(string module, string name) : base(module, name) { }
        public UIProperty(string module, string name, Func<T, bool> validator) : base(module, name) => Validator = validator;

        public UIProperty<T> Setup(PropertyInfo prop)
        {
            Logger.Trace($"[Core.UI] Property {Module}:{Name}");
            _get = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), prop.GetGetMethod());
            _set = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), prop.GetSetMethod());
            return this;
        }
    }
}
