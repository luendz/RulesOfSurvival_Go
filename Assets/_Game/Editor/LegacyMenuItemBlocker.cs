using System;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Impide que las utilidades internas Editor First registren entradas de
    /// menú por separado. El menú público del proyecto queda centralizado en
    /// EditorFirstMenuCleanup mediante UnityEditor.MenuItem explícito.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class MenuItem : Attribute
    {
        public MenuItem(string itemName)
            : this(itemName, false, 1000)
        {
        }

        public MenuItem(string itemName, bool isValidateFunction)
            : this(itemName, isValidateFunction, 1000)
        {
        }

        public MenuItem(
            string itemName,
            bool isValidateFunction,
            int priority
        )
        {
            ItemName = itemName;
            IsValidateFunction = isValidateFunction;
            Priority = priority;
        }

        public string ItemName { get; }
        public bool IsValidateFunction { get; }
        public int Priority { get; }
    }
}
