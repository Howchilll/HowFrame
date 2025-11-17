using System;

namespace HowFrame
{
    public static class UpdateAssistant
    {
        private static readonly OrderAction order60 = new();
        private static readonly OrderAction order30 = new();
        private static readonly OrderAction order15 = new();
        private static readonly OrderAction order1 = new();

        private static readonly OrderAction system60 = new();
        private static readonly OrderAction system30 = new();
        private static readonly OrderAction system15 = new();
        private static readonly OrderAction system1 = new();

        static UpdateAssistant()
        {
            // 自动注册到 Updater，使用自动心跳
            Updater.Instance.RegisterStatic(UnityUpdater, SystemUpdater);
        }

        private static void UnityUpdater()
        {
            order60.Invoke();
            if (Updater.Should30) order30.Invoke();
            if (Updater.Should15) order15.Invoke();
            if (Updater.Should1) order1.Invoke();
        }

        private static void SystemUpdater()
        {
            system60.Invoke();
            if (Updater.Should30) system30.Invoke();
            if (Updater.Should15) system15.Invoke();
            if (Updater.Should1) system1.Invoke();
        }

        public static void UnityUpdate(Action action, int order = 0, int fps = 60)
        {
            var oa = GetOrder(fps);
            oa += (action, order);
        }

        public static void SystemUpdate(Action action, int order = 0, int fps = 60)
        {
            var sa = GetSystem(fps);
            sa += (action, order);
        }

        public static void RemoveUnityUpdate(int index, int fps = 60)
        {
            var oa = GetOrder(fps);
            oa -= index;
        }

        public static void RemoveSystemUpdate(int index, int fps = 60)
        {
            var sa = GetSystem(fps);
            sa -= index;
        }

        public static void ClearAll()
        {
            order60.Clear(); order30.Clear(); order15.Clear(); order1.Clear();
            system60.Clear(); system30.Clear(); system15.Clear(); system1.Clear();
        }

        private static OrderAction GetOrder(int fps) => fps switch
        {
            60 => order60,
            30 => order30,
            15 => order15,
            1 => order1,
            _ => throw new ArgumentException($"不支持的帧率 {fps}")
        };

        private static OrderAction GetSystem(int fps) => fps switch
        {
            60 => system60,
            30 => system30,
            15 => system15,
            1 => system1,
            _ => throw new ArgumentException($"不支持的帧率 {fps}")
        };
    }
}
