using System;

namespace ROS.Game.Editor
{
    /// <summary>
    /// Bloquea el registro directo de menús legacy dentro de ROS.Game.Editor.
    ///
    /// Las herramientas siguen disponibles como métodos estáticos, pero las
    /// entradas históricas declaradas con [MenuItem] ya no se publican en la
    /// barra superior de Unity. Las opciones aprobadas se exponen únicamente
    /// desde RulesOfSurvivalToolsMenu usando UnityEditor.MenuItem de forma
    /// explícita.
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
