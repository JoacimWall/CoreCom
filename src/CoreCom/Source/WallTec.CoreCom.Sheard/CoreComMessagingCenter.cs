using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WallTec.CoreCom.Sheard.Models;

namespace WallTec.CoreCom.Sheard
{
    internal interface ICoreComMessagingCenter
    {
        void Subscribe(string message, Action callback, Boolean isAuth);
        void Subscribe(string message, Action<CoreComUserInfo> callback, Boolean isAuth);
        void Subscribe<TArgs>(string message, Action<TArgs> callback, Boolean isAuth) where TArgs : class;
        void Subscribe<TArgs>(string message, Action<TArgs, CoreComUserInfo> callback, Boolean isAuth) where TArgs : class;

        void Send(string message, CoreComUserInfo coreComUserInfo);
        void Send<TArgs>(string message, CoreComUserInfo coreComUserInfo, TArgs args) where TArgs : class;

        void Unsubscribe(string message);
    }

    public class CoreComMessagingCenter : ICoreComMessagingCenter
    {
        internal static ICoreComMessagingCenter Instance { get; } = new CoreComMessagingCenter();

        class Sender : Tuple<string, Type>
        {
            public Sender(string message, Type argType) : base(message, argType)
            {
            }
        }

        internal class Subscription : Tuple<object, MethodInfo, Type, Boolean>
        {
            public Subscription(object delegateSource, MethodInfo methodInfo, Type argType, Boolean isAuth)
                : base(delegateSource, methodInfo, argType, isAuth)
            {
            }


            object DelegateSource => Item1;
            MethodInfo MethodInfo => Item2;
            Type ArgType => Item3;
            Boolean IsAuth => Item4;

            public void InvokeCallback(object args, CoreComUserInfo coreComUserInfo)
            {

                if (MethodInfo.IsStatic)
                {
                    if (coreComUserInfo == null)
                        MethodInfo.Invoke(null, MethodInfo.GetParameters().Length == 1 ? new[] { args } : null);
                    else
                        MethodInfo.Invoke(null, MethodInfo.GetParameters().Length == 1 ? new[] { args, coreComUserInfo } : null);

                    return;
                }

                if (coreComUserInfo == null)
                    MethodInfo.Invoke(DelegateSource, new[] { args });
                else
                    MethodInfo.Invoke(DelegateSource, MethodInfo.GetParameters().Length == 1 ? new[] { coreComUserInfo } : new[] { args, coreComUserInfo });

            }
        }

        static internal readonly Dictionary<String, Subscription> _subscriptions =
            new Dictionary<String, Subscription>();

        public static Type GetMessageArgType(string message)
        {
            var type = _subscriptions.First(x => x.Key == message);
            return type.Value.Item3;

        }
        public static bool GetMessageIsAuth(string message)
        {
            var type = _subscriptions.First(x => x.Key == message);
            return type.Value.Item4;

        }
        public static void Subscribe(string message, Action callback, Boolean isAuth)
        {
            Instance.Subscribe(message, callback, isAuth);
        }
        public static void Subscribe(string message, Action<CoreComUserInfo> callback, Boolean isAuth)
        {
            Instance.Subscribe(message, callback, isAuth);
        }
        public static void Subscribe<TArgs>(string message, Action<TArgs> callback, Boolean isAuth) where TArgs : class
        {
            Instance.Subscribe(message, callback, isAuth);
        }
        public static void Subscribe<TArgs>(string message, Action<TArgs, CoreComUserInfo> callback, Boolean isAuth) where TArgs : class
        {
            Instance.Subscribe(message, callback, isAuth);
        }
        void ICoreComMessagingCenter.Subscribe(string message, Action callback, Boolean isAuth)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            InnerSubscribe(message, callback.Target, null, callback.GetMethodInfo(), isAuth);
        }
        void ICoreComMessagingCenter.Subscribe(string message, Action<CoreComUserInfo> callback, Boolean isAuth)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            InnerSubscribe(message, callback.Target, null, callback.GetMethodInfo(), isAuth);
        }
        void ICoreComMessagingCenter.Subscribe<TArgs>(string message, Action<TArgs> callback, Boolean isAuth)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            // var target = callback.Target;

            InnerSubscribe(message, callback.Target, typeof(TArgs), callback.GetMethodInfo(), isAuth);
        }
        void ICoreComMessagingCenter.Subscribe<TArgs>(string message, Action<TArgs, CoreComUserInfo> callback, Boolean isAuth)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            // var target = callback.Target;

            InnerSubscribe(message, callback.Target, typeof(TArgs), callback.GetMethodInfo(), isAuth);
        }
        void InnerSubscribe(string message, object target, Type argType, MethodInfo methodInfo, Boolean isAuth)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var value = new Subscription(target, methodInfo, argType, isAuth);
            if (_subscriptions.ContainsKey(message))
            {
                //TODO:Raise error signatur exist
                //_subscriptions[key].Add(value);
            }
            else
            {
                var list = new List<Subscription> { value };
                _subscriptions.Add(message, value);
            }
        }

        public static void Send(string message, CoreComUserInfo coreComUserInfo)
        {
            Instance.Send(message, coreComUserInfo);
        }
        public static void Send<TArgs>(string message, CoreComUserInfo coreComUserInfo, TArgs args) where TArgs : class
        {
            Instance.Send(message, coreComUserInfo, args);
        }


        void ICoreComMessagingCenter.Send(string message, CoreComUserInfo coreComUserInfo)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message));

            InnerSend(message, coreComUserInfo, null, null);
        }
        void ICoreComMessagingCenter.Send<TArgs>(string message, CoreComUserInfo coreComUserInfo, TArgs args)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message));

            InnerSend(message, coreComUserInfo, typeof(TArgs), args);
        }
        void InnerSend(string message, CoreComUserInfo coreComUserInfo, Type argType, object args)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message));

            if (!_subscriptions.ContainsKey(message) || !_subscriptions.Any())
                return;
            if (coreComUserInfo == null)
                _subscriptions[message].InvokeCallback(args, null);
            else
                _subscriptions[message].InvokeCallback(args, coreComUserInfo);
        }

        public static void Unsubscribe(string message)
        {
            Instance.Unsubscribe(message);

        }
        void ICoreComMessagingCenter.Unsubscribe(string message)
        {
            InnerUnsubscribe(message);
        }
        void InnerUnsubscribe(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (!_subscriptions.ContainsKey(message))
                return;
            _subscriptions.Remove(message);
        }
    }
}
